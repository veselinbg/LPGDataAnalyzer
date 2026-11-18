"""
Run: python tools/analysis/temperature_check.py [new log file names...]

Checks a temperature-table change: residual FAST (after the map error, all logs brought to the newest
log's tables) per reducer and gas band, old logs vs. the logs recorded with the new tables, and the
joint temperature fit on all / new / old logs. Default "new" = logs with the newest log's tables.
"""
import sys, os, statistics as st
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import temperature_tables as tt, map_prediction as loo
data = tt.data
err = tt.cell_errors(data)
LOGS = loo.logs()
NEWSET = set(sys.argv[1:]) or {n for _, n, g, r in LOGS if (g, r) == (LOGS[-1][2], LOGS[-1][3])}
RL = ['<=20','21-25','26-30','31-35','36-40','41-50','51-60','61-70','>70']
GL = ['<=0','0-10','11-20','21-30','31-40','41-50','51-60','61-70','>70']
def tab(sel, edges, labels, title):
    print(title)
    print('  band     old: n   mean  median |  new: n   mean  median')
    for b in range(9):
        row = []
        for grp in (lambda f: f not in NEWSET, lambda f: f in NEWSET):
            items = [it for it in data if grp(it.file)]
            res = [e for e, tg, tr in tt.samples(items, err) if tt.band(sel(tg, tr), edges) == b]
            res = [max(-15, min(15, e)) for e in res]
            row.append((len(res), st.mean(res) if res else 0, st.median(res) if res else 0))
        if row[0][0] or row[1][0]:
            print(f'  {labels[b]:7s} {row[0][0]:6d} {row[0][1]:+6.2f} {row[0][2]:+6.2f}  | {row[1][0]:6d} {row[1][1]:+6.2f} {row[1][2]:+6.2f}')
tab(lambda tg, tr: tr, tt.R_EDGES, RL, 'Reducer temp: residual FAST after map error (target = newest tables)')
tab(lambda tg, tr: tg, tt.G_EDGES, GL, 'Gas temp:')
print('tables now: gas', tt.CUR_G, 'reducer', tt.CUR_R)
for name, items in (('all', data), ('new only', [it for it in data if it.file in NEWSET]), ('old', [it for it in data if it.file not in NEWSET])):
    e = tt.cell_errors(items) if name != 'all' else err
    g, r = tt.joint_fit(tt.samples(items, err), lam=200)
    print(f'{name:9s} gas fit    ', ' '.join(f'{v:+.2f}' for v in g))
    print(f'{name:9s} reducer fit', ' '.join(f'{v:+.2f}' for v in r))
    print(f'{name:9s} suggestion  gas {tt.apply_round(g)}  reducer {tt.apply_round(r)}')
print('LOO score, new method lambda 200: %.2f %%' % tt.score(tt.m_new, lam=200))
