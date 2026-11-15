"""
Run: python tools/analysis/temperature_tables.py   (uses map_prediction.py for the data and the map error)

Temperature table prediction: Python copy of TemperatureCorrectionPrediction (current) and an improved version,
scored leave-one-file-out like the map: suggest table changes from the other files, then see how much of the
hidden file's (map-corrected) FAST trim the change would cancel. The ECU applies the tables interpolated between
the band upper edges (verified in temp2.py), so the hidden file is scored that way for every method.
"""
import bisect, math, os, statistics as st, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import map_prediction as loo
from collections import defaultdict

G_EDGES = [0, 10, 20, 30, 40, 50, 60, 70]          # Settings bands: (prev, edge], last = above 70
R_EDGES = [20, 25, 30, 35, 40, 50, 60, 70]
G_PTS = [0, 10, 20, 30, 40, 50, 60, 70, 80]          # where the ECU takes each band's value (upper edge)
R_PTS = [20, 25, 30, 35, 40, 50, 60, 70, 80]
G_ANCHOR = 4                                          # Temp_31_40 stays 0
NB = 9


def band(t, edges):
    for i, e in enumerate(edges):
        if t <= e:
            return i
    return len(edges)


def interp_w(t, pts):
    """Weights of the breakpoints around t (linear, clamped at the ends)."""
    if t <= pts[0]:
        return {0: 1.0}
    if t >= pts[-1]:
        return {len(pts) - 1: 1.0}
    j = bisect.bisect_left(pts, t)
    if pts[j] == t:
        return {j: 1.0}
    f = (t - pts[j - 1]) / (pts[j] - pts[j - 1])
    return {j - 1: 1 - f, j: f}


def bin_idx(labels, v):
    return min(bisect.bisect_left(labels, v), len(labels) - 1)


def keep(it):
    if it.fast1 == 0 and it.fast2 == 0:
        return False
    b = [(p[1] + p[2]) / 2 for p in it.pos + it.dpos]
    return max(b) / max(min(b), 0.1) <= 2.0


data = loo.load_all()
for it in data:
    rows = it.rows; n = len(rows)
    it.tg = loo.r2(sum(r['TempGas'] for r in rows) / n)
    it.tr = loo.r2(sum(r['TempReducer'] for r in rows) / n)
files = list(dict.fromkeys(it.file for it in data))

# extra fields for the current method
EX = {}
for it in data:
    rows = it.rows; n = len(rows)
    rpm = int(round(sum(r['Rpm'] for r in rows) / n))
    benz = loo.r2(sum((r['Benz1'] + r['Benz2']) / 2 for r in rows) / n)
    keys = [(bin_idx(loo.INJ, (r['Benz1'] + r['Benz2']) / 2), bin_idx(loo.RPM, r['Rpm'])) for r in rows]
    stab = max(keys.count(k) for k in set(keys)) / n if n > 1 else 1
    EX[id(it)] = dict(rpm=rpm, benz=benz, stab=stab,
                      map=loo.r2(sum(r['Map'] for r in rows) / n),
                      fast=loo.r2(sum((r['Fast1'] + r['Fast2']) / 2 for r in rows) / n),
                      b1=sum(r['Benz1'] for r in rows) / n, g1=sum(r['Gas1'] for r in rows) / n,
                      b2=sum(r['Benz2'] for r in rows) / n, g2=sum(r['Gas2'] for r in rows) / n)


