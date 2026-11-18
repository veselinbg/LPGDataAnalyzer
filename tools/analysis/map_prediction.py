"""
Python copy of LPGDbAnalyzer's map prediction (Parser grouping + FuelMapPrediction.BuildTable with ECU
interpolation weights, trim delay, pressure correction, the non-regulating / transient filter, gain 0.8)
and of PredictionScore (leave one file out). Used to try algorithm changes quickly before they go into C#.
On 2026-11-14 it reproduced the app exactly (4.75 % before the filter, per-file values to 0.1 %).

    python tools/analysis/map_prediction.py            # score with the app's settings

Environment: LPG_DB = database file (default: LPGDbAnalyzer/Data/lpg.db), LPG_TABLE = fuel table id (default 1);
the logs of every fuel table (.ml8 configuration) of the car with the same map are used, as "Analyze checked" does.
Like the app, load_all() brings every log's FAST trims to the newest log's temperature tables (TrimNormalizer,
temperature part only: all logs of one fuel table share the map). load_all(normalize_tables=False) skips that.
Keep it in step with the C# code when the algorithm changes.
"""
import bisect, json, math, os, sqlite3, statistics, sys
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DB = os.environ.get('LPG_DB', os.path.join(ROOT, 'LPGDbAnalyzer', 'Data', 'lpg.db'))
TABLE = int(os.environ.get('LPG_TABLE', '1'))
GAIN = 0.8
TOL = 0.1
MAX_GAP = 1500

con = sqlite3.connect(DB)
rpm_axis, inj_axis, cells = con.execute('select RpmAxis, InjectionAxis, CellValues from FuelTable where Id=?', (TABLE,)).fetchone()
RPM = json.loads(rpm_axis); INJ = json.loads(inj_axis)
VAL = json.loads(cells)                      # [inj][rpm]
BASE = [[VAL[i][r] for i in range(len(INJ))] for r in range(len(RPM))]   # [rpm][inj]
NR, NI = len(RPM), len(INJ)

COLS = 'TimeMs,Rpm,Gas1,Benz1,Press,Map,TempReducer,TempGas,Slow1,Fast1,Gas2,Benz2,Slow2,Fast2,Lambda1,Lambda2,Ox1,Ox2'.split(',')


def r2(x):  # C# Math.Round(x, 2), banker's
    return round(x, 2)


def reldiff(a, b):
    return 0.0 if a == 0 and b == 0 else r2((a - b) / ((a + b) / 2) * 100)


class Reading:
    __slots__ = ('file', 'fast1', 'fast2', 'press', 'benzdiff', 'pos', 'dpos', 'rows', 'drows', 'start', 'idx', 'tr', 'tg')


def load_file(log_id, name, delay_ms=850):
    raw = con.execute(f'select {",".join(COLS)} from LogRow where LogFileId=? order by RowIndex', (log_id,)).fetchall()
    rows = [dict(zip(COLS, r)) for r in raw]
    rows = [r for r in rows if r['Rpm'] > 0 and r['Gas1'] > 0 and r['Gas2'] > 0]
    groups, buf = [], []
    last1 = last2 = float('nan')
    for idx, r in enumerate(rows):
        t1 = r['Slow1'] + r['Fast1']; t2 = r['Slow2'] + r['Fast2']
        new = not buf or abs(t1 - last1) > TOL or abs(t2 - last2) > TOL
        if new and buf:
            groups.append(buf); buf = []
        last1, last2 = t1, t2
        buf.append(idx)
    if buf:
        groups.append(buf)
    times = [r['TimeMs'] for r in rows]
    pos = [(r['Rpm'], r['Benz1'], r['Benz2']) for r in rows]
    out = []
    for g_i, g in enumerate(groups):
        if g_i == 0:
            continue  # TEMPO 0 dropped
        it = Reading()
        it.file = name
        n = len(g)
        it.fast1 = r2(sum(rows[k]['Fast1'] for k in g) / n)
        it.fast2 = r2(sum(rows[k]['Fast2'] for k in g) / n)
        it.press = r2(sum(rows[k]['Press'] for k in g) / n)
        b1 = sum(rows[k]['Benz1'] for k in g) / n; b2 = sum(rows[k]['Benz2'] for k in g) / n
        it.benzdiff = r2(reldiff(b1, b2))
        it.pos = [pos[k] for k in g]
        it.rows = [rows[k] for k in g]
        d = []; dr = []
        for k in g:
            target = times[k] - delay_ms
            j = k
            while j >= 0 and times[j] > target:
                j -= 1
            if j < 0 or target - times[j] > MAX_GAP:
                d = []; dr = []; break
            d.append(pos[j]); dr.append(rows[j])
        it.dpos = d
        it.drows = dr
        it.idx = g[0]
        it.tg = r2(sum(rows[k]['TempGas'] for k in g) / n)
        it.tr = r2(sum(rows[k]['TempReducer'] for k in g) / n)
        out.append(it)
    return out, rows


