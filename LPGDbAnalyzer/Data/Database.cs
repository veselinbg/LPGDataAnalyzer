using LPGDataAnalyzer.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;

namespace LPGDbAnalyzer.Data
{
    /// <summary>
    /// SQLite storage: Car -> FuelTable -> LogFile -> LogRow.
    /// One file, no server. Every method opens its own short-lived connection.
    /// </summary>
    public sealed class Database
    {
        /// <summary>
        /// 1: Car, FuelTable, LogFile, LogRow.
        /// 2: LogFile.GasTempCoef / ReducerTempCoef (ECU temperature tables in use while the log was recorded).
        /// 3: Prediction (stored fuel map predictions with their options).
        /// 4: FuelTable.ConfigFile / ConfigFileName / ConfigSavedAt / ConfigGasTempCoef / ConfigReducerTempCoef
        ///    (the AEB .ml8 configuration of the table).
        /// 5: LogFile.Content (the original log file, gzip); the Prediction table is dropped (stored predictions were removed).
        /// </summary>
        private const int SchemaVersion = 5;

        private readonly string _connectionString;

        public string FilePath { get; }

        public Database(string filePath)
        {
            FilePath = Path.GetFullPath(filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = FilePath,
                ForeignKeys = true,
            }.ToString();
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        // ---------------- Schema ----------------

        public void EnsureCreated()
        {
            using var c = Open();

            long version = (long)Scalar(c, "PRAGMA user_version")!;
            if (version >= SchemaVersion)
                return;

            using var tx = c.BeginTransaction();

            Execute(c, """
                CREATE TABLE IF NOT EXISTS Car (
                    Id                INTEGER PRIMARY KEY,
                    Name              TEXT NOT NULL,
                    Engine            TEXT,
                    Banks             INTEGER NOT NULL DEFAULT 2,
                    Modifications     TEXT,
                    PetrolEcu         TEXT,
                    FastTrimStep      REAL NOT NULL DEFAULT 0.78,
                    LpgEcu            TEXT,
                    Injectors         TEXT,
                    Reducer           TEXT,
                    ReferencePressure REAL NOT NULL DEFAULT 1.49,
                    TrimDelayMs       INTEGER NOT NULL DEFAULT 850,
                    Notes             TEXT,
                    CreatedAt         TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS FuelTable (
                    Id            INTEGER PRIMARY KEY,
                    CarId         INTEGER NOT NULL REFERENCES Car(Id) ON DELETE CASCADE,
                    Name          TEXT NOT NULL,
                    CreatedAt     TEXT NOT NULL,
                    RpmAxis       TEXT NOT NULL,   -- JSON array
                    InjectionAxis TEXT NOT NULL,   -- JSON array
                    CellValues    TEXT NOT NULL,   -- JSON array of rows [injection][rpm]
                    Image         BLOB,
                    ImageFileName TEXT,
                    Notes         TEXT
                );
                CREATE INDEX IF NOT EXISTS IX_FuelTable_Car ON FuelTable(CarId);

                CREATE TABLE IF NOT EXISTS LogFile (
                    Id          INTEGER PRIMARY KEY,
                    FuelTableId INTEGER NOT NULL REFERENCES FuelTable(Id) ON DELETE CASCADE,
                    FileName    TEXT NOT NULL,
                    RecordedAt  TEXT NOT NULL,
                    ImportedAt  TEXT NOT NULL,
                    RowCount    INTEGER NOT NULL,
                    FileHash    TEXT NOT NULL UNIQUE
                );
                CREATE INDEX IF NOT EXISTS IX_LogFile_FuelTable ON LogFile(FuelTableId);

                CREATE TABLE IF NOT EXISTS LogRow (
                    LogFileId   INTEGER NOT NULL REFERENCES LogFile(Id) ON DELETE CASCADE,
                    RowIndex    INTEGER NOT NULL,
                    TimeMs      INTEGER NOT NULL,
                    Rpm         INTEGER NOT NULL,
                    Lambda1     REAL, Gas1 REAL, Benz1 REAL,
                    Press       REAL, Map REAL,
                    TempReducer REAL, TempGas REAL, Level REAL,
                    Slow1       REAL, Fast1 REAL, Ox1 REAL,
                    Lambda2     REAL, Gas2 REAL, Benz2 REAL,
                    Slow2       REAL, Fast2 REAL, Ox2 REAL,
                    Marker      INTEGER, AutoMarker INTEGER, EcuMarker INTEGER,
                    PRIMARY KEY (LogFileId, RowIndex)
                ) WITHOUT ROWID;
                """, tx);

            if (version < 2)
            {
                // Logs imported before version 2 get the tables from LPG.Core's Settings.cs (editable per log).
                Execute(c, $"""
                    ALTER TABLE LogFile ADD COLUMN GasTempCoef TEXT;
                    ALTER TABLE LogFile ADD COLUMN ReducerTempCoef TEXT;
                    UPDATE LogFile SET GasTempCoef = '{JsonSerializer.Serialize(DefaultGasCoefficients)}',
                                       ReducerTempCoef = '{JsonSerializer.Serialize(DefaultReducerCoefficients)}';
                    """, tx);
            }

            if (version < 3)
            {
                Execute(c, """
                    CREATE TABLE IF NOT EXISTS Prediction (
                        Id            INTEGER PRIMARY KEY,
                        FuelTableId   INTEGER NOT NULL REFERENCES FuelTable(Id) ON DELETE CASCADE,
                        CreatedAt     TEXT NOT NULL,
                        LogIds        TEXT NOT NULL,   -- JSON array of LogFile ids
                        ReadingCount  INTEGER NOT NULL,
                        Options       TEXT NOT NULL,   -- JSON PredictionOptions
                        RpmAxis       TEXT NOT NULL,   -- JSON array
                        InjectionAxis TEXT NOT NULL,   -- JSON array
                        BaseMap       TEXT NOT NULL,   -- JSON [rpm][injection], null = empty cell
                        NewMap        TEXT NOT NULL,   -- JSON [rpm][injection]
                        Counts        TEXT NOT NULL,   -- JSON [rpm][injection] readings per cell
                        Notes         TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_Prediction_FuelTable ON Prediction(FuelTableId);
                    """, tx);
            }

            if (version < 4)
            {
                Execute(c, """
                    ALTER TABLE FuelTable ADD COLUMN ConfigFile BLOB;
                    ALTER TABLE FuelTable ADD COLUMN ConfigFileName TEXT;
                    ALTER TABLE FuelTable ADD COLUMN ConfigSavedAt TEXT;
                    ALTER TABLE FuelTable ADD COLUMN ConfigGasTempCoef TEXT;     -- JSON int array
                    ALTER TABLE FuelTable ADD COLUMN ConfigReducerTempCoef TEXT; -- JSON int array
                    """, tx);
            }

            if (version < 5)
            {
                Execute(c, """
                    ALTER TABLE LogFile ADD COLUMN Content BLOB;   -- the original .txt, gzip
                    DROP TABLE IF EXISTS Prediction;
                    """, tx);
            }

            Execute(c, $"PRAGMA user_version = {SchemaVersion}", tx);
            tx.Commit();
        }

        /// <summary>The ECU temperature tables from LPG.Core's Settings.cs, as whole numbers.</summary>
        public static int[] DefaultGasCoefficients => Settings.GasTemperatureCorrectionCoef.Select(v => (int)v).ToArray();
        public static int[] DefaultReducerCoefficients => Settings.ReductorTemperatureCorrectionCoef.Select(v => (int)v).ToArray();

        // ---------------- Cars ----------------

        public List<Car> GetCars()
        {
            using var c = Open();
            using var cmd = Command(c, "SELECT * FROM Car ORDER BY Name");
            using var r = cmd.ExecuteReader();

            var list = new List<Car>();
            while (r.Read())
            {
                list.Add(new Car
                {
                    Id = r.GetInt64(r.GetOrdinal("Id")),
                    Name = r.GetString(r.GetOrdinal("Name")),
                    Engine = NullableString(r, "Engine"),
                    Banks = r.GetInt32(r.GetOrdinal("Banks")),
                    Modifications = NullableString(r, "Modifications"),
                    PetrolEcu = NullableString(r, "PetrolEcu"),
                    FastTrimStep = r.GetDouble(r.GetOrdinal("FastTrimStep")),
                    LpgEcu = NullableString(r, "LpgEcu"),
                    Injectors = NullableString(r, "Injectors"),
                    Reducer = NullableString(r, "Reducer"),
                    ReferencePressure = r.GetDouble(r.GetOrdinal("ReferencePressure")),
                    TrimDelayMs = r.GetInt32(r.GetOrdinal("TrimDelayMs")),
                    Notes = NullableString(r, "Notes"),
                    CreatedAt = ReadDate(r, "CreatedAt"),
                });
            }

            return list;
        }

        /// <summary>Inserts a new car (Id 0) or updates an existing one.</summary>
        public void SaveCar(Car car)
        {
            Validate(car);

            using var c = Open();
            using var cmd = Command(c, car.Id == 0
                ? """
                  INSERT INTO Car (Name, Engine, Banks, Modifications, PetrolEcu, FastTrimStep, LpgEcu, Injectors,
                                   Reducer, ReferencePressure, TrimDelayMs, Notes, CreatedAt)
                  VALUES ($Name, $Engine, $Banks, $Modifications, $PetrolEcu, $FastTrimStep, $LpgEcu, $Injectors,
                          $Reducer, $ReferencePressure, $TrimDelayMs, $Notes, $CreatedAt)
                  RETURNING Id
                  """
                : """
                  UPDATE Car SET Name = $Name, Engine = $Engine, Banks = $Banks, Modifications = $Modifications,
                                 PetrolEcu = $PetrolEcu, FastTrimStep = $FastTrimStep, LpgEcu = $LpgEcu,
                                 Injectors = $Injectors, Reducer = $Reducer, ReferencePressure = $ReferencePressure,
                                 TrimDelayMs = $TrimDelayMs, Notes = $Notes
                  WHERE Id = $Id
                  RETURNING Id
                  """);

            Add(cmd, "$Id", car.Id);
            Add(cmd, "$Name", car.Name.Trim());
            Add(cmd, "$Engine", car.Engine);
            Add(cmd, "$Banks", car.Banks);
            Add(cmd, "$Modifications", car.Modifications);
            Add(cmd, "$PetrolEcu", car.PetrolEcu);
            Add(cmd, "$FastTrimStep", car.FastTrimStep);
            Add(cmd, "$LpgEcu", car.LpgEcu);
            Add(cmd, "$Injectors", car.Injectors);
            Add(cmd, "$Reducer", car.Reducer);
            Add(cmd, "$ReferencePressure", car.ReferencePressure);
            Add(cmd, "$TrimDelayMs", car.TrimDelayMs);
            Add(cmd, "$Notes", car.Notes);
            Add(cmd, "$CreatedAt", WriteDate(car.CreatedAt));

            car.Id = (long)cmd.ExecuteScalar()!;
        }

        public static void Validate(Car car)
        {
            if (string.IsNullOrWhiteSpace(car.Name))
                throw new InvalidOperationException("The car needs a name.");
            if (car.Banks is < 1 or > 2)
                throw new InvalidOperationException("Banks must be 1 or 2.");
            if (car.ReferencePressure < 0)
                throw new InvalidOperationException("Reference pressure must be 0 or more.");
            if (car.TrimDelayMs < 0)
                throw new InvalidOperationException("Trim delay must be 0 or more.");
        }

        // ---------------- Fuel tables ----------------

        /// <summary>Fuel tables of a car, newest first. Images are not loaded (see <see cref="GetFuelTableImage"/>).</summary>
        public List<FuelTable> GetFuelTables(long carId)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT t.Id, t.CarId, t.Name, t.CreatedAt, t.RpmAxis, t.InjectionAxis, t.CellValues,
                       t.ImageFileName, t.Notes,
                       (SELECT COUNT(*) FROM LogFile l WHERE l.FuelTableId = t.Id) AS LogCount,
                       t.Image IS NOT NULL AS HasImage,
                       t.ConfigFileName, t.ConfigSavedAt, t.ConfigGasTempCoef, t.ConfigReducerTempCoef
                FROM FuelTable t
                WHERE t.CarId = $CarId
                ORDER BY t.CreatedAt DESC, t.Id DESC
                """);
            Add(cmd, "$CarId", carId);

            using var r = cmd.ExecuteReader();
            var list = new List<FuelTable>();

            while (r.Read())
            {
                list.Add(new FuelTable
                {
                    Id = r.GetInt64(0),
                    CarId = r.GetInt64(1),
                    Name = r.GetString(2),
                    CreatedAt = ReadDate(r, "CreatedAt"),
                    RpmAxis = JsonSerializer.Deserialize<double[]>(r.GetString(4)) ?? [],
                    InjectionAxis = JsonSerializer.Deserialize<double[]>(r.GetString(5)) ?? [],
                    Values = JsonSerializer.Deserialize<double[][]>(r.GetString(6)) ?? [],
                    ImageFileName = NullableString(r, "ImageFileName"),
                    Notes = NullableString(r, "Notes"),
                    LogCount = r.GetInt32(9),
                    HasImage = r.GetBoolean(10),
                    ConfigFileName = NullableString(r, "ConfigFileName"),
                    ConfigSavedAt = NullableString(r, "ConfigSavedAt") is null ? null : ReadDate(r, "ConfigSavedAt"),
                    ConfigGasTempCoef = NullableCoefficients(r, "ConfigGasTempCoef"),
                    ConfigReducerTempCoef = NullableCoefficients(r, "ConfigReducerTempCoef"),
                });
            }

            return list;
        }

        public byte[]? GetFuelTableConfig(long fuelTableId)
        {
            using var c = Open();
            using var cmd = Command(c, "SELECT ConfigFile FROM FuelTable WHERE Id = $Id");
            Add(cmd, "$Id", fuelTableId);
            return cmd.ExecuteScalar() as byte[];
        }

        /// <summary>Stores (or replaces) the .ml8 configuration of an existing fuel table.</summary>
        public void AttachConfig(long fuelTableId, byte[] file, string fileName, DateTime savedAt, int[] gas, int[] reducer)
        {
            using var c = Open();
            using var cmd = Command(c, """
                UPDATE FuelTable SET ConfigFile = $File, ConfigFileName = $Name, ConfigSavedAt = $SavedAt,
                       ConfigGasTempCoef = $Gas, ConfigReducerTempCoef = $Reducer
                WHERE Id = $Id
                """);
            Add(cmd, "$Id", fuelTableId);
            Add(cmd, "$File", file);
            Add(cmd, "$Name", fileName);
            Add(cmd, "$SavedAt", WriteDate(savedAt));
            Add(cmd, "$Gas", JsonSerializer.Serialize(ValidCoefficients(gas, Settings.GasTemperatureRanges.Length, "LPG")));
            Add(cmd, "$Reducer", JsonSerializer.Serialize(ValidCoefficients(reducer, Settings.ReductorTemperatureRanges.Length, "reducer")));
            cmd.ExecuteNonQuery();
        }

        /// <summary>The original log file as imported, or null for logs imported before schema version 5 without it.</summary>
        public byte[]? GetLogFileContent(long logFileId)
        {
            using var c = Open();
            using var cmd = Command(c, "SELECT Content FROM LogFile WHERE Id = $Id");
            Add(cmd, "$Id", logFileId);
            return cmd.ExecuteScalar() is byte[] packed ? Gunzip(packed) : null;
        }

        private static byte[] Gzip(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize))
                gzip.Write(data);
            return output.ToArray();
        }

        private static byte[] Gunzip(byte[] packed)
        {
            using var input = new GZipStream(new MemoryStream(packed), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return output.ToArray();
        }

        public byte[]? GetFuelTableImage(long fuelTableId)
        {
            using var c = Open();
            using var cmd = Command(c, "SELECT Image FROM FuelTable WHERE Id = $Id");
            Add(cmd, "$Id", fuelTableId);
            return cmd.ExecuteScalar() as byte[];
        }

        /// <summary>
        /// Inserts a new fuel table (Id 0) or updates an existing one. On update the image is
        /// kept unless <see cref="FuelTable.Image"/> is set; the .ml8 configuration is stored only
        /// when <see cref="FuelTable.ConfigFile"/> is set (see also <see cref="AttachConfig"/>).
        /// </summary>
        public void SaveFuelTable(FuelTable table)
        {
            Validate(table);
            SaveFuelTableValues(table);

            if (table.ConfigFile is not null && table.ConfigFileName is not null && table.ConfigSavedAt is DateTime savedAt &&
                table.ConfigGasTempCoef is not null && table.ConfigReducerTempCoef is not null)
                AttachConfig(table.Id, table.ConfigFile, table.ConfigFileName, savedAt, table.ConfigGasTempCoef, table.ConfigReducerTempCoef);
        }

        private void SaveFuelTableValues(FuelTable table)
        {

            using var c = Open();
            using var cmd = Command(c, table.Id == 0
                ? """
                  INSERT INTO FuelTable (CarId, Name, CreatedAt, RpmAxis, InjectionAxis, CellValues, Image, ImageFileName, Notes)
                  VALUES ($CarId, $Name, $CreatedAt, $RpmAxis, $InjectionAxis, $CellValues, $Image, $ImageFileName, $Notes)
                  RETURNING Id
                  """
                : """
                  UPDATE FuelTable SET Name = $Name, CreatedAt = $CreatedAt, RpmAxis = $RpmAxis,
                         InjectionAxis = $InjectionAxis, CellValues = $CellValues,
                         Image = COALESCE($Image, Image), ImageFileName = COALESCE($ImageFileName, ImageFileName),
                         Notes = $Notes
                  WHERE Id = $Id
                  RETURNING Id
                  """);

            Add(cmd, "$Id", table.Id);
            Add(cmd, "$CarId", table.CarId);
            Add(cmd, "$Name", table.Name.Trim());
            Add(cmd, "$CreatedAt", WriteDate(table.CreatedAt));
            Add(cmd, "$RpmAxis", JsonSerializer.Serialize(table.RpmAxis));
            Add(cmd, "$InjectionAxis", JsonSerializer.Serialize(table.InjectionAxis));
            Add(cmd, "$CellValues", JsonSerializer.Serialize(table.Values));
            Add(cmd, "$Image", table.Image);
            Add(cmd, "$ImageFileName", table.ImageFileName);
            Add(cmd, "$Notes", table.Notes);

            table.Id = (long)cmd.ExecuteScalar()!;
        }

        public static void Validate(FuelTable table)
        {
            if (table.CarId <= 0)
                throw new InvalidOperationException("The fuel table has no car.");
            if (string.IsNullOrWhiteSpace(table.Name))
                throw new InvalidOperationException("The fuel table needs a name.");
            if (table.RpmAxis.Length < 2 || table.InjectionAxis.Length < 2)
                throw new InvalidOperationException("Both axes need at least 2 breakpoints.");
            if (!IsAscending(table.RpmAxis))
                throw new InvalidOperationException("RPM breakpoints must be increasing.");
            if (!IsAscending(table.InjectionAxis))
                throw new InvalidOperationException("Injection breakpoints must be increasing.");
            if (table.Values.Length != table.InjectionAxis.Length ||
                table.Values.Any(row => row is null || row.Length != table.RpmAxis.Length))
                throw new InvalidOperationException(
                    $"Values must be {table.InjectionAxis.Length} rows x {table.RpmAxis.Length} columns.");
        }

        private static bool IsAscending(double[] axis)
        {
            for (int i = 1; i < axis.Length; i++)
                if (axis[i] <= axis[i - 1])
                    return false;
            return true;
        }

        // ---------------- Log files ----------------

        public List<LogFile> GetLogFiles(long fuelTableId)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT Id, FuelTableId, FileName, RecordedAt, ImportedAt, RowCount, FileHash, GasTempCoef, ReducerTempCoef,
                       Content IS NOT NULL AS HasContent
                FROM LogFile WHERE FuelTableId = $Id ORDER BY RecordedAt
                """);
            Add(cmd, "$Id", fuelTableId);

