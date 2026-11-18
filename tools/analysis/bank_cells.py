"""
Run: python tools/analysis/bank_cells.py   (LPG_TABLE=2 for the v2.94 map)

Bank difference per map cell, like the app's "TrimDiff" and "FuelCorrection" reports (Analyses tab), but per OBD reading
(one trim group, not raw rows) and only where the petrol ECU regulates. Checks whether the per-cell pattern is real:
the logs are split into two halves (alternating in recording order) and each half's cell medians are compared.
Also: how much of the cell pattern a single bank-2 value or one value per rpm column would remove.
"""
import bisect, os, statistics as st, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import map_prediction as loo
from collections import defaultdict

MIN_N = 15


def cell(labels, v):
    return min(bisect.bisect_left(labels, v), len(labels) - 1)   # (previous, own], above the last -> last


lst = loo.logs()
per = defaultdict(lambda: ([], [], []))   # (rpm, inj) -> (trim diff, half, fuel correction)
for k, (log_id, name, _, _) in enumerate(lst):
    items, _ = loo.load_file(log_id, name)
    for it in items:
        if not loo.app_filter(it) or abs(it.benzdiff) >= 6:
            continue
        rows = it.rows; n = len(rows)
        rpm = sum(r['Rpm'] for r in rows) / n
        b1 = sum(r['Benz1'] for r in rows) / n; b2 = sum(r['Benz2'] for r in rows) / n
        td = sum(r['Slow1'] + r['Fast1'] - r['Slow2'] - r['Fast2'] for r in rows) / n
        c = (cell(loo.RPM, rpm), cell(loo.INJ, (b1 + b2) / 2))
        per[c][0].append(td); per[c][1].append(k % 2); per[c][2].append(-(b2 - b1) / b1 * 100)

print(f'table {loo.TABLE}: {len(lst)} logs. Median TrimDiff (bank 1 - bank 2, SLOW+FAST) per cell, cells with >= {MIN_N} readings')
print('inj\\rpm ' + ''.join(f'{r:>7}' for r in loo.RPM))
for i in range(loo.NI - 1, -1, -1):
    line = f'{loo.INJ[i]:7.2f} '
    for r in range(loo.NR):
        v = per.get((r, i))
        line += f'{st.median(v[0]):7.2f}' if v and len(v[0]) >= MIN_N else '      .'
    print(line)

print('\nSame cells, FuelCorrection = -(BENZ2 - BENZ1) / BENZ1 %')
for i in range(loo.NI - 1, -1, -1):
    line = f'{loo.INJ[i]:7.2f} '
    for r in range(loo.NR):
        v = per.get((r, i))
        line += f'{st.median(v[2]):7.2f}' if v and len(v[0]) >= MIN_N else '      .'
    print(line)

# consistency: cell medians of the two halves of the logs
pairs = []
for c, (td, half, _) in per.items():
    a = [x for x, h in zip(td, half) if h == 0]; b = [x for x, h in zip(td, half) if h == 1]
    if len(a) >= MIN_N and len(b) >= MIN_N:
        pairs.append((c, st.median(a), st.median(b), len(a) + len(b)))
xa = [p[1] for p in pairs]; xb = [p[2] for p in pairs]
ma, mb = st.mean(xa), st.mean(xb)
cov = sum((x - ma) * (y - mb) for x, y in zip(xa, xb))
corr = cov / (sum((x - ma) ** 2 for x in xa) * sum((y - mb) ** 2 for y in xb)) ** 0.5
print(f'\nHalves of the logs: {len(pairs)} cells with data in both; correlation of cell medians {corr:.2f}')
for c, a, b, n in sorted(pairs):
    print(f'   rpm {loo.RPM[c[0]]:5d} inj {loo.INJ[c[1]]:5.2f}  n={n:5d}  half A {a:6.2f}  half B {b:6.2f}')

# what a constant or a per-rpm-column bank-2 value would leave
allv = [(c, x) for c, (td, _, _) in per.items() for x in td]
tot = [x for _, x in allv]
const = st.median(tot)
col = {r: st.median([x for c, x in allv if c[0] == r]) for r in {c[0] for c, _ in allv}}
cellm = {c: st.median(v[0]) for c, v in per.items()}
rms = lambda xs: (sum(x * x for x in xs) / len(xs)) ** 0.5
print(f'\nRMS of TrimDiff over {len(tot)} readings: as is {rms(tot):.2f}; minus one value ({const:.2f}) '
      f'{rms([x - const for x in tot]):.2f}; minus one per rpm column {rms([x - col[c[0]] for c, x in allv]):.2f}; '
      f'minus each cell\'s median {rms([x - cellm[c] for c, x in allv]):.2f}')
print('per rpm column: ' + ', '.join(f'{loo.RPM[r]}: {v:.2f}' for r, v in sorted(col.items())))