def current_method(train, cur_g, cur_r, min_band=20, deadband=1.0):
    """TemperatureCorrectionPrediction.Predict for both tables. Returns the suggested changes per band."""
    usable = []
    prev = None
    for it in train:
        x = EX[id(it)]
        ok = x['b1'] > 0 and x['g1'] > 0 and x['b2'] > 0 and x['g2'] > 0 and x['stab'] >= 0.6 and x['map'] <= 0.95
        if ok and prev is not None and prev.file == it.file and prev.press > 0 and abs(it.press - prev.press) / prev.press > 0.05:
            ok = False
        prev = it
        if ok:
            usable.append((it, bin_idx(loo.INJ, x['benz']) * loo.NR + bin_idx(loo.RPM, x['rpm'])))
    cells = defaultdict(list)
    for it, c in usable:
        cells[c].append(EX[id(it)]['fast'])
    med = {c: (st.median(v), len(v)) for c, v in cells.items()}
    out = []
    for temp, edges, anchor in ((lambda it: it.tg, G_EDGES, G_ANCHOR), (lambda it: it.tr, R_EDGES, None)):
        res = defaultdict(list)
        for it, c in usable:
            m, n = med[c]
            if n < 3:
                continue
            res[band(temp(it), edges)].append(EX[id(it)]['fast'] - m)
        err = {b: st.median(v) for b, v in res.items() if v}
        off = err.get(anchor, 0) if anchor is not None and len(res.get(anchor, [])) >= min_band else 0
        d = [0] * NB
        for b in range(NB):
            if b == anchor or b not in err:
                continue
            e = err[b] - off
            if len(res[b]) >= min_band and abs(e) >= deadband:
                d[b] = int(math.copysign(math.floor(abs(e) + 0.5), e))
        out.append(d)
    return out


def cell_errors(train, ref=1.49):
    """Per-cell map error (weighted median of pressure-corrected FAST, interpolation weights, delayed rows)."""
    cells1 = defaultdict(list); cells2 = defaultdict(list)
    for it in train:
        if abs(it.benzdiff) >= 6 or not it.dpos or not keep(it):
            continue
        for bank, cells in ((1, cells1), (2, cells2)):
            w = defaultdict(float); n = 0
            for (rpm, b1, b2) in it.dpos:
                benz = b1 if bank == 1 else b2
                if benz <= 0 or rpm <= 0:
                    continue
                r0, r1, fr = loo.bracket(loo.RPM, rpm); i0, i1, fi = loo.bracket(loo.INJ, benz)
                for ii, rr, ww in ((i0, r0, (1 - fr) * (1 - fi)), (i0, r1, fr * (1 - fi)), (i1, r0, (1 - fr) * fi), (i1, r1, fr * fi)):
                    if ww > 1e-9:
                        w[(rr, ii)] += ww
                n += 1
            if n:
                for c, ww in w.items():
                    cells[c].append((it, ww / n))
    err = [[0.0] * loo.NI for _ in range(loo.NR)]
    for r in range(loo.NR):
        for i in range(loo.NI):
            s = 0; banks = 0
            for cl, sel in ((cells1.get((r, i), []), 'fast1'), (cells2.get((r, i), []), 'fast2')):
                if cl:
                    s += loo.wmedian([loo.press_corr(getattr(it, sel), it.press, ref) for it, _ in cl], [w for _, w in cl]); banks += 1
            if banks:
                err[r][i] = s / banks
    return err


def map_err_at(it, bank, err):
    s = 0; n = 0
    for (rpm, b1, b2) in it.dpos:
        benz = b1 if bank == 1 else b2
        if benz <= 0 or rpm <= 0:
            continue
        r0, r1, fr = loo.bracket(loo.RPM, rpm); i0, i1, fi = loo.bracket(loo.INJ, benz)
        s += err[r0][i0] * (1 - fr) * (1 - fi) + err[r1][i0] * fr * (1 - fi) + err[r0][i1] * (1 - fr) * fi + err[r1][i1] * fr * fi
        n += 1
    return s / n if n else None


def samples(items, err, ref=1.49, filt=True):
    """(residual trim after the map error, gas temp, reducer temp) per bank."""
    out = []
    for it in items:
        if not it.dpos or (filt and not keep(it)):
            continue
        for bank, sel in ((1, 'fast1'), (2, 'fast2')):
            m = map_err_at(it, bank, err)
            if m is None:
                continue
            out.append((loo.press_corr(getattr(it, sel), it.press, ref) - m, it.tg, it.tr))
    return out