            using var r = cmd.ExecuteReader();
            var list = new List<LogFile>();

            while (r.Read())
            {
                list.Add(new LogFile
                {
                    Id = r.GetInt64(0),
                    FuelTableId = r.GetInt64(1),
                    FileName = r.GetString(2),
                    RecordedAt = ReadDate(r, "RecordedAt"),
                    ImportedAt = ReadDate(r, "ImportedAt"),
                    RowCount = r.GetInt32(5),
                    FileHash = r.GetString(6),
                    GasTempCoef = ReadCoefficients(r, 7, DefaultGasCoefficients),
                    ReducerTempCoef = ReadCoefficients(r, 8, DefaultReducerCoefficients),
                    HasContent = r.GetBoolean(9),
                });
            }

            return list;
        }

        /// <summary>All rows of one log, in the order they were logged.</summary>
        public List<LogRow> GetLogRows(long logFileId)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT TimeMs, Rpm, Lambda1, Gas1, Benz1, Press, Map, TempReducer, TempGas, Level,
                       Slow1, Fast1, Ox1, Lambda2, Gas2, Benz2, Slow2, Fast2, Ox2, Marker, AutoMarker, EcuMarker
                FROM LogRow WHERE LogFileId = $Id ORDER BY RowIndex
                """);
            Add(cmd, "$Id", logFileId);

            using var r = cmd.ExecuteReader();
            var rows = new List<LogRow>();

            while (r.Read())
            {
                rows.Add(new LogRow
                {
                    TimeMs = r.GetInt32(0),
                    Rpm = r.GetInt32(1),
                    Lambda1 = r.GetDouble(2),
                    Gas1 = r.GetDouble(3),
                    Benz1 = r.GetDouble(4),
                    Press = r.GetDouble(5),
                    Map = r.GetDouble(6),
                    TempReducer = r.GetDouble(7),
                    TempGas = r.GetDouble(8),
                    Level = r.GetDouble(9),
                    Slow1 = r.GetDouble(10),
                    Fast1 = r.GetDouble(11),
                    Ox1 = r.GetDouble(12),
                    Lambda2 = r.GetDouble(13),
                    Gas2 = r.GetDouble(14),
                    Benz2 = r.GetDouble(15),
                    Slow2 = r.GetDouble(16),
                    Fast2 = r.GetDouble(17),
                    Ox2 = r.GetDouble(18),
                    Marker = r.GetInt32(19),
                    AutoMarker = r.GetInt32(20),
                    EcuMarker = r.GetInt32(21),
                });
            }

            return rows;
        }

        /// <summary>Changes the temperature tables stored for one log.</summary>
        public void UpdateLogCoefficients(long logFileId, int[] gas, int[] reducer)
        {
            using var c = Open();
            using var cmd = Command(c, "UPDATE LogFile SET GasTempCoef = $Gas, ReducerTempCoef = $Reducer WHERE Id = $Id");
            Add(cmd, "$Id", logFileId);
            Add(cmd, "$Gas", JsonSerializer.Serialize(ValidCoefficients(gas, Settings.GasTemperatureRanges.Length, "LPG")));
            Add(cmd, "$Reducer", JsonSerializer.Serialize(ValidCoefficients(reducer, Settings.ReductorTemperatureRanges.Length, "reducer")));
            cmd.ExecuteNonQuery();
        }

        /// <summary>Recording time and temperature tables of every log of a car (for "same as previous").</summary>
        public List<(DateTime RecordedAt, int[] Gas, int[] Reducer)> GetCarLogCoefficients(long carId)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT l.RecordedAt, l.GasTempCoef, l.ReducerTempCoef
                FROM LogFile l JOIN FuelTable t ON t.Id = l.FuelTableId
                WHERE t.CarId = $CarId
                ORDER BY l.RecordedAt
                """);
            Add(cmd, "$CarId", carId);

            using var r = cmd.ExecuteReader();
            var list = new List<(DateTime, int[], int[])>();
            while (r.Read())
                list.Add((ReadDate(r, "RecordedAt"),
                          ReadCoefficients(r, 1, DefaultGasCoefficients),
                          ReadCoefficients(r, 2, DefaultReducerCoefficients)));
            return list;
        }

        /// <summary>
        /// Save time and temperature tables of every .ml8 configuration of a car (for "same as previous":
        /// the tables of a configuration were in the ECU from the moment it was saved).
        /// </summary>
        public List<(DateTime SavedAt, int[] Gas, int[] Reducer)> GetCarConfigCoefficients(long carId)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT ConfigSavedAt, ConfigGasTempCoef, ConfigReducerTempCoef
                FROM FuelTable
                WHERE CarId = $CarId AND ConfigSavedAt IS NOT NULL
                  AND ConfigGasTempCoef IS NOT NULL AND ConfigReducerTempCoef IS NOT NULL
                ORDER BY ConfigSavedAt
                """);
            Add(cmd, "$CarId", carId);

            using var r = cmd.ExecuteReader();
            var list = new List<(DateTime, int[], int[])>();
            while (r.Read())
                list.Add((ReadDate(r, "ConfigSavedAt"),
                          ReadCoefficients(r, 1, DefaultGasCoefficients),
                          ReadCoefficients(r, 2, DefaultReducerCoefficients)));
            return list;
        }

        private static int[]? NullableCoefficients(SqliteDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : JsonSerializer.Deserialize<int[]>(r.GetString(i));
        }

        private static int[] ReadCoefficients(SqliteDataReader r, int ordinal, int[] fallback) =>
            r.IsDBNull(ordinal) ? fallback : JsonSerializer.Deserialize<int[]>(r.GetString(ordinal)) ?? fallback;

        /// <summary>One whole number per temperature band of LPG.Core's Settings.</summary>
        public static int[] ValidCoefficients(int[]? values, int bands, string name)
        {
            if (values is null || values.Length != bands)
                throw new InvalidOperationException($"The {name} temperature table needs {bands} values, one per band.");
            return values;
        }

        /// <summary>
        /// Where a file with this hash was already imported ("car / fuel table / file"), or null.
        /// </summary>
        public string? FindImported(string fileHash)
        {
            using var c = Open();
            using var cmd = Command(c, """
                SELECT c.Name || ' / ' || t.Name || ' / ' || l.FileName
                FROM LogFile l
                JOIN FuelTable t ON t.Id = l.FuelTableId
                JOIN Car c ON c.Id = t.CarId
                WHERE l.FileHash = $Hash
                """);
            Add(cmd, "$Hash", fileHash);
            return cmd.ExecuteScalar() as string;
        }

        /// <summary>Saves a log file and all its rows in one transaction.</summary>
        public void ImportLog(LogFile log, IReadOnlyList<LogRow> rows)
        {
            using var c = Open();
            using var tx = c.BeginTransaction();

            using (var cmd = Command(c, """
                INSERT INTO LogFile (FuelTableId, FileName, RecordedAt, ImportedAt, RowCount, FileHash, GasTempCoef, ReducerTempCoef, Content)
                VALUES ($FuelTableId, $FileName, $RecordedAt, $ImportedAt, $RowCount, $FileHash, $GasTempCoef, $ReducerTempCoef, $Content)
                RETURNING Id
                """, tx))
            {
                Add(cmd, "$FuelTableId", log.FuelTableId);
                Add(cmd, "$FileName", log.FileName);
                Add(cmd, "$RecordedAt", WriteDate(log.RecordedAt));
                Add(cmd, "$ImportedAt", WriteDate(log.ImportedAt));
                Add(cmd, "$RowCount", rows.Count);
                Add(cmd, "$FileHash", log.FileHash);
                Add(cmd, "$GasTempCoef", JsonSerializer.Serialize(ValidCoefficients(log.GasTempCoef, Settings.GasTemperatureRanges.Length, "LPG")));
                Add(cmd, "$ReducerTempCoef", JsonSerializer.Serialize(ValidCoefficients(log.ReducerTempCoef, Settings.ReductorTemperatureRanges.Length, "reducer")));
                Add(cmd, "$Content", log.Content is null ? null : Gzip(log.Content));
                log.Id = (long)cmd.ExecuteScalar()!;
                log.RowCount = rows.Count;
            }

            string[] names =
            [
                "LogFileId", "RowIndex", "TimeMs", "Rpm", "Lambda1", "Gas1", "Benz1", "Press", "Map",
                "TempReducer", "TempGas", "Level", "Slow1", "Fast1", "Ox1", "Lambda2", "Gas2", "Benz2",
                "Slow2", "Fast2", "Ox2", "Marker", "AutoMarker", "EcuMarker",
            ];

            using var insert = Command(c,
                $"INSERT INTO LogRow ({string.Join(", ", names)}) VALUES ({string.Join(", ", names.Select(n => "$" + n))})",
                tx);

            // No fixed SqliteType: each value is bound with the type of its C# value (int or double).
            var p = names.ToDictionary(n => n, n => insert.Parameters.Add(new SqliteParameter { ParameterName = "$" + n }));
            p["LogFileId"].Value = log.Id;
            insert.Prepare();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                p["RowIndex"].Value = i;
                p["TimeMs"].Value = row.TimeMs;
                p["Rpm"].Value = row.Rpm;
                p["Lambda1"].Value = row.Lambda1;
                p["Gas1"].Value = row.Gas1;
                p["Benz1"].Value = row.Benz1;
                p["Press"].Value = row.Press;
                p["Map"].Value = row.Map;
                p["TempReducer"].Value = row.TempReducer;
                p["TempGas"].Value = row.TempGas;
                p["Level"].Value = row.Level;
                p["Slow1"].Value = row.Slow1;
                p["Fast1"].Value = row.Fast1;
                p["Ox1"].Value = row.Ox1;
                p["Lambda2"].Value = row.Lambda2;
                p["Gas2"].Value = row.Gas2;
                p["Benz2"].Value = row.Benz2;
                p["Slow2"].Value = row.Slow2;
                p["Fast2"].Value = row.Fast2;
                p["Ox2"].Value = row.Ox2;
                p["Marker"].Value = row.Marker;
                p["AutoMarker"].Value = row.AutoMarker;
                p["EcuMarker"].Value = row.EcuMarker;
                insert.ExecuteNonQuery();
            }

            tx.Commit();
        }


        // ---------------- Delete ----------------
        // Foreign keys cascade: a car takes its fuel tables, a fuel table its logs, a log its rows.

        public void DeleteCar(long carId) => Delete("DELETE FROM Car WHERE Id = $Id", carId);

        public void DeleteFuelTable(long fuelTableId) => Delete("DELETE FROM FuelTable WHERE Id = $Id", fuelTableId);

        public void DeleteLogFile(long logFileId) => Delete("DELETE FROM LogFile WHERE Id = $Id", logFileId);

        /// <summary>Removes only the screenshot of a fuel table; its values stay.</summary>
        public void DeleteFuelTableImage(long fuelTableId) =>
            Delete("UPDATE FuelTable SET Image = NULL, ImageFileName = NULL WHERE Id = $Id", fuelTableId);

        private void Delete(string sql, long id)
        {
            using (var c = Open())
            using (var cmd = Command(c, sql))
            {
                Add(cmd, "$Id", id);
                cmd.ExecuteNonQuery();
            }

            // Give the freed space back, so the file shrinks after deleting logs.
            using var vacuum = Open();
            Execute(vacuum, "VACUUM");
        }

        // ---------------- Helpers ----------------

        private static SqliteCommand Command(SqliteConnection c, string sql, SqliteTransaction? tx = null)
        {
            var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = tx;
            return cmd;
        }

        private static void Execute(SqliteConnection c, string sql, SqliteTransaction? tx = null)
        {
            using var cmd = Command(c, sql, tx);
            cmd.ExecuteNonQuery();
        }

        private static object? Scalar(SqliteConnection c, string sql)
        {
            using var cmd = Command(c, sql);
            return cmd.ExecuteScalar();
        }

        private static void Add(SqliteCommand cmd, string name, object? value) =>
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        private static string? NullableString(SqliteDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        // Dates are stored as ISO 8601 text, so they sort correctly and read the same in any culture.
        private static string WriteDate(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        private static DateTime ReadDate(SqliteDataReader r, string column) =>
            DateTime.ParseExact(r.GetString(r.GetOrdinal(column)), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