def bracket(labels, v):
    if v <= labels[0]:
        return 0, 0, 0.0
    last = len(labels) - 1
    if v >= labels[last]:
        return last, last, 0.0
    up = bisect.bisect_left(labels, v)
    if labels[up] == v:
        return up, up, 0.0
    lo = up - 1
    return lo, up, (v - labels[lo]) / (labels[up] - labels[lo])


def press_corr(trim, p, ref):
    if ref <= 0:
        return trim
    if p <= 0:
        p = ref
    return trim * (math.sqrt(ref / p) if trim < 0 else math.sqrt(p / ref))


def wmedian(vals, ws):
    pairs = sorted((v, w) for v, w in zip(vals, ws) if w > 0)
    tot = sum(w for _, w in pairs)
    if tot <= 0:
        return 0.0
    half = tot / 2; eps = tot * 1e-9; cum = 0
    for i, (v, w) in enumerate(pairs):
        cum += w
        if cum > half + eps:
            return v
        if cum >= half - eps:
            return (v + pairs[i + 1][0]) / 2 if i + 1 < len(pairs) else v
    return pairs[-1][0]


def app_filter(it):
    """FuelMapPrediction.IsNotRegulating / IsLoadTransient: FAST 0 on both banks, or BENZ changing > 2x."""
    if it.fast1 == 0 and it.fast2 == 0:
        return False
    b = [(p[1] + p[2]) / 2 for p in it.pos + it.dpos]
    return max(b) / max(min(b), 0.1) <= 2.0


def build(train, ref=1.49, benzmax=6.0, min_change=0.0, round_map=True, keep=app_filter, agg=None, gain=None, min_count=1, sign_gain=None, train_fast=None, read_w=None, pooled=False):
    """Returns the new map [rpm][inj]. keep(reading) -> bool filters readings."""
    cells1 = defaultdict(list); cells2 = defaultdict(list)
    for it in train:
        if abs(it.benzdiff) >= benzmax:
            continue
        if not it.dpos:
            continue
        if keep is not None and not keep(it):
            continue
        for bank, cells in ((1, cells1), (2, cells2)):
            w = defaultdict(float); n = 0
            for (rpm, b1, b2) in it.dpos:
                benz = b1 if bank == 1 else b2
                if benz <= 0 or rpm <= 0:
                    continue
                r0, r1, fr = bracket(RPM, rpm); i0, i1, fi = bracket(INJ, benz)
                for (ii, rr, ww) in ((i0, r0, (1 - fr) * (1 - fi)), (i0, r1, fr * (1 - fi)), (i1, r0, (1 - fr) * fi), (i1, r1, fr * fi)):
                    if ww > 1e-9:
                        w[(rr, ii)] += ww
                n += 1
            if n:
                for c, ww in w.items():
                    cells[c].append((it, ww / n * (read_w(it) if read_w else 1)))
    new = [row[:] for row in BASE]
    for r in range(NR):
        for i in range(NI):
            c1 = cells1.get((r, i), []); c2 = cells2.get((r, i), [])
            eff = sum(w for _, w in c1) + sum(w for _, w in c2)
            if eff <= 0 or eff <= min_count:
                continue
            s = 0; banks = 0
            if pooled:
                vals = [press_corr(it.fast1, it.press, ref) for it, _ in c1] + [press_corr(it.fast2, it.press, ref) for it, _ in c2]
                ws = [w for _, w in c1] + [w for _, w in c2]
                s, banks = wmedian(vals, ws), 1
            for cl, sel in (() if pooled else ((c1, 'fast1'), (c2, 'fast2'))):
                if cl:
                    vals = [press_corr(train_fast(it, sel) if train_fast else getattr(it, sel), it.press, ref) for it, _ in cl]
                    ws = [w for _, w in cl]
                    s += (agg or wmedian)(vals, ws); banks += 1
            avg = s / banks
            if abs(avg) > min_change:
                g = (sign_gain[0] if avg > 0 else sign_gain[1]) if sign_gain else (gain(eff) if gain else GAIN)
                v = BASE[r][i] * (1 + avg * g / 100)
                new[r][i] = round(v) if round_map else round(v, 2)
    return new


