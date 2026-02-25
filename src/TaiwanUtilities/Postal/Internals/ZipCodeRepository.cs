// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Data.Sqlite;

/// <summary>
/// 郵遞區號資料存取層（Repository）
/// 負責 SQLite 資料庫的索引建立、查詢和管理
/// </summary>
internal class ZipCodeRepository : IDisposable
{
    private readonly string _dbPath;
    private readonly bool _keepAlive;
    private SqliteConnection? _conn;

    /// <summary>
    /// 靜態建構子：初始化 SQLite provider
    /// </summary>
    static ZipCodeRepository()
    {
        SQLitePCL.Batteries.Init();
    }

    internal ZipCodeRepository(string dbPath, bool keepAlive = false)
    {
        _dbPath = dbPath;
        _keepAlive = keepAlive;
    }

    /// <summary>
    /// 建立資料表
    /// </summary>
    internal void CreateTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();

        // 精確匹配表（包含 road_type, department 和 office 欄位）
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS precise (
                addr_str TEXT,
                rule_str TEXT,
                zipcode TEXT,
                road_type TEXT,
                department TEXT,
                office TEXT,
                rule_id INTEGER,
                PRIMARY KEY (addr_str, rule_str)
            )";
        cmd.ExecuteNonQuery();

        // 漸進式匹配表
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS gradual (
                addr_str TEXT PRIMARY KEY,
                zipcode TEXT
            )";
        cmd.ExecuteNonQuery();

        // 新增：結構化投遞規則表（保存所有 DBF 欄位）
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS postal_rules (
                id INTEGER PRIMARY KEY AUTOINCREMENT,

                -- 基本地址組件
                zipcode TEXT NOT NULL,
                city TEXT NOT NULL,
                area TEXT NOT NULL,
                road TEXT NOT NULL,

                -- 巷弄範圍（DBF: LANE/LANE1）
                lane_start INTEGER,
                lane_end INTEGER,

                -- 弄範圍（DBF: ALLEY/ALLEY1）
                alley_start INTEGER,
                alley_end INTEGER,

                -- 門牌號碼範圍（DBF: NO_BGN/NO_BGN1/NO_END/NO_END1）
                number_start INTEGER,
                number_start_sub INTEGER,
                number_end INTEGER,
                number_end_sub INTEGER,

                -- 單雙號規則（DBF: EVEN）
                even_odd INTEGER,

                -- 樓層資訊（DBF: FLOOR/FLOOR1）- 僅存儲不驗證
                floor_start INTEGER,
                floor_end INTEGER,

                -- 其他欄位
                scope TEXT,
                department TEXT,
                office TEXT,
                remark TEXT,

                -- 未知欄位（保留但不使用）
                road_no TEXT,
                road1 TEXT,
                lane11 INTEGER,
                lane22 INTEGER,
                alley11 INTEGER,
                alley22 INTEGER
            )";
        cmd.ExecuteNonQuery();

        // 建立索引（優化查詢效能）
        cmd.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_postal_rules_location
                ON postal_rules(city, area, road);
            CREATE INDEX IF NOT EXISTS idx_postal_rules_zipcode
                ON postal_rules(zipcode);
            CREATE INDEX IF NOT EXISTS idx_postal_rules_lane
                ON postal_rules(lane_start, lane_end);
            CREATE INDEX IF NOT EXISTS idx_postal_rules_number
                ON postal_rules(number_start, number_end);
            CREATE INDEX IF NOT EXISTS idx_precise_rule_id
                ON precise(rule_id);
        ";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 取得兩個郵遞區號的公共前綴（避免產生 4 碼等不標準長度）
    /// </summary>
    internal static string? GetCommonPart(string? strA, string? strB)
    {
        if (strA == null) return strB;
        if (strB == null) return strA;

        var minLen = Math.Min(strA.Length, strB.Length);
        var i = 0;

        for (; i < minLen; i++)
        {
            if (strA[i] != strB[i])
                break;
        }

        // 台灣郵遞區號格式：3碼（區域）、5碼（3+2）、6碼（3+2+1）
        // 避免產生 4 碼：如果共同前綴是 4 碼，截取到 3 碼
        if (i >= 6) return strA.Substring(0, 6);  // 完整 6 碼
        if (i == 5) return strA.Substring(0, 5);  // 5 碼（標準）
        if (i == 4) return strA.Substring(0, 3);  // 4 碼 → 截取到 3 碼（避免不標準長度）
        if (i >= 3) return strA.Substring(0, 3);  // 3 碼（區域碼）

        // 共同前綴 < 3 碼（1或2碼），保留原樣以支持漸進式查詢（如："1"代表臺北）
        return strA.Substring(0, i);
    }

