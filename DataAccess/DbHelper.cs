using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace ISO11820.DataAccess
{
    public class DbHelper
    {
        private readonly string _connStr;

        public DbHelper(string dbPath)
        {
            _connStr = $"Data Source={dbPath}";
        }

        public void InitializeDatabase()
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();

            string[] createTables = new[]
            {
                @"CREATE TABLE IF NOT EXISTS operators (
                    userid    TEXT NOT NULL,
                    username  TEXT NOT NULL,
                    pwd       TEXT NOT NULL,
                    usertype  TEXT NOT NULL
                );",

                @"CREATE TABLE IF NOT EXISTS apparatus (
                    apparatusid   INTEGER NOT NULL CONSTRAINT PK_apparatus PRIMARY KEY,
                    innernumber   TEXT NOT NULL,
                    apparatusname TEXT NOT NULL,
                    checkdatef    date NOT NULL,
                    checkdatet    date NOT NULL,
                    pidport       TEXT NOT NULL,
                    powerport     TEXT NOT NULL,
                    constpower    INTEGER NULL
                );",

                @"CREATE TABLE IF NOT EXISTS productmaster (
                    productid   TEXT NOT NULL CONSTRAINT PK_productmaster PRIMARY KEY,
                    productname TEXT NOT NULL,
                    specific    TEXT NOT NULL,
                    diameter    REAL NOT NULL,
                    height      REAL NOT NULL,
                    flag        TEXT NULL
                );",

                @"CREATE TABLE IF NOT EXISTS testmaster (
                    productid        TEXT NOT NULL,
                    testid           TEXT NOT NULL,
                    testdate         date NOT NULL,
                    ambtemp          REAL NOT NULL,
                    ambhumi          REAL NOT NULL,
                    according        TEXT NOT NULL,
                    operator         TEXT NOT NULL,
                    apparatusid      TEXT NOT NULL,
                    apparatusname    TEXT NOT NULL,
                    apparatuschkdate date NOT NULL,
                    rptno            TEXT NOT NULL,
                    preweight        REAL NOT NULL,
                    postweight       REAL NOT NULL,
                    lostweight       REAL NOT NULL,
                    lostweight_per   REAL NOT NULL,
                    totaltesttime    INTEGER NOT NULL,
                    constpower       INTEGER NOT NULL,
                    phenocode        TEXT NOT NULL,
                    flametime        INTEGER NOT NULL,
                    flameduration    INTEGER NOT NULL,
                    maxtf1           REAL NOT NULL,
                    maxtf2           REAL NOT NULL,
                    maxts            REAL NOT NULL,
                    maxtc            REAL NOT NULL,
                    maxtf1_time      INTEGER NOT NULL,
                    maxtf2_time      INTEGER NOT NULL,
                    maxts_time       INTEGER NOT NULL,
                    maxtc_time       INTEGER NOT NULL,
                    finaltf1         REAL NOT NULL,
                    finaltf2         REAL NOT NULL,
                    finalts          REAL NOT NULL,
                    finaltc          REAL NOT NULL,
                    finaltf1_time    INTEGER NOT NULL,
                    finaltf2_time    INTEGER NOT NULL,
                    finalts_time     INTEGER NOT NULL,
                    finaltc_time     INTEGER NOT NULL,
                    deltatf1         REAL NOT NULL,
                    deltatf2         REAL NOT NULL,
                    deltatf          REAL NOT NULL,
                    deltats          REAL NOT NULL,
                    deltatc          REAL NOT NULL,
                    memo             TEXT NULL,
                    flag             TEXT NULL,
                    CONSTRAINT PK_testmaster PRIMARY KEY (productid, testid),
                    CONSTRAINT FK_testmaster_productmaster FOREIGN KEY (productid) REFERENCES productmaster (productid)
                );",

                @"CREATE TABLE IF NOT EXISTS sensors (
                    sensorid    INTEGER NOT NULL CONSTRAINT PK_sensors PRIMARY KEY,
                    sensorname  TEXT NOT NULL,
                    dispname    TEXT NOT NULL,
                    sensorgroup TEXT NOT NULL,
                    unit        TEXT NOT NULL,
                    discription TEXT NOT NULL,
                    flag        TEXT NOT NULL,
                    signalzero  REAL NOT NULL,
                    signalspan  REAL NOT NULL,
                    outputzero  REAL NOT NULL,
                    outputspan  REAL NOT NULL,
                    outputvalue REAL NOT NULL,
                    inputvalue  REAL NOT NULL,
                    signaltype  INTEGER NOT NULL
                );",

                @"CREATE TABLE IF NOT EXISTS CalibrationRecords (
                    Id                 TEXT NOT NULL CONSTRAINT PK_CalibrationRecords PRIMARY KEY,
                    CalibrationDate    TEXT NOT NULL,
                    CalibrationType    TEXT NOT NULL,
                    ApparatusId        INTEGER NOT NULL,
                    Operator           TEXT NOT NULL,
                    TemperatureData    TEXT NOT NULL,
                    UniformityResult   REAL NULL,
                    MaxDeviation       REAL NULL,
                    AverageTemperature REAL NULL,
                    PassedCriteria     INTEGER NOT NULL,
                    Remarks            TEXT NOT NULL,
                    CreatedAt          TEXT NOT NULL,
                    TempA1 REAL NULL, TempA2 REAL NULL, TempA3 REAL NULL,
                    TempB1 REAL NULL, TempB2 REAL NULL, TempB3 REAL NULL,
                    TempC1 REAL NULL, TempC2 REAL NULL, TempC3 REAL NULL,
                    TAvg        REAL NULL,
                    TAvgAxis1   REAL NULL, TAvgAxis2 REAL NULL, TAvgAxis3 REAL NULL,
                    TAvgLevela  REAL NULL, TAvgLevelb REAL NULL, TAvgLevelc REAL NULL,
                    TDevAxis1   REAL NULL, TDevAxis2 REAL NULL, TDevAxis3 REAL NULL,
                    TDevLevela  REAL NULL, TDevLevelb REAL NULL, TDevLevelc REAL NULL,
                    TAvgDevAxis REAL NULL, TAvgDevLevel REAL NULL,
                    CenterTempData TEXT NULL,
                    Memo           TEXT NULL
                );",

                @"CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate ON testmaster (testdate);",
                @"CREATE INDEX IF NOT EXISTS IX_Testmaster_Operator ON testmaster (operator);",
                @"CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate_Productid ON testmaster (testdate, productid);",
                @"CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Date ON CalibrationRecords (CalibrationDate);",
                @"CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Operator ON CalibrationRecords (Operator);"
            };

            foreach (var sql in createTables)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            InsertInitialData(conn);
        }

        private void InsertInitialData(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO operators (userid, username, pwd, usertype)
                    SELECT '1', 'admin', '123456', 'admin'
                    WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'admin');";
                cmd.ExecuteNonQuery();
            }
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO operators (userid, username, pwd, usertype)
                    SELECT '2', 'experimenter', '123456', 'operator'
                    WHERE NOT EXISTS (SELECT 1 FROM operators WHERE username = 'experimenter');";
                cmd.ExecuteNonQuery();
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO apparatus (apparatusid, innernumber, apparatusname, checkdatef, checkdatet, pidport, powerport, constpower)
                    SELECT 0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048
                    WHERE NOT EXISTS (SELECT 1 FROM apparatus WHERE apparatusid = 0);";
                cmd.ExecuteNonQuery();
            }

            string[] sensorInserts = new[]
            {
                "INSERT OR IGNORE INTO sensors VALUES (0,'Sensor0','炉温1','采集','℃','炉温1','启用',0,0,0,1000,0,0,4);",
                "INSERT OR IGNORE INTO sensors VALUES (1,'Sensor1','炉温2','采集','℃','炉温2','启用',0,0,0,1000,0,0,4);",
                "INSERT OR IGNORE INTO sensors VALUES (2,'Sensor2','表面温度','采集','℃','表面温度','启用',0,0,0,1000,0,0,4);",
                "INSERT OR IGNORE INTO sensors VALUES (3,'Sensor3','中心温度','采集','℃','中心温度','启用',0,0,0,1000,0,0,4);",
                "INSERT OR IGNORE INTO sensors VALUES (16,'Sensor16','校准温度','校准','℃','校准温度','启用',0,0,0,1000,0,0,4);"
            };

            foreach (var sql in sensorInserts)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        public bool Login(string username, string pwd, out string userid, out string usertype)
        {
            userid = ""; usertype = "";
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT userid, usertype FROM operators WHERE username=$name AND pwd=$pwd";
            cmd.Parameters.AddWithValue("$name", username);
            cmd.Parameters.AddWithValue("$pwd", pwd);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                userid = reader.GetString(0);
                usertype = reader.GetString(1);
                return true;
            }
            return false;
        }

        public List<(string Username, string Usertype)> GetAllOperators()
        {
            var list = new List<(string, string)>();
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username, usertype FROM operators";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add((reader.GetString(0), reader.GetString(1)));
            }
            return list;
        }

        public (string InnerNumber, string Name, DateTime CheckDate, int ConstPower)? GetApparatus(int id = 0)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT innernumber, apparatusname, checkdatef, constpower FROM apparatus WHERE apparatusid=$id";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    reader.GetString(0),
                    reader.GetString(1),
                    DateTime.Parse(reader.GetString(2)),
                    reader.IsDBNull(3) ? 2048 : reader.GetInt32(3)
                );
            }
            return null;
        }

        public void InsertTest(string productId, string testId, string operatorName,
                               double preweight, double ambtemp, double ambhumi,
                               string apparatusId, string apparatusName, DateTime apparatusChkDate)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO testmaster
                    (productid, testid, testdate, operator, ambtemp, ambhumi,
                     according, apparatusid, apparatusname, apparatuschkdate, rptno,
                     preweight, postweight, lostweight, lostweight_per,
                     totaltesttime, constpower, phenocode, flametime, flameduration,
                     maxtf1,maxtf2,maxts,maxtc,
                     maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
                     finaltf1,finaltf2,finalts,finaltc,
                     finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
                     deltatf1,deltatf2,deltatf,deltats,deltatc)
                VALUES
                    ($pid,$tid,date('now'),$op,$ambtemp,$ambhumi,
                     'ISO 11820:2022',$appid,$appname,$appchkdate,$rptno,
                     $prewt,0,0,0,
                     0,0,'',0,0,
                     0,0,0,0,0,0,0,0,
                     0,0,0,0,0,0,0,0,
                     0,0,0,0,0)";
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            cmd.Parameters.AddWithValue("$op", operatorName);
            cmd.Parameters.AddWithValue("$ambtemp", ambtemp);
            cmd.Parameters.AddWithValue("$ambhumi", ambhumi);
            cmd.Parameters.AddWithValue("$appid", apparatusId);
            cmd.Parameters.AddWithValue("$appname", apparatusName);
            cmd.Parameters.AddWithValue("$appchkdate", apparatusChkDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$rptno", productId);
            cmd.Parameters.AddWithValue("$prewt", preweight);
            cmd.ExecuteNonQuery();
        }

        public void UpdateTestResult(string productId, string testId,
                                     double postweight, double lostweight, double lostPer,
                                     double deltaTf, int totalTime, string phenocode,
                                     int flameTime, int flameDuration,
                                     double maxTf1, double maxTf2, double maxTs, double maxTc,
                                     double finalTf1, double finalTf2, double finalTs, double finalTc,
                                     double deltaTf1, double deltaTf2, double deltaTs, double deltaTc,
                                     string memo = "")
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE testmaster SET
                    postweight      = $post,
                    lostweight      = $lost,
                    lostweight_per  = $lostper,
                    deltatf         = $dtf,
                    totaltesttime   = $time,
                    phenocode       = $pheno,
                    flametime       = $flametime,
                    flameduration   = $flamedur,
                    flag            = '10000000',
                    maxtf1 = $maxtf1, maxtf2 = $maxtf2, maxts = $maxts, maxtc = $maxtc,
                    maxtf1_time = $time, maxtf2_time = $time, maxts_time = $time, maxtc_time = $time,
                    finaltf1 = $finaltf1, finaltf2 = $finaltf2, finalts = $finalts, finaltc = $finaltc,
                    finaltf1_time = $time, finaltf2_time = $time, finalts_time = $time, finaltc_time = $time,
                    deltatf1 = $deltatf1, deltatf2 = $deltatf2, deltats = $deltats, deltatc = $deltatc,
                    memo = $memo
                WHERE productid=$pid AND testid=$tid";
            cmd.Parameters.AddWithValue("$post", postweight);
            cmd.Parameters.AddWithValue("$lost", lostweight);
            cmd.Parameters.AddWithValue("$lostper", lostPer);
            cmd.Parameters.AddWithValue("$dtf", deltaTf);
            cmd.Parameters.AddWithValue("$time", totalTime);
            cmd.Parameters.AddWithValue("$pheno", phenocode);
            cmd.Parameters.AddWithValue("$flametime", flameTime);
            cmd.Parameters.AddWithValue("$flamedur", flameDuration);
            cmd.Parameters.AddWithValue("$maxtf1", maxTf1);
            cmd.Parameters.AddWithValue("$maxtf2", maxTf2);
            cmd.Parameters.AddWithValue("$maxts", maxTs);
            cmd.Parameters.AddWithValue("$maxtc", maxTc);
            cmd.Parameters.AddWithValue("$finaltf1", finalTf1);
            cmd.Parameters.AddWithValue("$finaltf2", finalTf2);
            cmd.Parameters.AddWithValue("$finalts", finalTs);
            cmd.Parameters.AddWithValue("$finaltc", finalTc);
            cmd.Parameters.AddWithValue("$deltatf1", deltaTf1);
            cmd.Parameters.AddWithValue("$deltatf2", deltaTf2);
            cmd.Parameters.AddWithValue("$deltats", deltaTs);
            cmd.Parameters.AddWithValue("$deltatc", deltaTc);
            cmd.Parameters.AddWithValue("$memo", memo);
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            cmd.ExecuteNonQuery();
        }

        public List<Dictionary<string, object>> QueryTests(DateTime fromDate, DateTime toDate, string productId = "", string operatorName = "")
        {
            var result = new List<Dictionary<string, object>>();
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT testid, productid, testdate, operator, totaltesttime, flag
                FROM testmaster
                WHERE testdate BETWEEN $from AND $to
                  AND ($pid = '' OR productid LIKE '%' || $pid || '%')
                  AND ($op = '' OR operator = $op)
                ORDER BY testdate DESC";
            cmd.Parameters.AddWithValue("$from", fromDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$to", toDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$op", operatorName);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }
            return result;
        }

        public Dictionary<string, object>? GetTestDetail(string productId, string testId)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM testmaster WHERE productid=$pid AND testid=$tid";
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$tid", testId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                return row;
            }
            return null;
        }

        public void InsertProduct(string productId, string productName, string specific, double diameter, double height)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO productmaster (productid, productname, specific, diameter, height, flag)
                VALUES ($pid, $name, $spec, $dia, $height, '')";
            cmd.Parameters.AddWithValue("$pid", productId);
            cmd.Parameters.AddWithValue("$name", productName);
            cmd.Parameters.AddWithValue("$spec", specific);
            cmd.Parameters.AddWithValue("$dia", diameter);
            cmd.Parameters.AddWithValue("$height", height);
            cmd.ExecuteNonQuery();
        }
    }
}