def change_at(dpos, bank, ch):
    s = 0; n = 0
    for (rpm, b1, b2) in dpos:
        benz = b1 if bank == 1 else b2
        if benz <= 0 or rpm <= 0:
            continue
        r0, r1, fr = bracket(RPM, rpm); i0, i1, fi = bracket(INJ, benz)
        s += ch[r0][i0] * (1 - fr) * (1 - fi) + ch[r1][i0] * fr * (1 - fi) + ch[r0][i1] * (1 - fr) * fi + ch[r1][i1] * fr * fi
        n += 1
    return s / n if n else None


def score(data, **kw):
    files = list(dict.fromkeys(it.file for it in data))
    tot_now = tot_after = 0; per = []
    for f in files:
        new = build([it for it in data if it.file != f], **kw)
        ch = [[(new[r][i] / BASE[r][i] - 1) * 100 if BASE[r][i] > 0 else 0 for i in range(NI)] for r in range(NR)]
        now = after = 0; n = 0
        for it in data:
            if it.file != f or not it.dpos:
                continue
            for bank, trim in ((1, it.fast1), (2, it.fast2)):
                c = change_at(it.dpos, bank, ch)
                if c is None:
                    continue
                now += trim * trim; after += (trim - c) ** 2; n += 1
        per.append((f, n // 2, 1 - after / now))
        tot_now += now; tot_after += after
    return 1 - tot_after / tot_now, per


G_PTS = [0, 10, 20, 30, 40, 50, 60, 70, 80]   # gas table breakpoints (band upper temperatures, last + 10)
R_PTS = [20, 25, 30, 35, 40, 50, 60, 70, 80]   # reducer table breakpoints


def table_at(t, pts, vals):
    """TemperatureCorrectionPrediction.TableValueAt: linear between breakpoints, clamped at the ends."""
    if t <= pts[0]:
        return vals[0]
    if t >= pts[-1]:
        return vals[-1]
    j = bisect.bisect_left(pts, t)
    if pts[j] == t:
        return vals[j]
    f = (t - pts[j - 1]) / (pts[j] - pts[j - 1])
    return vals[j - 1] * (1 - f) + vals[j] * f


def normalize(items, rec_g, rec_r, tgt_g, tgt_r):
    """TrimNormalizer, temperature part: FAST -= (target factor / recorded factor - 1) * 100."""
    if rec_g == tgt_g and rec_r == tgt_r:
        return
    for it in items:
        if it.fast1 == 0 and it.fast2 == 0:
            continue
        def factor(g, r):
            return (1 + table_at(it.tg, G_PTS, g) / 100) * (1 + table_at(it.tr, R_PTS, r) / 100)
        extra = (factor(tgt_g, tgt_r) / factor(rec_g, rec_r) - 1) * 100
        it.fast1 = r2(it.fast1 - extra); it.fast2 = r2(it.fast2 - extra)


def logs():
    """(id, file name, gas table, reducer table) of the fuel table's logs in recording order."""
    return [(i, n, json.loads(g), json.loads(r)) for i, n, g, r in con.execute(
        '''select l.Id, l.FileName, l.GasTempCoef, l.ReducerTempCoef from LogFile l join FuelTable t on t.Id = l.FuelTableId
           join FuelTable s on s.Id = ? and s.CarId = t.CarId and s.RpmAxis = t.RpmAxis and s.InjectionAxis = t.InjectionAxis
                and s.CellValues = t.CellValues
           order by l.RecordedAt, l.Id''', (TABLE,))]


def load_all(delay_ms=850, normalize_tables=True, only=None, target=None):
    """All readings of the fuel table; only = optional set of file names.
    target = (gas table, reducer table) to bring the trims to; default the newest log's tables. Use it when the
    ECU tables were changed after the last log, so the map is predicted for the tables it will run with."""
    lst = logs()
    tgt_g, tgt_r = target if target else (lst[-1][2], lst[-1][3])
    data = []
    for log_id, name, g, r in lst:
        if only is not None and name not in only:
            continue
        items, _ = load_file(log_id, name, delay_ms)
        if normalize_tables:
            normalize(items, g, r, tgt_g, tgt_r)
        data += items
    return data


if __name__ == '__main__':
    data = load_all()
    s, per = score(data)
    print(f'all: {s*100:.2f} %')
    for f, n, sc in per:
        print(f'  {f:20s} {n:6d} {sc*100:6.2f} %')
