"""Run: python tools/analysis/gas_step_4ms.py
GAS / (BENZ x interpolated K) around 4 ms petrol time (LPG 35-45 C, reducer 40-70 C): the gas time steps up at BENZ = 4.00 ms,
which matches TempoMaxExtraInjBenz = 1562 ticks (4.00 ms) in the .ml8."""
import sys, statistics as st, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import map_prediction as loo
from collections import defaultdict
pts = []   # (benz, gas, k, rpm, file)
for lid, name, _, _ in loo.logs():
    _, rows = loo.load_file(lid, name)
    for r in rows:
        if not (35 < r['TempGas'] < 45 and 40 < r['TempReducer'] < 70): continue
        for g, p in ((r['Gas1'], r['Benz1']), (r['Gas2'], r['Benz2'])):
            if p <= 0 or r['Rpm'] <= 0: continue
            r0, r1, fr = loo.bracket(loo.RPM, r['Rpm']); i0, i1, fi = loo.bracket(loo.INJ, p)
            k = (loo.BASE[r0][i0]*(1-fr)*(1-fi)+loo.BASE[r1][i0]*fr*(1-fi)+loo.BASE[r0][i1]*(1-fr)*fi+loo.BASE[r1][i1]*fr*fi)/100
            pts.append((p, g, k, r['Rpm'], name))
print('fine bins 3.5-4.5 ms: BENZ, rows, GAS/(BENZ*K)')
b = defaultdict(list)
for p, g, k, rpm, f in pts:
    if 3.5 <= p < 4.5: b[int(p*20)/20].append(g/(p*k))
for x in sorted(b): print(f'  {x:.2f} {len(b[x]):5d} {st.median(b[x]):.3f}')
# linear fit gas = a*(benz*k) + c on each side
def fit(sel):
    xs = [p*k for p, g, k, *_ in sel]; ys = [g for p, g, k, *_ in sel]
    mx, my = st.mean(xs), st.mean(ys)
    a = sum((x-mx)*(y-my) for x, y in zip(xs, ys))/sum((x-mx)**2 for x in xs)
    return a, my - a*mx, len(sel)
for lo, hi in ((1.5, 3.99), (4.01, 8), (8, 16)):
    a, c, n = fit([q for q in pts if lo <= q[0] < hi])
    print(f'BENZ {lo}-{hi}: GAS = {a:.3f} * BENZ*K + {c:.3f} ms  (n={n})')
print('per rpm band: ratio just below (3.6-3.99) and just above (4.01-4.4)')
for lo, hi in ((0, 1200), (1200, 2000), (2000, 2800), (2800, 9000)):
    a = [g/(p*k) for p, g, k, rpm, f in pts if lo < rpm <= hi and 3.6 <= p < 3.99]
    c = [g/(p*k) for p, g, k, rpm, f in pts if lo < rpm <= hi and 4.01 <= p < 4.4]
    if len(a) > 20 and len(c) > 20: print(f'  rpm {lo}-{hi}: {st.median(a):.3f} (n={len(a)})  {st.median(c):.3f} (n={len(c)})')
print('per log:')
for f in dict.fromkeys(q[4] for q in pts):
    a = [g/(p*k) for p, g, k, rpm, ff in pts if ff == f and 3.6 <= p < 3.99]
    c = [g/(p*k) for p, g, k, rpm, ff in pts if ff == f and 4.01 <= p < 4.4]
    if len(a) > 10 and len(c) > 10: print(f'  {f:20s} {st.median(a):.3f} {st.median(c):.3f}')