    /// <summary>
    /// 插入精確匹配記錄
    /// </summary>
    internal void PutPrecise(SqliteConnection conn, string addrStr, string ruleStr, string zipcode, string roadType = "Unknown", string department = "", string office = "", long? ruleId = null)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO precise VALUES ($addr_str, $rule_str, $zipcode, $road_type, $department, $office, $rule_id)";
        cmd.Parameters.AddWithValue("$addr_str", addrStr);
        cmd.Parameters.AddWithValue("$rule_str", ruleStr);
        cmd.Parameters.AddWithValue("$zipcode", zipcode);
        cmd.Parameters.AddWithValue("$road_type", roadType);
        cmd.Parameters.AddWithValue("$department", department);
        cmd.Parameters.AddWithValue("$office", office);
        cmd.Parameters.AddWithValue("$rule_id", ruleId.HasValue ? (object)ruleId.Value : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 插入或更新漸進式匹配記錄
    /// </summary>
    internal void PutGradual(SqliteConnection conn, string addrStr, string zipcode)
    {
        // 查詢現有記錄
        string? storedZipcode = null;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT zipcode FROM gradual WHERE addr_str = $addr_str";
            cmd.Parameters.AddWithValue("$addr_str", addrStr);
            var result = cmd.ExecuteScalar();
            if (result != null)
                storedZipcode = result.ToString();
        }

        // 更新或插入
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "REPLACE INTO gradual VALUES ($addr_str, $zipcode)";
            cmd.Parameters.AddWithValue("$addr_str", addrStr);
            cmd.Parameters.AddWithValue("$zipcode", GetCommonPart(storedZipcode, zipcode) ?? string.Empty);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 加入一筆地址規則到索引
    /// </summary>
    internal void Put(SqliteConnection conn, string headAddrStr, string tailRuleStr, string zipcode, string roadName = "", string department = "", string office = "", long? ruleId = null)
    {
        var addr = new AddressTokenizer(headAddrStr);

        // 分類路名類型
        var roadType = string.IsNullOrEmpty(roadName)
            ? PostalRoadType.Unknown.ToString()
            : PostalRoadClassifier.Classify(roadName).ToString();

        // 插入精確匹配
        PutPrecise(conn, addr.Flat(), headAddrStr + tailRuleStr, zipcode, roadType, department, office, ruleId);

        // 插入所有連續子序列到漸進式索引
        // (a, b, c) -> (a,); (a, b); (a, b, c); (b,); (b, c); (c,)
        var lenTokens = addr.Tokens.Count;
        for (var f = 0; f < lenTokens; f++)
        {
            for (var l = f; l < lenTokens; l++)
            {
                PutGradual(conn, addr.Flat(f, l + 1), zipcode);
            }
        }

        // 對於 3 個以上的 tokens，加入跳躍索引 (a, b, c, d) -> (a, c)
        if (lenTokens >= 3)
        {
            PutGradual(conn, addr.PickToFlat(0, 2), zipcode);
        }
    }

    /// <summary>
    /// 插入結構化投遞規則記錄
    /// </summary>
    internal long PutPostalRule(SqliteConnection conn, PostalRuleData rule)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO postal_rules (
                zipcode, city, area, road,
                lane_start, lane_end, alley_start, alley_end,
                number_start, number_start_sub, number_end, number_end_sub,
                even_odd, floor_start, floor_end,
                scope, department, office, remark,
                road_no, road1, lane11, lane22, alley11, alley22
            ) VALUES (
                $zipcode, $city, $area, $road,
                $lane_start, $lane_end, $alley_start, $alley_end,
                $number_start, $number_start_sub, $number_end, $number_end_sub,
                $even_odd, $floor_start, $floor_end,
                $scope, $department, $office, $remark,
                $road_no, $road1, $lane11, $lane22, $alley11, $alley22
            )";

        cmd.Parameters.AddWithValue("$zipcode", rule.ZipCode);
        cmd.Parameters.AddWithValue("$city", rule.City);
        cmd.Parameters.AddWithValue("$area", rule.Area);
        cmd.Parameters.AddWithValue("$road", rule.Road);

        cmd.Parameters.AddWithValue("$lane_start", ToDbValue(rule.LaneStart));
        cmd.Parameters.AddWithValue("$lane_end", ToDbValue(rule.LaneEnd));
        cmd.Parameters.AddWithValue("$alley_start", ToDbValue(rule.AlleyStart));
        cmd.Parameters.AddWithValue("$alley_end", ToDbValue(rule.AlleyEnd));

        cmd.Parameters.AddWithValue("$number_start", ToDbValue(rule.NumberStart));
        cmd.Parameters.AddWithValue("$number_start_sub", ToDbValue(rule.NumberStartSub));
        cmd.Parameters.AddWithValue("$number_end", ToDbValue(rule.NumberEnd));
        cmd.Parameters.AddWithValue("$number_end_sub", ToDbValue(rule.NumberEndSub));

        cmd.Parameters.AddWithValue("$even_odd", ToDbValue(rule.EvenOdd));
        cmd.Parameters.AddWithValue("$floor_start", ToDbValue(rule.FloorStart));
        cmd.Parameters.AddWithValue("$floor_end", ToDbValue(rule.FloorEnd));

        cmd.Parameters.AddWithValue("$scope", rule.Scope ?? string.Empty);
        cmd.Parameters.AddWithValue("$department", rule.Department ?? string.Empty);
        cmd.Parameters.AddWithValue("$office", rule.Office ?? string.Empty);
        cmd.Parameters.AddWithValue("$remark", rule.Remark ?? string.Empty);

        cmd.Parameters.AddWithValue("$road_no", rule.RoadNo ?? string.Empty);
        cmd.Parameters.AddWithValue("$road1", rule.Road1 ?? string.Empty);
        cmd.Parameters.AddWithValue("$lane11", ToDbValue(rule.Lane11));
        cmd.Parameters.AddWithValue("$lane22", ToDbValue(rule.Lane22));
        cmd.Parameters.AddWithValue("$alley11", ToDbValue(rule.Alley11));
        cmd.Parameters.AddWithValue("$alley22", ToDbValue(rule.Alley22));

        cmd.ExecuteNonQuery();

        // 返回插入記錄的 ID
        cmd.CommandText = "SELECT last_insert_rowid()";
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    /// <summary>
    /// 查詢結構化投遞規則（內部方法，使用現有連接）
    /// </summary>
    private List<PostalRule> QueryPostalRulesInternal(SqliteConnection conn, string city, string area, string road)
    {
        var rules = new List<PostalRule>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT * FROM postal_rules
            WHERE city = $city AND area = $area AND road = $road
            ORDER BY id";

        cmd.Parameters.AddWithValue("$city", city);
        cmd.Parameters.AddWithValue("$area", area);
        cmd.Parameters.AddWithValue("$road", road);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rules.Add(MapToPostalRule(reader));
        }

        return rules;
    }

