"""Run: python tools/analysis/ecu_temperature_model.py

How does the ECU apply its temperature tables? Gas time = f(BENZ, map, pressure, temps): look at the ratio
GAS / model (map bilinear, pressure factor) against gas temperature and reducer temperature."""
import math, os, sys, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import map_prediction as loo
from collections import defaultdict

OFF = 0.47
rows = []
for log_id, name, _, _ in loo.logs():   # logs of every configuration with the map of LPG_TABLE
    for r in loo.con.execute(f'select Rpm,Gas1,Benz1,Gas2,Benz2,Press,TempGas,TempReducer,Map from LogRow where LogFileId=? order by RowIndex', (log_id,)):
        rpm, g1, b1, g2, b2, p, tg, tr, mp = r
        if rpm <= 0 or g1 <= 0 or b1 < 2.0 or p <= 0.5:
            continue
        r0, r1, fr = loo.bracket(loo.RPM, rpm); i0, i1, fi = loo.bracket(loo.INJ, b1)
        B = loo.BASE
        m = B[r0][i0]*(1-fr)*(1-fi) + B[r1][i0]*fr*(1-fi) + B[r0][i1]*(1-fr)*fi + B[r1][i1]*fr*fi
        model = b1 * m / 100 * (1.49 / p) ** 0.65
        rows.append(((g1 - OFF) / model, tg, tr))

G_EDGES = [0, 10, 20, 30, 40, 50, 60, 70]          # band i = (edge[i-1], edge[i]]
R_EDGES = [20, 25, 30, 35, 40, 50, 60, 70]
G_TAB = [-7, -5, -4, -2, 0, 2, 3, 4, 5]
R_TAB = [-7, -4, -1, 0, 0, 0, 0, 0, 0]
def band(t, edges):
    for i, e in enumerate(edges):
        if t <= e: return i
    return len(edges)
y = [math.log(x[0]) for x in rows]
gb = [band(x[1], G_EDGES) for x in rows]; rb = [band(x[2], R_EDGES) for x in rows]

def rms(res): return math.sqrt(sum(v * v for v in res) / len(res)) * 100

c = st.mean(y)
print(f'H0 constant only               rms {rms([v - c for v in y]):.3f} %')

# free step effects per band (backfitting)
g = [0.0] * 9; r = [0.0] * 9
for _ in range(30):
    c = st.mean(yy - g[a] - r[b] for yy, a, b in zip(y, gb, rb))
    for k in range(9):
        v = [yy - c - r[b] for yy, a, b in zip(y, gb, rb) if a == k]
        g[k] = st.mean(v) if v else 0
    for k in range(9):
        v = [yy - c - g[a] for yy, a, b in zip(y, gb, rb) if b == k]
        r[k] = st.mean(v) if v else 0
res = [yy - c - g[a] - r[b] for yy, a, b in zip(y, gb, rb)]
cg = Counter = __import__('collections').Counter
ng = cg(gb); nr = cg(rb)
# express relative to the anchor bands (gas 31-40 = index 4, reducer 36-40 = index 4)
print(f'Hfree step per band            rms {rms(res):.3f} %')
print('  gas band:     ' + ' '.join(f'{(math.exp(g[k]-g[4])-1)*100:+5.1f}({ng[k]})' for k in range(9)))
print('  gas table:    ' + ' '.join(f'{t - G_TAB[4]:+5d}' for t in G_TAB))
print('  reducer band: ' + ' '.join(f'{(math.exp(r[k]-r[4])-1)*100:+5.1f}({nr[k]})' for k in range(9)))
print('  reducer tab:  ' + ' '.join(f'{t:+5d}' for t in R_TAB))

def interp(t, pts, vals):
    if t <= pts[0]: return vals[0]
    if t >= pts[-1]: return vals[-1]
    for i in range(1, len(pts)):
        if t <= pts[i]:
            f = (t - pts[i-1]) / (pts[i] - pts[i-1]); return vals[i-1] + f * (vals[i] - vals[i-1])
def test(name, gf, rf):
    pred = [math.log(1 + gf(x[1]) / 100) + math.log(1 + rf(x[2]) / 100) for x in rows]
    c = st.mean(yy - p for yy, p in zip(y, pred))
    print(f'{name:30s} rms {rms([yy - p - c for yy, p in zip(y, pred)]):.3f} %')
test('table as steps', lambda t: G_TAB[band(t, G_EDGES)], lambda t: R_TAB[band(t, R_EDGES)])
gc = [-10, 5, 15, 25, 35, 45, 55, 65, 75]; rc = [10, 22.5, 27.5, 32.5, 37.5, 45, 55, 65, 75]
test('table interpolated, centres', lambda t: interp(t, gc, G_TAB), lambda t: interp(t, rc, R_TAB))
ge = [0, 10, 20, 30, 40, 50, 60, 70, 80]; re_ = [20, 25, 30, 35, 40, 50, 60, 70, 80]
test('table interpolated, upper edge', lambda t: interp(t, ge, G_TAB), lambda t: interp(t, re_, R_TAB))
gl = [-20, 0, 10, 20, 30, 40, 50, 60, 70]; rl = [0, 20, 25, 30, 35, 40, 50, 60, 70]
test('table interpolated, lower edge', lambda t: interp(t, gl, G_TAB), lambda t: interp(t, rl, R_TAB))
