"""
Imports log files into LPGDbAnalyzer's database the same way Services/LogFileReader + Database.ImportLog do:
2 header lines skipped, rows with 22 numeric tab-separated columns kept unfiltered, RecordedAt from the
header, SHA-256 (upper-case hex) against duplicates, ECU temperature tables as compact JSON.
Usage: import_logs.py <db> <fuelTableId> <gasCoefJson> <reducerCoefJson> <file>...
"""
import datetime, gzip, hashlib, json, os, re, sqlite3, sys

COLUMNS = 22
INT_COLS = {0, 1, 19, 20, 21}  # TEMPO, GIRI, MARKER, AUTOMARKER, ECUMARKER -> (int) cast
NAMES = ["TimeMs", "Rpm", "Lambda1", "Gas1", "Benz1", "Press", "Map", "TempReducer", "TempGas", "Level",
         "Slow1", "Fast1", "Ox1", "Lambda2", "Gas2", "Benz2", "Slow2", "Fast2", "Ox2", "Marker", "AutoMarker", "EcuMarker"]
NUM = re.compile(r'^\s*[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?\s*$')


def parse_row(line):
    parts = line.split('\t')
    if len(parts) < COLUMNS:
        return None
    vals = []
    for i in range(COLUMNS):
        if not NUM.match(parts[i]):
            return None
        v = float(parts[i])
        vals.append(int(v) if i in INT_COLS else v)  # C# (int) truncates toward zero, like int()
    return vals


def recorded_at(header, path):
    m = re.search(r'real time data,(.*)$', header or '', re.I)
    if m:
        try:
            return datetime.datetime.strptime(m.group(1).strip(), '%A, %B %d, %Y %I:%M:%S %p')
        except ValueError:
            pass
    return datetime.datetime.fromtimestamp(os.path.getmtime(path))


def main():
    db, table_id, gas, reducer, *files = sys.argv[1:]
    gas_json = json.dumps(json.loads(gas), separators=(',', ':'))
    reducer_json = json.dumps(json.loads(reducer), separators=(',', ':'))

    con = sqlite3.connect(db)
    con.execute('PRAGMA foreign_keys = ON')
    assert con.execute('SELECT COUNT(*) FROM FuelTable WHERE Id = ?', (table_id,)).fetchone()[0] == 1

    for path in files:
        raw = open(path, 'rb').read()
        digest = hashlib.sha256(raw).hexdigest().upper()
        name = os.path.basename(path)

        dup = con.execute('SELECT FileName FROM LogFile WHERE FileHash = ?', (digest,)).fetchone()
        if dup:
            print(f'{name}: already imported as {dup[0]}, skipped')
            continue

        text = raw.decode('utf-8-sig', errors='replace')
        lines = re.split(r'\r\n|\n|\r', text)
        if lines and lines[-1] == '':
            lines.pop()

        rows, skipped = [], 0
        for line in lines[2:]:
            if not line.strip():
                continue
            r = parse_row(line)
            if r is None:
                skipped += 1
            else:
                rows.append(r)

        when = recorded_at(lines[0] if lines else None, path)
        now = datetime.datetime.now()
        with con:
            cur = con.execute(
                'INSERT INTO LogFile (FuelTableId, FileName, RecordedAt, ImportedAt, RowCount, FileHash, GasTempCoef, ReducerTempCoef, Content) '
                'VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)',
                (table_id, name, when.strftime('%Y-%m-%d %H:%M:%S'), now.strftime('%Y-%m-%d %H:%M:%S'),
                 len(rows), digest, gas_json, reducer_json, gzip.compress(open(path, 'rb').read(), 9)))  # the original file (schema 5)
            log_id = cur.lastrowid
            con.executemany(
                f'INSERT INTO LogRow (LogFileId, RowIndex, {", ".join(NAMES)}) VALUES ({", ".join("?" * (COLUMNS + 2))})',
                ((log_id, i, *r) for i, r in enumerate(rows)))
        lpg = sum(1 for r in rows if r[1] > 0 and r[3] > 0)
        print(f'{name}: recorded {when:%Y-%m-%d %H:%M:%S}, {len(rows)} rows ({lpg} on LPG), {skipped} skipped lines, LogFile.Id={log_id}')

    con.close()


main()
