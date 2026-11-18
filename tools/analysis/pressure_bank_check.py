"""Run: python tools/analysis/pressure_bank_check.py   (LPG_TABLE=2 for the v2.94 map)
1) Does FAST still depend on reducer pressure after the map error? (leave one file out, raw FAST, no pressure correction)
   2) Bank balance per 500 rpm (the RifGiri axis that CorrSecondaBancata0..11 probably uses) and GAS/BENZ per bank."""
import os, statistics as st, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import map_prediction as loo
from collections import defaultdict

data = loo.load_all()
files = list(dict.fromkeys(it.file for it in data))
print(f'table {loo.TABLE}: {len(files)} logs, {len(data)} readings')


def keep(it):
    return bool(it.dpos) and abs(it.benzdiff) < 6 and loo.app_filter(it)


def cell_err(train):
    acc = defaultdict(lambda: ([], []))
    for it in train:
        if not keep(it):
            continue
        for bank in (1, 2):
            w = defaultdict(float); n = 0
            for rpm, b1, b2 in it.dpos:
                benz = b1 if bank == 1 else b2
                if benz <= 0 or rpm <= 0:
                    continue
                r0, r1, fr = loo.bracket(loo.RPM, rpm); i0, i1, fi = loo.bracket(loo.INJ, benz)
                for c, ww in (((r0, i0), (1 - fr) * (1 - fi)), ((r1, i0), fr * (1 - fi)), ((r0, i1), (1 - fr) * fi), ((r1, i1), fr * fi)):
                    if ww > 1e-9:
                        w[c] += ww
                n += 1
            for c, ww in w.items():
                vals, ws = acc[(c, bank)]
                vals.append(it.fast1 if bank == 1 else it.fast2); ws.append(ww / n)
    err = {}
    for (c, bank), (vals, ws) in acc.items():
        err.setdefault(c, []).append(loo.wmedian(vals, ws))
    return {c: sum(v) / len(v) for c, v in err.items()}


def err_at(it, bank, err):
    s = 0; n = 0
    for rpm, b1, b2 in it.dpos:
        benz = b1 if bank == 1 else b2
        r0, r1, fr = loo.bracket(loo.RPM, rpm); i0, i1, fi = loo.bracket(loo.INJ, benz)
        for c, ww in (((r0, i0), (1 - fr) * (1 - fi)), ((r1, i0), fr * (1 - fi)), ((r0, i1), (1 - fr) * fi), ((r1, i1), fr * fi)):
            s += err.get(c, 0.0) * ww
        n += 1
    return s / n


# ---------- 1) pressure ----------
res = []   # (pressure at the delayed rows, residual, file, fast)
for f in files:
    err = cell_err([it for it in data if it.file != f])
    for it in data:
        if it.file != f or not keep(it):
            continue
        p = sum(r['Press'] for r in it.drows) / len(it.drows)
        for bank in (1, 2):
            fast = it.fast1 if bank == 1 else it.fast2
            res.append((p, fast - err_at(it, bank, err), f, fast))

print('\nPressure at the delayed rows -> FAST minus the map error (from the other logs), both banks')
EDGES = [0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.45, 1.5, 1.55, 1.6, 9]
print(f'{"bar":>12} {"n":>6} {"median":>7} {"mean":>6} {"FAST med":>8}  ECU curve %')
CURVE = list(zip([800, 862, 925, 987, 1050, 1113, 1175, 1238, 1301, 1363, 1426, 1489, 1570, 1652, 1734],
                 [2828, 2538, 2255, 1981, 1713, 1452, 1197, 948, 703, 464, 230, 0, -338, -668, -989]))


def curve(p):
    mb = p * 1000
    if mb <= CURVE[0][0]: return CURVE[0][1] / 100
    for (a, ca), (b, cb) in zip(CURVE, CURVE[1:]):
        if mb <= b: return (ca + (cb - ca) * (mb - a) / (b - a)) / 100
    return CURVE[-1][1] / 100


lo = 0
for hi in EDGES:
    sel = [r for r in res if lo < r[0] <= hi]
    if len(sel) >= 20:
        mid = st.median(r[0] for r in sel)
        print(f'{lo:5.2f}-{hi:5.2f} {len(sel):6d} {st.median(r[1] for r in sel):7.2f} {st.mean(r[1] for r in sel):6.2f} '
              f'{st.median(r[3] for r in sel):8.2f}  {curve(mid):6.1f} (at {mid:.2f})')
    lo = hi

# least-squares slope of residual on pressure (clip residuals to +-15 like the temperature fit)
xs = [r[0] for r in res]; ys = [max(-15, min(15, r[1])) for r in res]
mx, my = st.mean(xs), st.mean(ys)
b = sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / sum((x - mx) ** 2 for x in xs)
print(f'slope {b:.2f} % per bar (mean pressure {mx:.3f}); per file:')
for f in files:
    sel = [(r[0], max(-15, min(15, r[1]))) for r in res if r[2] == f]
    x = [s[0] for s in sel]; y = [s[1] for s in sel]
    if len(set(x)) > 1:
        mxf, myf = st.mean(x), st.mean(y)
        bf = sum((a - mxf) * (c - myf) for a, c in zip(x, y)) / sum((a - mxf) ** 2 for a in x)
        print(f'   {f:20s} n={len(sel):5d}  p {min(x):.2f}-{max(x):.2f}  slope {bf:6.2f}')

# ---------- 2) banks ----------
print('\nBanks per 500 rpm (rows where the petrol ECU regulates, |BENZ diff| < 6 %)')
print(f'{"rpm":>9} {"rows":>6} {"SLOW1-2":>8} {"FAST1-2":>8} {"total1-2":>9} {"GAS/BENZ 1/2":>13}')
rows = [r for it in data for r in it.rows if not (r['Fast1'] == 0 and r['Fast2'] == 0)
        and r['Benz1'] > 0 and r['Benz2'] > 0 and abs(r['Benz1'] - r['Benz2']) / ((r['Benz1'] + r['Benz2']) / 2) < 0.06]
for k in range(12):
    lo_r, hi_r = (0 if k == 0 else 500 * k + 250), 500 * (k + 1) + 250
    sel = [r for r in rows if lo_r < r['Rpm'] <= hi_r]
    if len(sel) < 50:
        print(f'{500 * (k + 1):9d} {len(sel):6d}  (too few)'); continue
    ds = st.median(r['Slow1'] - r['Slow2'] for r in sel)
    df = st.median(r['Fast1'] - r['Fast2'] for r in sel)
    dt = st.median(r['Slow1'] + r['Fast1'] - r['Slow2'] - r['Fast2'] for r in sel)
    ratio = st.median((r['Gas1'] / r['Benz1']) / (r['Gas2'] / r['Benz2']) for r in sel)
    print(f'{500 * (k + 1):9d} {len(sel):6d} {ds:8.2f} {df:8.2f} {dt:9.2f} {ratio:13.4f}')