def joint_fit(smp, lam=50.0, clip=15.0, interpolate=True, iters=40):
    """Least squares of residual = gas(tg) + reducer(tr) (changes per breakpoint), ridge towards 0, gas anchor fixed."""
    rows = []
    for e, tg, tr in smp:
        e = max(-clip, min(clip, e))
        wg = interp_w(tg, G_PTS) if interpolate else {band(tg, G_EDGES): 1.0}
        wr = interp_w(tr, R_PTS) if interpolate else {band(tr, R_EDGES): 1.0}
        rows.append((e, wg, wr))
    g = [0.0] * NB; r = [0.0] * NB
    for _ in range(iters):
        for which in ('g', 'r'):
            num = [0.0] * NB; den = [0.0] * NB
            for e, wg, wr in rows:
                pg = sum(w * g[k] for k, w in wg.items()); pr = sum(w * r[k] for k, w in wr.items())
                ws = wg if which == 'g' else wr
                vec = g if which == 'g' else r
                rest = e - pg - pr
                for k, w in ws.items():
                    num[k] += w * (rest + w * vec[k]); den[k] += w * w
            for k in range(NB):
                if which == 'g' and k == G_ANCHOR:
                    continue
                vec[k] = num[k] / (den[k] + lam) if den[k] > 0 else 0.0
    return g, r


def apply_round(vals, deadband=0.5):
    return [int(math.copysign(math.floor(abs(v) + 0.5), v)) if abs(v) >= deadband else 0 for v in vals]


def score(method, **kw):
    now = after = 0
    for f in files:
        train = [it for it in data if it.file != f]
        hidden = [it for it in data if it.file == f]
        err = cell_errors(train)
        dg, dr = method(train, err, **kw)
        for e, tg, tr in samples(hidden, err, filt=False):
            ch = sum(w * dg[k] for k, w in interp_w(tg, G_PTS).items()) + sum(w * dr[k] for k, w in interp_w(tr, R_PTS).items())
            now += e * e; after += (e - ch) ** 2
    return (1 - after / now) * 100


CUR_G = [-7, -5, -4, -2, 0, 2, 3, 4, 5]; CUR_R = [-7, -4, -1, 0, 0, 0, 0, 0, 0]

def m_current(train, err, **kw):
    return current_method(train, CUR_G, CUR_R, **kw)

def m_new(train, err, lam=50.0, interpolate=True, deadband=0.5, separate=False):
    smp = samples(train, err)
    if separate:
        g, _ = joint_fit([(e, tg, 40) for e, tg, tr in smp], lam=lam, interpolate=interpolate)
        _, r = joint_fit([(e, 40, tr) for e, tg, tr in smp], lam=lam, interpolate=interpolate)
    else:
        g, r = joint_fit(smp, lam=lam, interpolate=interpolate)
    return apply_round(g, deadband), apply_round(r, deadband)


if __name__ == '__main__':
    print(f'current method                         {score(m_current):6.2f} %')
    full = cell_errors(data)
    print('  suggested now (all files): ', current_method(data, CUR_G, CUR_R))
    for lam in (20, 50, 200, 1000):
        print(f'new joint, interpolated, lambda {lam:<5}  {score(m_new, lam=lam):6.2f} %')
    print(f'new joint, steps, lambda 200           {score(m_new, lam=200, interpolate=False):6.2f} %')
    print(f'new separate, interpolated, lambda 200 {score(m_new, lam=200, separate=True):6.2f} %')
    g, r = joint_fit(samples(data, full), lam=200)
    print('  new fit on all files gas:    ', ' '.join(f'{v:+.2f}' for v in g))
    print('  new fit on all files reducer:', ' '.join(f'{v:+.2f}' for v in r))