    /// <summary>
    /// 查詢結構化投遞規則（公開方法）
    /// </summary>
    internal List<PostalRule> QueryPostalRules(string city, string area, string road)
    {
        return WithinTransaction(conn => QueryPostalRulesInternal(conn, city, area, road));
    }

    /// <summary>
    /// 查詢指定區域的所有投遞規則
    /// </summary>
    internal List<PostalRule> QueryPostalRulesByArea(string city, string area)
    {
        return WithinTransaction(conn =>
        {
            var rules = new List<PostalRule>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM postal_rules
                WHERE city = $city AND area = $area
                ORDER BY road, id
                LIMIT 1000";

            cmd.Parameters.AddWithValue("$city", city);
            cmd.Parameters.AddWithValue("$area", area);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rules.Add(MapToPostalRule(reader));
            }

            return rules;
        });
    }

    /// <summary>
    /// 查詢指定城市的所有投遞規則
    /// </summary>
    internal List<PostalRule> QueryPostalRulesByCity(string city)
    {
        return WithinTransaction(conn =>
        {
            var rules = new List<PostalRule>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM postal_rules
                WHERE city = $city
                ORDER BY area, road, id
                LIMIT 1000";

            cmd.Parameters.AddWithValue("$city", city);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rules.Add(MapToPostalRule(reader));
            }

            return rules;
        });
    }

    /// <summary>
    /// 隨機查詢投遞規則
    /// </summary>
    internal List<PostalRule> QueryRandomPostalRules(int count)
    {
        return WithinTransaction(conn =>
        {
            var rules = new List<PostalRule>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM postal_rules
                ORDER BY RANDOM()
                LIMIT $count";

            cmd.Parameters.AddWithValue("$count", count);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                rules.Add(MapToPostalRule(reader));
            }

            return rules;
        });
    }

    /// <summary>
    /// 載入所有特殊路名（不以 路/街/道 結尾）- 內部方法
    /// </summary>
    private HashSet<string> LoadSpecialRoadNamesInternal(SqliteConnection conn)
    {
        var specialRoads = new HashSet<string>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT road FROM postal_rules
            WHERE road NOT LIKE '%路'
              AND road NOT LIKE '%街'
              AND road NOT LIKE '%道'";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            specialRoads.Add(reader.GetString(0));
        }

        return specialRoads;
    }

    /// <summary>
    /// 載入所有特殊路名（不以 路/街/道 結尾）- 公開方法
    /// </summary>
    internal HashSet<string> LoadSpecialRoadNames()
    {
        return WithinTransaction(conn => LoadSpecialRoadNamesInternal(conn));
    }

    /// <summary>
    /// 載入所有投遞規則（用於 PostalRulesEngine）
    /// </summary>
    /// <remarks>
    /// 此方法會載入資料庫中的所有 postal_rules 記錄，
    /// 用於 PostalRulesEngine 的規則預載入。
    /// 載入時間約 50-100ms（取決於資料庫大小）。
    /// </remarks>
    internal List<PostalRule> LoadAllPostalRules()
    {
        return WithinTransaction(conn =>
        {
            var rules = new List<PostalRule>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM postal_rules
                ORDER BY city, area, road, id";

            using var reader = cmd.ExecuteReader();

            // 快取所有欄位的 ordinal，避免每行重複查詢（80,000+ 行 × 26 欄）
            var ordinals = new PostalRuleOrdinals(reader);

            while (reader.Read())
            {
                rules.Add(MapToPostalRule(reader, ordinals));
            }

            return rules;
        });
    }

    /// <summary>
    /// 快取 postal_rules 所有欄位的 ordinal 索引
    /// </summary>
    private readonly struct PostalRuleOrdinals
    {
        public readonly int Id, ZipCode, City, Area, Road;
        public readonly int LaneStart, LaneEnd, AlleyStart, AlleyEnd;
        public readonly int NumberStart, NumberStartSub, NumberEnd, NumberEndSub;
        public readonly int EvenOdd, FloorStart, FloorEnd;
        public readonly int Scope, Department, Office, Remark;
        public readonly int RoadNo, Road1, Lane11, Lane22, Alley11, Alley22;

        public PostalRuleOrdinals(SqliteDataReader reader)
        {
            Id = reader.GetOrdinal("id");
            ZipCode = reader.GetOrdinal("zipcode");
            City = reader.GetOrdinal("city");
            Area = reader.GetOrdinal("area");
            Road = reader.GetOrdinal("road");
            LaneStart = reader.GetOrdinal("lane_start");
            LaneEnd = reader.GetOrdinal("lane_end");
            AlleyStart = reader.GetOrdinal("alley_start");
            AlleyEnd = reader.GetOrdinal("alley_end");
            NumberStart = reader.GetOrdinal("number_start");
            NumberStartSub = reader.GetOrdinal("number_start_sub");
            NumberEnd = reader.GetOrdinal("number_end");
            NumberEndSub = reader.GetOrdinal("number_end_sub");
            EvenOdd = reader.GetOrdinal("even_odd");
            FloorStart = reader.GetOrdinal("floor_start");
            FloorEnd = reader.GetOrdinal("floor_end");
            Scope = reader.GetOrdinal("scope");
            Department = reader.GetOrdinal("department");
            Office = reader.GetOrdinal("office");
            Remark = reader.GetOrdinal("remark");
            RoadNo = reader.GetOrdinal("road_no");
            Road1 = reader.GetOrdinal("road1");
            Lane11 = reader.GetOrdinal("lane11");
            Lane22 = reader.GetOrdinal("lane22");
            Alley11 = reader.GetOrdinal("alley11");
            Alley22 = reader.GetOrdinal("alley22");
        }
    }

    /// <summary>
    /// 輔助方法：將 nullable int 轉換為資料庫值
    /// </summary>
    private static object ToDbValue(int? value)
    {
        return value.HasValue ? (object)value.Value : DBNull.Value;
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 映射到 PostalRule（使用快取的 ordinal）
    /// </summary>
    private static PostalRule MapToPostalRule(SqliteDataReader reader, in PostalRuleOrdinals ord)
    {
        return new PostalRule
        {
            Id = reader.GetInt64(ord.Id),
            ZipCode = reader.GetString(ord.ZipCode),
            City = reader.GetString(ord.City),
            Area = reader.GetString(ord.Area),
            Road = reader.GetString(ord.Road),

            LaneStart = GetNullableInt(reader, ord.LaneStart),
            LaneEnd = GetNullableInt(reader, ord.LaneEnd),
            AlleyStart = GetNullableInt(reader, ord.AlleyStart),
            AlleyEnd = GetNullableInt(reader, ord.AlleyEnd),

            NumberStart = GetNullableInt(reader, ord.NumberStart),
            NumberStartSub = GetNullableInt(reader, ord.NumberStartSub),
            NumberEnd = GetNullableInt(reader, ord.NumberEnd),
            NumberEndSub = GetNullableInt(reader, ord.NumberEndSub),

            EvenOdd = GetNullableInt(reader, ord.EvenOdd),
            FloorStart = GetNullableInt(reader, ord.FloorStart),
            FloorEnd = GetNullableInt(reader, ord.FloorEnd),

            Scope = GetNullableString(reader, ord.Scope),
            Department = GetNullableString(reader, ord.Department),
            Office = GetNullableString(reader, ord.Office),
            Remark = GetNullableString(reader, ord.Remark),

            RoadNo = GetNullableString(reader, ord.RoadNo),
            Road1 = GetNullableString(reader, ord.Road1),
            Lane11 = GetNullableInt(reader, ord.Lane11),
            Lane22 = GetNullableInt(reader, ord.Lane22),
            Alley11 = GetNullableInt(reader, ord.Alley11),
            Alley22 = GetNullableInt(reader, ord.Alley22)
        };
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 映射到 PostalRule（單行查詢用，自動解析 ordinal）
    /// </summary>
    private static PostalRule MapToPostalRule(SqliteDataReader reader)
    {
        return MapToPostalRule(reader, new PostalRuleOrdinals(reader));
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 讀取 nullable int（使用快取的 ordinal）
    /// </summary>
    private static int? GetNullableInt(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 讀取 nullable int
    /// </summary>
    private static int? GetNullableInt(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 讀取 nullable string（使用快取的 ordinal）
    /// </summary>
    private static string? GetNullableString(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        var value = reader.GetString(ordinal);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// 輔助方法：從 DbDataReader 讀取 nullable string
    /// </summary>
    private static string? GetNullableString(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;
        var value = reader.GetString(ordinal);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    /// 從 CSV 資料載入索引
    /// </summary>
    internal void LoadFromCsv(IEnumerable<string[]> csvRows)
    {
        WithinTransaction(conn =>
        {
            CreateTables(conn);

            foreach (var row in csvRows)
            {
                if (row.Length < 2) continue;

                var zipcode = row[0];

                // 新格式：[zipcode, city, area, road, scope, department, office]
                // 中格式：[zipcode, city, area, road, scope, department]
                // 舊格式：[zipcode, city, area, road, scope]
                var hasOffice = row.Length >= 7;
                var hasDepartment = row.Length >= 6;

                var headAddr = (hasOffice || hasDepartment)
                    ? string.Join("", row.Skip(1).Take(3))  // city + area + road
                    : string.Join("", row.Skip(1).Take(row.Length - 2));

                var tailRule = (hasOffice || hasDepartment) ? row[4] : row[row.Length - 1];  // scope
                var roadName = row.Length >= 4 ? row[3] : "";
                var department = hasDepartment ? row[5] : "";
                var office = hasOffice ? row[6] : "";

                Put(conn, headAddr, tailRule, zipcode, roadName, department, office);
            }
        });
    }

    /// <summary>
    /// 從結構化資料載入索引（包含所有 DBF 欄位）
    /// </summary>
    internal void LoadFromStructuredData(IEnumerable<PostalRuleData> rules, IEnumerable<string[]> csvRows)
    {
        WithinTransaction(conn =>
        {
            CreateTables(conn);

            // 首先插入所有結構化規則並建立 ID 映射
            var ruleIdMap = new Dictionary<string, long>(); // key: zipcode+city+area+road+scope

            foreach (var rule in rules)
            {
                var ruleId = PutPostalRule(conn, rule);

                // 建立鍵值映射（用於關聯 precise 表）
                var key = $"{rule.ZipCode}|{rule.City}|{rule.Area}|{rule.Road}|{rule.Scope}";
                ruleIdMap[key] = ruleId;
            }

            // 然後載入傳統資料（用於 precise 和 gradual 表）
            foreach (var row in csvRows)
            {
                if (row.Length < 2) continue;

                var zipcode = row[0];

                // 新格式：[zipcode, city, area, road, scope, department, office]
                // 中格式：[zipcode, city, area, road, scope, department]
                // 舊格式：[zipcode, city, area, road, scope]
                var hasOffice = row.Length >= 7;
                var hasDepartment = row.Length >= 6;

                var city = row.Length >= 2 ? row[1] : "";
                var area = row.Length >= 3 ? row[2] : "";
                var roadName = row.Length >= 4 ? row[3] : "";

                var headAddr = (hasOffice || hasDepartment)
                    ? string.Join("", row.Skip(1).Take(3))  // city + area + road
                    : string.Join("", row.Skip(1).Take(row.Length - 2));

                var tailRule = (hasOffice || hasDepartment) ? row[4] : row[row.Length - 1];  // scope
                var department = hasDepartment ? row[5] : "";
                var office = hasOffice ? row[6] : "";

                // 查找對應的 rule_id
                var key = $"{zipcode}|{city}|{area}|{roadName}|{tailRule}";
                var ruleId = ruleIdMap.ContainsKey(key) ? (long?)ruleIdMap[key] : null;

                Put(conn, headAddr, tailRule, zipcode, roadName, department, office, ruleId);
            }
        });
    }

    /// <summary>
    /// 查詢精確匹配的規則
    /// </summary>
    private List<(string RuleStr, string Zipcode, string Department, string Office)> GetRuleStrZipcodePairs(SqliteConnection conn, string addrStr)
    {
        var results = new List<(string, string, string, string)>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT rule_str, zipcode, department, office FROM precise WHERE addr_str = $addr_str";
        cmd.Parameters.AddWithValue("$addr_str", addrStr);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var ruleStr = reader.GetString(0);
            var zipcode = reader.GetString(1);
            var department = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var office = reader.IsDBNull(3) ? "" : reader.GetString(3);
            results.Add((ruleStr, zipcode, department, office));
        }

        return results;
    }

    /// <summary>
    /// 查詢漸進式匹配的郵遞區號
    /// </summary>
    private string? GetGradualZipcode(SqliteConnection conn, string addrStr)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT zipcode FROM gradual WHERE addr_str = $addr_str";
        cmd.Parameters.AddWithValue("$addr_str", addrStr);

        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    /// <summary>
    /// 查詢地址的路名類型
    /// </summary>
    internal PostalRoadType? GetPostalRoadType(string addrStr)
    {
        return WithinTransaction(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT road_type FROM precise WHERE addr_str = $addr_str LIMIT 1";
            cmd.Parameters.AddWithValue("$addr_str", addrStr);

            var result = cmd.ExecuteScalar();
            if (result != null && Enum.TryParse<PostalRoadType>(result.ToString(), out var roadType))
            {
                return (PostalRoadType?)roadType;
            }
            return null;
        });
    }

    /// <summary>
    /// 查詢地址的郵遞區號（返回詳細結果）
    /// </summary>
    internal ZipCodeResult FindDetailed(string addrStr)
    {
        return WithinTransaction(conn =>
        {
            var addr = new AddressTokenizer(addrStr);
            var lenAddrTokens = addr.Tokens.Count;

            // 避免不必要的迭代
            var startLen = lenAddrTokens;
            while (startLen >= 0)
            {
                var pair = addr.Parse(startLen - 1);
                if (pair.No == 0 && pair.SubNos.Count == 0)
                    break;
                startLen--;
            }

            for (var i = startLen; i > 0; i--)
            {
                var currentAddrStr = addr.Flat(i);
                var rzpairs = GetRuleStrZipcodePairs(conn, currentAddrStr);

                // 處理「村里鄰」的特殊情況
                if (i == startLen &&
                    lenAddrTokens >= 4 &&
                    addr.Tokens[2][AddressTokenizer.UNIT] is "村" or "里" &&
                    rzpairs.Count == 0)
                {
                    if (addr.Tokens[3][AddressTokenizer.UNIT] == "鄰")
                    {
                        // 刪除「鄰」這個不重要的 token
                        addr.Tokens.RemoveAt(3);
                        lenAddrTokens--;
                    }

                    if (lenAddrTokens >= 4 && addr.Tokens[3][AddressTokenizer.UNIT] == "號")
                    {
                        // 清空村里 token 的單位
                        addr.Tokens[2] = new[] { "", "", addr.Tokens[2][AddressTokenizer.NAME], "" };
                    }
                    else
                    {
                        // 刪除「村里」這個不重要的 token
                        addr.Tokens.RemoveAt(2);
                    }

                    rzpairs = GetRuleStrZipcodePairs(conn, addr.Flat(3));
                }

                // 檢查規則匹配
                if (rzpairs.Count > 0)
                {
                    foreach (var (ruleStr, zipcode, department, office) in rzpairs)
                    {
                        if (new DeliveryRuleMatcher(ruleStr).Match(addr))
                        {
                            var result = ZipCodeResult.ExactMatch(addrStr, zipcode, ruleStr);
                            result.Department = string.IsNullOrEmpty(department) ? null : department;
                            result.Office = string.IsNullOrEmpty(office) ? null : office;
                            return result;
                        }
                    }
                }

                // 檢查漸進式匹配
                var gzipcode = GetGradualZipcode(conn, currentAddrStr);
                if (gzipcode != null)
                    return ZipCodeResult.PartialMatch(addrStr, gzipcode, currentAddrStr);
            }

            return ZipCodeResult.NotFound(addrStr);
        });
    }


    /// <summary>
    /// 在交易中執行操作
    /// </summary>
    private T WithinTransaction<T>(Func<SqliteConnection, T> action)
    {
        EnsureConnection();
        return action(_conn!);
    }

    /// <summary>
    /// 在交易中執行寫入操作（無返回值）
    /// </summary>
    private void WithinTransaction(Action<SqliteConnection> action)
    {
        EnsureConnection();

        using var transaction = _conn!.BeginTransaction();
        try
        {
            action(_conn);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void EnsureConnection()
    {
        if (_conn == null || !_keepAlive)
        {
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Cache = SqliteCacheMode.Shared
            };

            // keepAlive 模式下，根據檔案是否存在決定開啟模式：
            // - 檔案存在：唯讀查詢
            // - 檔案不存在：讀寫建立（供 builder 使用）
            if (_keepAlive && System.IO.File.Exists(_dbPath))
                csb.Mode = SqliteOpenMode.ReadOnly;

            // 釋放舊連線，避免資源洩漏
            if (_conn != null)
            {
                try { _conn.Dispose(); } catch { }
            }

            _conn = new SqliteConnection(csb.ToString());
        }

        if (_conn.State != System.Data.ConnectionState.Open)
            _conn.Open();
    }

    /// <summary>
    /// 驗證地址是否合法且在合理範圍內
    /// </summary>
    /// <param name="addrStr">完整地址字串</param>
    /// <returns>驗證結果</returns>
    internal PostalValidationResult ValidateAddress(string addrStr)
    {
        var result = new PostalValidationResult
        {
            IsValid = false
        };

        if (string.IsNullOrWhiteSpace(addrStr))
        {
            result.FailureReason = PostalValidationFailureReason.InvalidFormat;
            result.Messages.Add("地址不能為空");
            return result;
        }

        return WithinTransaction(conn =>
        {
            var addr = new AddressTokenizer(addrStr);
            result.NormalizedAddress = addr.Flat();
            var lenAddrTokens = addr.Tokens.Count;

            if (lenAddrTokens == 0)
            {
                result.FailureReason = PostalValidationFailureReason.InvalidFormat;
                result.Messages.Add("地址格式無效");
                return result;
            }

            // 避免不必要的迭代
            var startLen = lenAddrTokens;
            while (startLen >= 0)
            {
                var pair = addr.Parse(startLen - 1);
                if (pair.No == 0 && pair.SubNos.Count == 0)
                    break;
                startLen--;
            }

            bool foundAnyMatch = false;
            string partialZipcode = string.Empty;

            for (var i = startLen; i > 0; i--)
            {
                var currentAddrStr = addr.Flat(i);
                var rzpairs = GetRuleStrZipcodePairs(conn, currentAddrStr);

                // 處理「村里鄰」的特殊情況
                if (i == startLen &&
                    lenAddrTokens >= 4 &&
                    addr.Tokens[2][AddressTokenizer.UNIT] is "村" or "里" &&
                    rzpairs.Count == 0)
                {
                    if (addr.Tokens[3][AddressTokenizer.UNIT] == "鄰")
                    {
                        addr.Tokens.RemoveAt(3);
                        lenAddrTokens--;
                    }

                    if (lenAddrTokens >= 4 && addr.Tokens[3][AddressTokenizer.UNIT] == "號")
                    {
                        addr.Tokens[2] = new[] { "", "", addr.Tokens[2][AddressTokenizer.NAME], "" };
                    }
                    else
                    {
                        addr.Tokens.RemoveAt(2);
                    }

                    rzpairs = GetRuleStrZipcodePairs(conn, addr.Flat(3));
                }

                // 檢查精確規則匹配
                if (rzpairs.Count > 0)
                {
                    foundAnyMatch = true;
                    bool ruleMatched = false;

                    foreach (var (ruleStr, zipcode, department, office) in rzpairs)
                    {
                        var rule = new DeliveryRuleMatcher(ruleStr);
                        if (rule.Match(addr))
                        {
                            result.IsValid = true;
                            result.ZipCode = zipcode;
                            result.Messages.Add($"地址有效，郵遞區號：{zipcode}");
                            return result;
                        }
                        else
                        {
                            ruleMatched = true;
                        }
                    }

                    // 找到規則但不匹配
                    if (ruleMatched && !result.IsValid)
                    {
                        result.FailureReason = PostalValidationFailureReason.NumberRuleMismatch;
                        result.Messages.Add($"門牌號碼不符合規則");

                        // 提供部分地址的郵遞區號作為參考
                        var gzipcode = GetGradualZipcode(conn, currentAddrStr);
                        if (gzipcode != null)
                        {
                            result.Messages.Add($"該路段的郵遞區號：{gzipcode}");
                        }

                        return result;
                    }
                }

                // 檢查漸進式匹配
                var gradualZipcode = GetGradualZipcode(conn, currentAddrStr);
                if (gradualZipcode != null)
                {
                    partialZipcode = gradualZipcode;
                    foundAnyMatch = true;
                }
            }

            // 只找到部分匹配（沒有完整門牌號規則）
            if (foundAnyMatch && !string.IsNullOrEmpty(partialZipcode))
            {
                result.FailureReason = PostalValidationFailureReason.NumberOutOfRange;
                result.Messages.Add("找不到完整的門牌號碼規則");
                result.Messages.Add($"部分地址的郵遞區號：{partialZipcode}");
                return result;
            }

            // 完全找不到匹配
            result.FailureReason = PostalValidationFailureReason.AddressNotFound;
            result.Messages.Add("找不到此地址");

            // 嘗試提供建議
            var suggestions = GetPostalAddressSuggestions(conn, addr);
            if (suggestions.Count > 0)
            {
                result.Suggestions.AddRange(suggestions);
                result.Messages.Add($"您是否要找：{string.Join("、", suggestions.Take(3))}");
            }

            return result;
        });
    }

    /// <summary>
    /// 取得地址建議（用於驗證失敗時）
    /// </summary>
    private List<string> GetPostalAddressSuggestions(SqliteConnection conn, AddressTokenizer addr)
    {
        var suggestions = new List<string>();

        // 嘗試找出相似的地址
        for (int i = addr.Tokens.Count; i > 0; i--)
        {
            var partialAddr = addr.Flat(i - 1);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT addr_str
                FROM gradual
                WHERE addr_str LIKE $pattern
                LIMIT 5";
            cmd.Parameters.AddWithValue("$pattern", partialAddr + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var suggestion = reader.GetString(0);
                if (!suggestions.Contains(suggestion))
                {
                    suggestions.Add(suggestion);
                }
            }

            if (suggestions.Count >= 3)
                break;
        }

        return suggestions;
    }

    /// <summary>
    /// 取得地址的所有可能投遞規則
    /// </summary>
    /// <param name="addrStr">地址字串</param>
    /// <returns>郵遞區號和投遞規則的清單</returns>
    internal List<ZipCodeDeliveryRule> GetDeliveryRules(string addrStr)
    {
        return WithinTransaction(conn =>
        {
            var results = new List<ZipCodeDeliveryRule>();
            var addr = new AddressTokenizer(addrStr);
            var normalizedAddr = addr.Flat();

            // 查詢所有匹配的規則
            var rzpairs = GetRuleStrZipcodePairs(conn, normalizedAddr);

            foreach (var (ruleStr, zipcode, department, office) in rzpairs)
            {
                try
                {
                    var rule = PostalDeliveryRule.Parse(ruleStr);
                    results.Add(new ZipCodeDeliveryRule(zipcode, rule));
                }
                catch
                {
                    // 如果規則解析失敗，跳過
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 取得地址候選清單（詳細版，包含郵遞區號和組件）
    /// </summary>
    /// <param name="partialAddress">部分地址</param>
    /// <param name="maxResults">最大返回數量（預設 10）</param>
    /// <returns>候選地址清單（包含郵遞區號）</returns>
    internal List<PostalAddressSuggestion> GetSuggestionsDetailed(string partialAddress, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(partialAddress))
            return new List<PostalAddressSuggestion>();

        return WithinTransaction(conn =>
        {
            var addr = new AddressTokenizer(partialAddress);
            var normalizedInput = addr.Flat();

            // 從 gradual 表查詢所有以輸入開頭的地址
            var suggestions = new List<PostalAddressSuggestion>();
            var seenAddresses = new HashSet<string>();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT addr_str, zipcode
                FROM gradual
                WHERE addr_str LIKE $pattern
                LIMIT $limit";
            cmd.Parameters.AddWithValue("$pattern", normalizedInput + "%");
            cmd.Parameters.AddWithValue("$limit", maxResults * 10); // 多抓一些以便篩選

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var matchedAddr = reader.GetString(0);
                var zipcode = reader.GetString(1);

                // 提取候選部分：從匹配地址中取得輸入之後的部分
                if (matchedAddr.Length > normalizedInput.Length)
                {
                    var remainingPart = matchedAddr.Substring(normalizedInput.Length);
                    var remainingAddr = new AddressTokenizer(remainingPart);

                    // 如果有剩餘 tokens，取第一個完整的 token 作為候選
                    if (remainingAddr.Tokens.Count > 0)
                    {
                        var suggestionAddr = normalizedInput + string.Concat(remainingAddr.Tokens[0]);

                        if (!seenAddresses.Contains(suggestionAddr))
                        {
                            seenAddresses.Add(suggestionAddr);

                            suggestions.Add(new PostalAddressSuggestion(
                                AddressText: suggestionAddr,
                                ZipCode: zipcode,
                                Address: PostalAddress.Parse(suggestionAddr)
                            ));

                            if (suggestions.Count >= maxResults)
                                break;
                        }
                    }
                }
            }

            return suggestions;
        });
    }


    public void Dispose()
    {
        _conn?.Dispose();
        _conn = null;
    }
}
