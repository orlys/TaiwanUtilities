namespace TaiwanUtilities.Builder;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using TaiwanUtilities;

class Program
{
    static int Main(string[] args)
    {
        // 註冊 Big5 和其他編碼提供者
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 如果沒有參數或第一個參數是命令，執行命令模式
        if (args.Length > 0)
        {
            var command = args[0].ToLower();
            var verbose = args.Any(a => a == "--verbose" || a == "-v");

            switch (command)
            {
                case "inspect":
                    return InspectCommand(args);

                case "validate":
                    return ValidateCommand(args, verbose);

                case "stats":
                    return StatsCommand(args);

                case "analyze-department":
                case "dept":
                    return AnalyzeDepartmentCommand(args);

                case "export-all":
                case "lab":
                    return ExportAllCommand(args);

                case "analyze-roads":
                case "roads":
                    return AnalyzeRoadsCommand(args);

                case "generate":
                case "gen":
                    return GenerateAddressesCommand(args);

                case "codegen":
                    return CodegenCommand(args);

                case "build":
                    Console.WriteLine("⚠ 'build' 命令已移除。請改用 'codegen' 命令生成靜態 C# 資料：");
                    Console.WriteLine("  dotnet run -- codegen <input.dbf> <output.g.cs>");
                    return 1;

                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;

                default:
                    // 如果不是命令，當作 codegen 的輸入路徑處理
                    Console.WriteLine($"未知命令: {command}");
                    Console.WriteLine("使用 'help' 查看可用命令。");
                    return 1;
            }
        }

        ShowHelp();
        return 0;
    }

    static void ShowHelp()
    {
        Console.WriteLine("=== TaiwanUtilities.Builder - 郵遞區號資料工具 ===\n");
        Console.WriteLine("用法: dotnet run -- [command] [options]\n");
        Console.WriteLine("命令:");
        Console.WriteLine("  codegen <dbf> <output>  生成靜態 C# 資料（PostalData.g.cs）");
        Console.WriteLine("  inspect <dbf>           檢查 .dbf 檔案結構");
        Console.WriteLine("  validate <json>         驗證 JSON 資料集");
        Console.WriteLine("  stats <json>            顯示資料集統計");
        Console.WriteLine("  analyze-department      分析 DEPARTMENT 欄位（別名: dept）");
        Console.WriteLine("  export-all [input] [output]  匯出所有欄位到 SQLite（別名: lab）");
        Console.WriteLine("  generate [count] [db]   從 SQLite 資料庫隨機生成測試地址（別名: gen）");
        Console.WriteLine("  help                    顯示此說明\n");
        Console.WriteLine("選項:");
        Console.WriteLine("  --verbose, -v           顯示詳細資訊\n");
        Console.WriteLine("範例:");
        Console.WriteLine("  dotnet run -- codegen temp/rall1.dbf src/TaiwanUtilities/Postal/PostalData.g.cs");
        Console.WriteLine("  dotnet run -- inspect ../../dataset/rall1.dbf # 檢查 DBF");
        Console.WriteLine("  dotnet run -- validate ../../dataset/zipcode.json");
        Console.WriteLine("  dotnet run -- stats ../../dataset/zipcode.json");
        Console.WriteLine("  dotnet run -- analyze-department              # 分析 DEPARTMENT");
    }

    /// <summary>
    /// 決定輸入檔案路徑：優先使用 /data (本機開發)，否則使用 dataset (GitHub Action)
    /// </summary>
    static string DetermineInputPath()
    {
        // 優先順序：
        // 1. data/rall1.dbf (本機開發)
        // 2. ../../dataset/rall1.dbf (GitHub Action 或傳統路徑)

        var localDataPath = "../../data/rall1.dbf";
        var ciDataPath = "../../dataset/rall1.dbf";

        if (File.Exists(localDataPath))
        {
            Console.WriteLine($"[本機開發] 使用 data/ 目錄的資料");
            return localDataPath;
        }
        else if (File.Exists(ciDataPath))
        {
            Console.WriteLine($"[CI/CD] 使用 dataset/ 目錄的資料");
            return ciDataPath;
        }
        else
        {
            Console.WriteLine($"[警告] 找不到資料檔案，使用預設路徑");
            return ciDataPath; // 返回預設路徑，讓後續錯誤處理接手
        }
    }

    static int InspectCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("❌ 錯誤: 請指定要檢查的 .dbf 檔案");
            Console.WriteLine("用法: dotnet run -- inspect <file.dbf>");
            return 1;
        }

        var filePath = args[1];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 錯誤: 檔案不存在: {filePath}");
            return 1;
        }

        DbfInspector.Inspect(filePath);
        return 0;
    }

    static int ValidateCommand(string[] args, bool verbose)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("❌ 錯誤: 請指定要驗證的檔案");
            Console.WriteLine("用法: dotnet run -- validate <file>");
            return 1;
        }

        var filePath = args[1];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 錯誤: 檔案不存在: {filePath}");
            return 1;
        }

        Console.WriteLine($"驗證檔案: {filePath}\n");

        var validator = new DatasetValidator();
        var result = validator.Validate(filePath);

        DatasetValidator.PrintReport(result, verbose);

        return result.IsValid ? 0 : 1;
    }

    static int StatsCommand(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("❌ 錯誤: 請指定要分析的檔案");
            Console.WriteLine("用法: dotnet run -- stats <file>");
            return 1;
        }

        var filePath = args[1];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 錯誤: 檔案不存在: {filePath}");
            return 1;
        }

        Console.WriteLine($"分析檔案: {filePath}\n");

        var validator = new DatasetValidator();
        var result = validator.Validate(filePath);

        Console.WriteLine("=== 資料集統計資訊 ===\n");

        foreach (var kvp in result.Statistics.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }

        return 0;
    }

    static int AnalyzeDepartmentCommand(string[] args)
    {
        var filePath = args.Length > 1 ? args[1] : DetermineInputPath();

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ 錯誤: 檔案不存在: {filePath}");
            return 1;
        }

        Console.WriteLine($"=== 分析 DEPARTMENT 欄位 ===");
        Console.WriteLine($"檔案: {filePath}\n");

        try
        {
            using var stream = File.OpenRead(filePath);
            using var reader = new DbfDataReader.DbfDataReader(stream, new DbfDataReader.DbfDataReaderOptions
            {
                Encoding = Encoding.GetEncoding("big5")
            });

            var deptIdx = reader.GetOrdinal("DEPARTMENT");
            var cityIdx = reader.GetOrdinal("CITY");
            var areaIdx = reader.GetOrdinal("AREA");
            var roadIdx = reader.GetOrdinal("ROAD");
            var zipcodeIdx = reader.GetOrdinal("ZIPCODE");
            var scopeIdx = reader.GetOrdinal("SCOOP");

            int total = 0;
            int withDept = 0;
            var deptExamples = new List<string>();
            var deptStats = new Dictionary<string, int>();

            while (reader.Read())
            {
                total++;
                var dept = reader.GetString(deptIdx)?.Trim() ?? "";

                if (!string.IsNullOrEmpty(dept))
                {
                    withDept++;

                    // 統計各 department 出現次數
                    if (deptStats.ContainsKey(dept))
                    {
                        deptStats[dept]++;
                    }
                    else
                    {
                        deptStats[dept] = 1;
                    }

                    if (deptExamples.Count < 30)
                    {
                        var city = reader.GetString(cityIdx)?.Trim() ?? "";
                        var area = reader.GetString(areaIdx)?.Trim() ?? "";
                        var road = reader.GetString(roadIdx)?.Trim() ?? "";
                        var zipcode = reader.GetString(zipcodeIdx)?.Trim() ?? "";
                        var scope = reader.GetString(scopeIdx)?.Trim() ?? "";

                        deptExamples.Add($"{zipcode} {city}{area}{road} {scope} → [{dept}]");
                    }
                }
            }

            Console.WriteLine($"總記錄數: {total:N0}");
            Console.WriteLine($"有 DEPARTMENT 的記錄: {withDept:N0} ({(double)withDept / total * 100:F2}%)");
            Console.WriteLine($"不同的 DEPARTMENT 值: {deptStats.Count:N0}");
            Console.WriteLine();

            if (deptStats.Count > 0)
            {
                Console.WriteLine("DEPARTMENT 統計（依出現次數排序）:");
                foreach (var kvp in deptStats.OrderByDescending(kv => kv.Value).Take(20))
                {
                    Console.WriteLine($"  [{kvp.Key}] → {kvp.Value:N0} 筆");
                }
                Console.WriteLine();
            }

            if (deptExamples.Count > 0)
            {
                Console.WriteLine("DEPARTMENT 範例（前 30 筆）:");
                foreach (var example in deptExamples)
                {
                    Console.WriteLine($"  {example}");
                }
            }
            else
            {
                Console.WriteLine("⚠ 沒有找到包含 DEPARTMENT 資料的記錄");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 錯誤: {ex.Message}");
            return 1;
        }
    }

    static int AnalyzeRoadsCommand(string[] args)
    {
        try
        {
            var inputPath = DetermineInputPath();

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"❌ 找不到資料檔案: {inputPath}");
                return 1;
            }

            Console.WriteLine("=== 分析路名 ===");
            Console.WriteLine($"資料來源: {inputPath}");
            Console.WriteLine();

            var roads = new Dictionary<string, HashSet<string>>(); // road -> set of full addresses
            var normalRoadSuffixes = new[] { "路", "街", "道", "大道", "boulevard" };
            var abnormalRoads = new Dictionary<string, HashSet<string>>();

            using var stream = File.OpenRead(inputPath);
            using (var reader = new DbfDataReader.DbfDataReader(stream, new DbfDataReader.DbfDataReaderOptions
            {
                Encoding = Encoding.GetEncoding("big5")
            }))
            {
                var roadIdx = reader.GetOrdinal("ROAD");
                var cityIdx = reader.GetOrdinal("CITY");
                var areaIdx = reader.GetOrdinal("AREA");

                while (reader.Read())
                {
                    var road = reader.GetString(roadIdx)?.Trim() ?? "";
                    if (string.IsNullOrEmpty(road))
                    {
                        continue;
                    }

                    var city = reader.GetString(cityIdx)?.Trim() ?? "";
                    var area = reader.GetString(areaIdx)?.Trim() ?? "";
                    var fullAddr = $"{city}{area}{road}";

                    // 檢查是否為正常路名（以路、街、道等結尾）
                    bool isNormal = normalRoadSuffixes.Any(suffix => road.EndsWith(suffix));

                    if (!isNormal)
                    {
                        if (!abnormalRoads.ContainsKey(road))
                        {
                            abnormalRoads[road] = new HashSet<string>();
                        }

                        abnormalRoads[road].Add(fullAddr);
                    }
                    else
                    {
                        if (!roads.ContainsKey(road))
                        {
                            roads[road] = new HashSet<string>();
                        }

                        roads[road].Add(fullAddr);
                    }
                }
            }

            Console.WriteLine($"✓ 正常路名數量: {roads.Count:N0}");
            Console.WriteLine($"⚠ 非正常路名數量: {abnormalRoads.Count:N0}");
            Console.WriteLine();

            if (abnormalRoads.Count > 0)
            {
                Console.WriteLine("=== 非正常路名清單 ===");
                Console.WriteLine();

                var sorted = abnormalRoads.OrderBy(kv => kv.Key).ToList();

                foreach (var kvp in sorted)
                {
                    var road = kvp.Key;
                    var addresses = kvp.Value.OrderBy(a => a).ToList();

                    Console.WriteLine($"路名: {road}");
                    Console.WriteLine($"  出現次數: {addresses.Count}");
                    Console.WriteLine($"  完整地址範例:");

                    foreach (var addr in addresses.Take(5))
                    {
                        Console.WriteLine($"    - {addr}");
                    }

                    if (addresses.Count > 5)
                    {
                        Console.WriteLine($"    ... 還有 {addresses.Count - 5} 筆");
                    }

                    Console.WriteLine();
                }

                // 輸出到檔案
                var outputPath = "abnormal_roads.txt";
                using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8))
                {
                    writer.WriteLine("=== 非正常路名清單 ===");
                    writer.WriteLine($"生成時間: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"總數: {abnormalRoads.Count}");
                    writer.WriteLine();

                    foreach (var kvp in sorted)
                    {
                        writer.WriteLine($"路名: {kvp.Key}");
                        writer.WriteLine($"  出現次數: {kvp.Value.Count}");
                        writer.WriteLine($"  完整地址:");

                        foreach (var addr in kvp.Value.OrderBy(a => a))
                        {
                            writer.WriteLine($"    - {addr}");
                        }

                        writer.WriteLine();
                    }
                }

                Console.WriteLine($"✓ 已輸出到檔案: {outputPath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 錯誤: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static int ExportAllCommand(string[] args)
    {
        var inputPath = args.Length > 1 ? args[1] : DetermineInputPath();
        var outputPath = args.Length > 2 ? args[2] : "lab.db";

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"❌ 錯誤: 檔案不存在: {inputPath}");
            return 1;
        }

        Console.WriteLine("=== 匯出所有 DBF 欄位到 SQLite ===");
        Console.WriteLine($"輸入: {inputPath}");
        Console.WriteLine($"輸出: {outputPath}\n");

        try
        {
            // 刪除舊的資料庫，如果失敗則使用新檔名
            if (File.Exists(outputPath))
            {
                try
                {
                    Console.WriteLine("刪除舊的資料庫...");
                    File.Delete(outputPath);
                }
                catch (IOException)
                {
                    // 檔案被占用，使用新檔名
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    outputPath = $"lab_{timestamp}.db";
                    Console.WriteLine($"⚠ 原檔案被占用，改用新檔名: {outputPath}");
                }
            }

            using var dbfStream = File.OpenRead(inputPath);
            using var dbfReader = new DbfDataReader.DbfDataReader(dbfStream, new DbfDataReader.DbfDataReaderOptions
            {
                Encoding = Encoding.GetEncoding("big5")
            });

            using var sqliteConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={outputPath}");
            sqliteConn.Open();

            // 建立資料表（包含所有 37 個欄位）
            using (var cmd = sqliteConn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE postal_data (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        OFFICE TEXT,
                        ZIP3A TEXT,
                        ZIPCODE TEXT,
                        CITY TEXT,
                        AREA TEXT,
                        AREA1 TEXT,
                        ROAD TEXT,
                        SCOOP TEXT,
                        EVEN INTEGER,
                        CMP_LABLE TEXT,
                        LANE INTEGER,
                        LANE1 INTEGER,
                        ALLEY INTEGER,
                        ALLEY1 INTEGER,
                        NO_BGN INTEGER,
                        NO_BGN1 INTEGER,
                        NO_END INTEGER,
                        NO_END1 INTEGER,
                        FLOOR INTEGER,
                        FLOOR1 INTEGER,
                        LANE11 INTEGER,
                        LANE22 INTEGER,
                        ALLEY11 INTEGER,
                        ALLEY22 INTEGER,
                        ROAD_NO TEXT,
                        ROAD1 TEXT,
                        EROAD TEXT,
                        RMK TEXT,
                        RMK1 TEXT,
                        ZIP3RMK INTEGER,
                        ECITY TEXT,
                        EAREA TEXT,
                        UWORD TEXT,
                        ISN INTEGER,
                        ISS INTEGER,
                        DEPARTMENT TEXT,
                        BN1 TEXT
                    )";
                cmd.ExecuteNonQuery();
            }

            // 建立索引
            using (var cmd = sqliteConn.CreateCommand())
            {
                cmd.CommandText = "CREATE INDEX idx_zipcode ON postal_data(ZIPCODE)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX idx_city_area ON postal_data(CITY, AREA)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "CREATE INDEX idx_department ON postal_data(DEPARTMENT)";
                cmd.ExecuteNonQuery();
            }

            Console.Write("正在匯出資料...");

            int count = 0;
            using var transaction = sqliteConn.BeginTransaction();
            using var insertCmd = sqliteConn.CreateCommand();

            insertCmd.CommandText = @"
                INSERT INTO postal_data (
                    OFFICE, ZIP3A, ZIPCODE, CITY, AREA, AREA1, ROAD, SCOOP, EVEN, CMP_LABLE,
                    LANE, LANE1, ALLEY, ALLEY1, NO_BGN, NO_BGN1, NO_END, NO_END1,
                    FLOOR, FLOOR1, LANE11, LANE22, ALLEY11, ALLEY22, ROAD_NO, ROAD1,
                    EROAD, RMK, RMK1, ZIP3RMK, ECITY, EAREA, UWORD, ISN, ISS, DEPARTMENT, BN1
                ) VALUES (
                    $1, $2, $3, $4, $5, $6, $7, $8, $9, $10,
                    $11, $12, $13, $14, $15, $16, $17, $18, $19, $20,
                    $21, $22, $23, $24, $25, $26, $27, $28, $29, $30,
                    $31, $32, $33, $34, $35, $36, $37
                )";

            while (dbfReader.Read())
            {
                insertCmd.Parameters.Clear();

                // 讀取所有欄位
                insertCmd.Parameters.AddWithValue("$1", dbfReader.GetString(dbfReader.GetOrdinal("OFFICE"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$2", dbfReader.GetString(dbfReader.GetOrdinal("ZIP3A"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$3", dbfReader.GetString(dbfReader.GetOrdinal("ZIPCODE"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$4", dbfReader.GetString(dbfReader.GetOrdinal("CITY"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$5", dbfReader.GetString(dbfReader.GetOrdinal("AREA"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$6", dbfReader.GetString(dbfReader.GetOrdinal("AREA1"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$7", dbfReader.GetString(dbfReader.GetOrdinal("ROAD"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$8", dbfReader.GetString(dbfReader.GetOrdinal("SCOOP"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$9", GetIntValue(dbfReader, "EVEN"));
                insertCmd.Parameters.AddWithValue("$10", dbfReader.GetString(dbfReader.GetOrdinal("CMP_LABLE"))?.Trim() ?? "");

                insertCmd.Parameters.AddWithValue("$11", GetIntValue(dbfReader, "LANE"));
                insertCmd.Parameters.AddWithValue("$12", GetIntValue(dbfReader, "LANE1"));
                insertCmd.Parameters.AddWithValue("$13", GetIntValue(dbfReader, "ALLEY"));
                insertCmd.Parameters.AddWithValue("$14", GetIntValue(dbfReader, "ALLEY1"));
                insertCmd.Parameters.AddWithValue("$15", GetIntValue(dbfReader, "NO_BGN"));
                insertCmd.Parameters.AddWithValue("$16", GetIntValue(dbfReader, "NO_BGN1"));
                insertCmd.Parameters.AddWithValue("$17", GetIntValue(dbfReader, "NO_END"));
                insertCmd.Parameters.AddWithValue("$18", GetIntValue(dbfReader, "NO_END1"));
                insertCmd.Parameters.AddWithValue("$19", GetIntValue(dbfReader, "FLOOR"));
                insertCmd.Parameters.AddWithValue("$20", GetIntValue(dbfReader, "FLOOR1"));

                insertCmd.Parameters.AddWithValue("$21", GetIntValue(dbfReader, "LANE11"));
                insertCmd.Parameters.AddWithValue("$22", GetIntValue(dbfReader, "LANE22"));
                insertCmd.Parameters.AddWithValue("$23", GetIntValue(dbfReader, "ALLEY11"));
                insertCmd.Parameters.AddWithValue("$24", GetIntValue(dbfReader, "ALLEY22"));
                insertCmd.Parameters.AddWithValue("$25", dbfReader.GetString(dbfReader.GetOrdinal("ROAD_NO"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$26", dbfReader.GetString(dbfReader.GetOrdinal("ROAD1"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$27", dbfReader.GetString(dbfReader.GetOrdinal("EROAD"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$28", dbfReader.GetString(dbfReader.GetOrdinal("RMK"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$29", dbfReader.GetString(dbfReader.GetOrdinal("RMK1"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$30", GetBoolValue(dbfReader, "ZIP3RMK"));

                insertCmd.Parameters.AddWithValue("$31", dbfReader.GetString(dbfReader.GetOrdinal("ECITY"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$32", dbfReader.GetString(dbfReader.GetOrdinal("EAREA"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$33", dbfReader.GetString(dbfReader.GetOrdinal("UWORD"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$34", GetBoolValue(dbfReader, "ISN"));
                insertCmd.Parameters.AddWithValue("$35", GetBoolValue(dbfReader, "ISS"));
                insertCmd.Parameters.AddWithValue("$36", dbfReader.GetString(dbfReader.GetOrdinal("DEPARTMENT"))?.Trim() ?? "");
                insertCmd.Parameters.AddWithValue("$37", dbfReader.GetString(dbfReader.GetOrdinal("BN1"))?.Trim() ?? "");

                insertCmd.ExecuteNonQuery();

                count++;
                if (count % 10000 == 0)
                {
                    Console.Write($"\r正在匯出資料... {count:N0} 筆");
                }
            }

            transaction.Commit();

            Console.WriteLine($"\r正在匯出資料... {count:N0} 筆 完成！");
            Console.WriteLine();

            // 顯示資料庫資訊
            var fileInfo = new FileInfo(outputPath);
            Console.WriteLine($"✓ 匯出完成！");
            Console.WriteLine($"  資料庫: {outputPath}");
            Console.WriteLine($"  大小: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"  記錄數: {count:N0}");
            Console.WriteLine();
            Console.WriteLine("可以使用以下工具查看：");
            Console.WriteLine("  - DB Browser for SQLite");
            Console.WriteLine("  - sqlite3 lab.db");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 錯誤: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static int GetIntValue(DbfDataReader.DbfDataReader reader, string columnName)
    {
        try
        {
            var idx = reader.GetOrdinal(columnName);
            return reader.IsDBNull(idx) ? 0 : reader.GetInt32(idx);
        }
        catch
        {
            return 0;
        }
    }

    static int GetBoolValue(DbfDataReader.DbfDataReader reader, string columnName)
    {
        try
        {
            var idx = reader.GetOrdinal(columnName);
            return reader.IsDBNull(idx) ? 0 : (reader.GetBoolean(idx) ? 1 : 0);
        }
        catch
        {
            return 0;
        }
    }

    static int GenerateAddressesCommand(string[] args)
    {
        try
        {
            var count = args.Length > 1 && int.TryParse(args[1], out var c) ? c : 20;
            var dbPath = args.Length > 2 ? args[2] : "../../src/TaiwanUtilities/Postal/zipcode.db";

            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"❌ 錯誤: 資料庫檔案不存在: {dbPath}");
                Console.WriteLine("請先執行 'dotnet run -- build' 建立資料庫");
                return 1;
            }

            Console.WriteLine("=== 從資料庫隨機生成測試地址 ===");
            Console.WriteLine($"資料庫: {dbPath}");
            Console.WriteLine($"生成數量: {count}");
            Console.WriteLine();

            // 重要：在任何查詢之前設置資料庫路徑
            PostalDatabase.UseExternalDatabase(dbPath);

            var generator = new AddressGenerator(dbPath);

            // 顯示資料庫統計資訊
            Console.WriteLine("資料庫統計：");
            var stats = generator.GetDatabaseStatistics();
            Console.WriteLine($"  總規則數: {stats.TotalRules:N0}");
            Console.WriteLine($"  有巷號規則: {stats.RulesWithLane:N0} ({Percentage(stats.RulesWithLane, stats.TotalRules)})");
            Console.WriteLine($"  有弄號規則: {stats.RulesWithAlley:N0} ({Percentage(stats.RulesWithAlley, stats.TotalRules)})");
            Console.WriteLine($"  有單雙號規則: {stats.RulesWithEvenOdd:N0} ({Percentage(stats.RulesWithEvenOdd, stats.TotalRules)})");
            Console.WriteLine($"    - 單號規則: {stats.RulesOddOnly:N0}");
            Console.WriteLine($"    - 雙號規則: {stats.RulesEvenOnly:N0}");
            Console.WriteLine($"  有附號規則: {stats.RulesWithSubNumber:N0} ({Percentage(stats.RulesWithSubNumber, stats.TotalRules)})");
            Console.WriteLine($"  不同路名數: {stats.UniqueRoads:N0}");
            Console.WriteLine($"  特殊路名數: {stats.SpecialRoadNames:N0}");
            Console.WriteLine();

            // 生成地址
            Console.WriteLine("正在生成地址...");
            var addresses = generator.GenerateRandomAddresses(count);

            if (addresses.Count == 0)
            {
                Console.WriteLine("⚠ 無法生成地址，資料庫可能為空或沒有 postal_rules 資料表");
                return 1;
            }

            Console.WriteLine($"\n生成了 {addresses.Count} 個測試地址：\n");

            // 顯示生成的地址
            for (int i = 0; i < addresses.Count; i++)
            {
                var addr = addresses[i];
                Console.WriteLine($"[{i + 1:D2}] {addr.FullAddress}");
                Console.WriteLine($"     郵遞區號: {addr.ExpectedZipCode}");
                Console.WriteLine($"     規則範圍: {addr.RuleScope}");
                Console.WriteLine($"     資料來源: {addr.Source}");

                // 驗證生成的地址
                var result = ZipCode.Find(addr.FullAddress);

                if (result.ZipCode == addr.ExpectedZipCode)
                {
                    Console.WriteLine($"     ✓ 驗證通過（查詢結果: {result.ZipCode}）");
                }
                else
                {
                    Console.WriteLine($"     ✗ 驗證失敗（預期: {addr.ExpectedZipCode}, 實際: {result.ZipCode}）");
                }

                // 如果是結構化資料，測試驗證功能
                if (addr.Source == "postal_rules")
                {
                    var postalAddr = PostalAddress.Parse(addr.FullAddress);
                    var validation = PostalAddress.Validate(postalAddr);

                    if (validation.IsValid)
                    {
                        Console.WriteLine($"     ✓ 結構化驗證通過");
                        if (validation.MatchedRule != null)
                        {
                            Console.WriteLine($"       匹配規則 ID: {validation.MatchedRule.Id}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"     ✗ 結構化驗證失敗: {string.Join(", ", validation.Messages)}");
                    }
                }

                Console.WriteLine();
            }

            // 統計資訊
            var sourceGroups = addresses.GroupBy(a => a.Source);
            Console.WriteLine("來源統計:");
            foreach (var group in sourceGroups)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()} 筆");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 錯誤: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static string Percentage(int value, int total)
    {
        if (total == 0)
        {
            return "0%";
        }

        return $"{(double)value / total * 100:F1}%";
    }

    // ── Codegen command ──────────────────────────────────────────────────────

    static int CodegenCommand(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("❌ 錯誤: 請指定輸入 DBF 和輸出路徑");
            Console.WriteLine("用法: dotnet run -- codegen <input.dbf> <output.g.cs>");
            return 1;
        }

        var inputPath  = args[1];
        var outputPath = args[2];

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"❌ 錯誤: 找不到輸入檔案: {inputPath}");
            return 1;
        }

        var nested = args.Contains("--nested");
        Console.WriteLine(nested ? "=== 生成 PostalLookup.g.cs（nested-switch 模式）===" : "=== 生成 PostalData.g.cs ===");
        Console.WriteLine($"輸入: {inputPath}");
        Console.WriteLine($"輸出: {outputPath}");
        Console.WriteLine();

        try
        {
            // 1. 讀取 DBF
            Console.Write("讀取 DBF...");
            var rules = ReadDbfFileStructured(inputPath);
            Console.WriteLine($" {rules.Count:N0} 筆");

            // 2. 建立 string pools
            var zipCodePool   = new List<string>();
            var departments   = new List<string> { string.Empty };  // index 0 = empty
            var offices       = new List<string> { string.Empty };
            var scopes        = new List<string> { string.Empty };
            var zipCodeIndex  = new Dictionary<string, int>(StringComparer.Ordinal);
            var deptIndex     = new Dictionary<string, int>(StringComparer.Ordinal);
            var officeIndex   = new Dictionary<string, int>(StringComparer.Ordinal);
            var scopeIndex    = new Dictionary<string, int>(StringComparer.Ordinal);

            int GetOrAddZip(string s)
            {
                if (zipCodeIndex.TryGetValue(s, out int idx)) return idx;
                idx = zipCodePool.Count;
                zipCodePool.Add(s);
                zipCodeIndex[s] = idx;
                return idx;
            }

            int GetOrAddPool(List<string> pool, Dictionary<string, int> index, string? s)
            {
                if (string.IsNullOrEmpty(s)) return 0;
                if (index.TryGetValue(s!, out int idx)) return idx;
                idx = pool.Count;
                pool.Add(s!);
                index[s!] = idx;
                return idx;
            }

            // 3. 找出特殊路名（不以路/街/道結尾）
            var normalSuffixes = new[] { "路", "街", "道" };
            var specialRoadNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (!string.IsNullOrEmpty(rule.Road) &&
                    !normalSuffixes.Any(s => rule.Road!.EndsWith(s, StringComparison.Ordinal)))
                {
                    specialRoadNames.Add(rule.Road!);
                }
            }

            // 4. 按 city|area|road 分組，各組按特異性排序
            var groups = new Dictionary<string, List<PostalRuleData>>(5200, StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                var key = $"{rule.City}|{rule.Area}|{rule.Road}";
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<PostalRuleData>();
                    groups[key] = list;
                }
                list.Add(rule);
            }

            // Sort each group by specificity descending
            foreach (var kvp in groups)
            {
                kvp.Value.Sort((a, b) => GetSpecificity(b).CompareTo(GetSpecificity(a)));
            }

            // 5. Build per-group arrays and prefetch indices
            var groupKeys = groups.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            var groupData = new List<(string key, int count,
                int[] ns, int[] ne, byte[] hlc, int[] ls, int[] le,
                byte[] hac, int[] als, int[] ale,
                byte[] eo, int[] nss, int[] nse,
                int[] zi, int[] di, int[] oi, int[] sci)>();

            foreach (var key in groupKeys)
            {
                var group = groups[key];
                int n = group.Count;
                var ns  = new int[n]; var ne  = new int[n];
                var hlc = new byte[n]; var ls = new int[n]; var le = new int[n];
                var hac = new byte[n]; var als= new int[n]; var ale= new int[n];
                var eo  = new byte[n];
                var nss = new int[n]; var nse = new int[n];
                var zi  = new int[n]; var di = new int[n];
                var oi  = new int[n]; var sci= new int[n];

                for (int i = 0; i < n; i++)
                {
                    var r = group[i];
                    ns[i]  = r.NumberStart ?? 0;
                    ne[i]  = r.NumberEnd ?? int.MaxValue;
                    if (r.LaneStart.HasValue)
                    {
                        hlc[i] = 1;
                        ls[i]  = r.LaneStart.Value;
                        le[i]  = r.LaneEnd ?? r.LaneStart.Value;
                    }
                    if (r.AlleyStart.HasValue)
                    {
                        hac[i] = 1;
                        als[i] = r.AlleyStart.Value;
                        ale[i] = r.AlleyEnd ?? r.AlleyStart.Value;
                    }
                    eo[i]  = (byte)(r.EvenOdd ?? 0);
                    nss[i] = r.NumberStartSub ?? 0;
                    nse[i] = r.NumberEndSub ?? int.MaxValue;
                    zi[i]  = GetOrAddZip(r.ZipCode);
                    di[i]  = GetOrAddPool(departments, deptIndex, r.Department);
                    oi[i]  = GetOrAddPool(offices, officeIndex, r.Office);
                    sci[i] = GetOrAddPool(scopes, scopeIndex, r.Scope);
                }

                groupData.Add((key, n, ns, ne, hlc, ls, le, hac, als, ale, eo, nss, nse, zi, di, oi, sci));
            }

            // 6. Write the generated file
            Console.Write("生成 C# 原始碼...");
            var generatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            using var sw = new StreamWriter(outputPath, false, Encoding.UTF8);

            if (nested)
            {
                WriteNestedSwitchFile(sw, groupData, generatedDate, rules.Count);
                Console.WriteLine(" 完成！");
                var nfi = new FileInfo(outputPath);
                Console.WriteLine($"輸出大小: {nfi.Length / 1024.0 / 1024.0:F2} MB");
                Console.WriteLine($"路索引鍵數: {groupData.Count:N0}");
                return 0;
            }

            const int ENTRIES_PER_METHOD = 80;
            int methodCount = (groupData.Count + ENTRIES_PER_METHOD - 1) / ENTRIES_PER_METHOD;

            sw.WriteLine("// <auto-generated/>");
            sw.WriteLine("// SPDX-License-Identifier: MIT");
            sw.WriteLine("// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0");
            sw.WriteLine("// This file is generated by: dotnet run --project tools/postal/Postal.Builder -- codegen [dbf] [output]");
            sw.WriteLine("// DO NOT EDIT MANUALLY.");
            sw.WriteLine($"// Generated: {generatedDate} | Records: {rules.Count:N0}");
            sw.WriteLine();
            sw.WriteLine("#nullable enable");
            sw.WriteLine();
            sw.WriteLine("namespace TaiwanUtilities.Internals;");
            sw.WriteLine();
            sw.WriteLine("using System;");
            sw.WriteLine("using System.Collections.Generic;");
            sw.WriteLine();
            sw.WriteLine("#if NET8_0_OR_GREATER");
            sw.WriteLine("using System.Collections.Frozen;");
            sw.WriteLine("#endif");
            sw.WriteLine();
            sw.WriteLine("internal static class PostalData");
            sw.WriteLine("{");
            sw.WriteLine($"    internal static readonly string GeneratedDate = \"{generatedDate}\";");
            sw.WriteLine($"    internal static readonly int RecordCount = {rules.Count};");
            sw.WriteLine();

            // ZipCodePool
            sw.Write("    internal static readonly string[] ZipCodePool = new[] { ");
            sw.Write(string.Join(", ", zipCodePool.Select(z => $"\"{EscapeString(z)}\"")));
            sw.WriteLine(" };");
            sw.WriteLine();

            // Departments
            sw.Write("    internal static readonly string[] Departments = new[] { ");
            sw.Write(string.Join(", ", departments.Select(d => d.Length == 0 ? "string.Empty" : $"\"{EscapeString(d)}\"")));
            sw.WriteLine(" };");
            sw.WriteLine();

            // Offices
            sw.Write("    internal static readonly string[] Offices = new[] { ");
            sw.Write(string.Join(", ", offices.Select(o => o.Length == 0 ? "string.Empty" : $"\"{EscapeString(o)}\"")));
            sw.WriteLine(" };");
            sw.WriteLine();

            // Scopes
            sw.Write("    internal static readonly string[] Scopes = new[] { ");
            sw.Write(string.Join(", ", scopes.Select(s => s.Length == 0 ? "string.Empty" : $"\"{EscapeString(s)}\"")));
            sw.WriteLine(" };");
            sw.WriteLine();

            // SpecialRoadNames
            sw.Write("    internal static readonly System.Collections.Generic.HashSet<string> SpecialRoadNames =");
            sw.WriteLine();
            sw.Write("        new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal) { ");
            sw.Write(string.Join(", ", specialRoadNames.OrderBy(r => r).Select(r => $"\"{EscapeString(r)}\"")));
            sw.WriteLine(" };");
            sw.WriteLine();

            // CreateRules
            sw.WriteLine("    private static System.Collections.Generic.Dictionary<string, PostalRuleSet> CreateRules()");
            sw.WriteLine("    {");
            sw.WriteLine($"        var d = new System.Collections.Generic.Dictionary<string, PostalRuleSet>({groupData.Count}, System.StringComparer.Ordinal);");
            for (int m = 0; m < methodCount; m++)
            {
                sw.WriteLine($"        InitRules{m}(d);");
            }
            sw.WriteLine("        return d;");
            sw.WriteLine("    }");
            sw.WriteLine();

            // Rules field
            sw.WriteLine("#if NET8_0_OR_GREATER");
            sw.WriteLine("    internal static readonly System.Collections.Frozen.FrozenDictionary<string, PostalRuleSet> Rules =");
            sw.WriteLine("        CreateRules().ToFrozenDictionary(System.StringComparer.Ordinal);");
            sw.WriteLine("#else");
            sw.WriteLine("    internal static readonly System.Collections.Generic.Dictionary<string, PostalRuleSet> Rules = CreateRules();");
            sw.WriteLine("#endif");
            sw.WriteLine();

            // InitRulesN methods
            for (int m = 0; m < methodCount; m++)
            {
                sw.WriteLine($"    private static void InitRules{m}(System.Collections.Generic.Dictionary<string, PostalRuleSet> d)");
                sw.WriteLine("    {");
                int start = m * ENTRIES_PER_METHOD;
                int end   = Math.Min(start + ENTRIES_PER_METHOD, groupData.Count);

                for (int gi = start; gi < end; gi++)
                {
                    var (key, cnt, ns, ne, hlc, ls, le, hac, als, ale, eo, nss, nse, zi, di, oi, sci) = groupData[gi];
                    sw.WriteLine($"        d[\"{EscapeString(key)}\"] = new PostalRuleSet(");
                    sw.WriteLine($"            {cnt},");
                    sw.WriteLine($"            new int[] {{ {IntArray(ns)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArrayMaxValue(ne)} }},");
                    sw.WriteLine($"            new byte[] {{ {ByteArray(hlc)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(ls)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(le)} }},");
                    sw.WriteLine($"            new byte[] {{ {ByteArray(hac)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(als)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(ale)} }},");
                    sw.WriteLine($"            new byte[] {{ {ByteArray(eo)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(nss)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArrayMaxValue(nse)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(zi)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(di)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(oi)} }},");
                    sw.WriteLine($"            new int[] {{ {IntArray(sci)} }}");
                    sw.WriteLine("        );");
                }

                sw.WriteLine("    }");
                sw.WriteLine();
            }

            sw.WriteLine("}");

            Console.WriteLine(" 完成！");

            var fi = new FileInfo(outputPath);
            Console.WriteLine($"輸出大小: {fi.Length / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"路索引鍵數: {groupData.Count:N0}");
            Console.WriteLine($"ZipCode pool: {zipCodePool.Count:N0}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 錯誤: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static int GetSpecificity(PostalRuleData rule)
    {
        int score = 0;
        if (rule.LaneStart.HasValue)  score += 1000;
        if (rule.AlleyStart.HasValue) score += 500;
        if (rule.NumberStartSub.HasValue && rule.NumberStartSub.Value > 0) score += 100;
        if (rule.NumberEndSub.HasValue && rule.NumberEndSub.Value > 0 && rule.NumberEndSub.Value < int.MaxValue) score += 100;
        if (rule.NumberStart.HasValue && rule.NumberEnd.HasValue && rule.NumberStart.Value == rule.NumberEnd.Value) score += 50;
        if (rule.NumberStart.HasValue || rule.NumberEnd.HasValue) score += 20;
        if (rule.EvenOdd.HasValue && rule.EvenOdd.Value != 0) score += 10;
        return score;
    }

    static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    static string IntArray(int[] arr) =>
        string.Join(", ", arr.Select(v => v.ToString()));

    static string IntArrayMaxValue(int[] arr) =>
        string.Join(", ", arr.Select(v => v == int.MaxValue ? "int.MaxValue" : v.ToString()));

    static string ByteArray(byte[] arr) =>
        string.Join(", ", arr.Select(v => v.ToString()));

    static List<string[]> ReadDbfFile(string dbfPath)
    {
        var rows = new List<string[]>();

        using var stream = File.OpenRead(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(stream, new DbfDataReader.DbfDataReaderOptions
        {
            Encoding = Encoding.GetEncoding("big5")
        });

        // 取得欄位索引（向後相容：7 個基本欄位）
        var cityIdx = reader.GetOrdinal("CITY");
        var areaIdx = reader.GetOrdinal("AREA");
        var roadIdx = reader.GetOrdinal("ROAD");
        var zipcodeIdx = reader.GetOrdinal("ZIPCODE");
        var scopeIdx = reader.GetOrdinal("SCOOP");
        var deptIdx = reader.GetOrdinal("DEPARTMENT");
        var officeIdx = reader.GetOrdinal("OFFICE");

        int count = 0;
        while (reader.Read())
        {
            try
            {
                var city = reader.GetString(cityIdx)?.Trim() ?? "";
                var area = reader.GetString(areaIdx)?.Trim() ?? "";
                var road = reader.GetString(roadIdx)?.Trim() ?? "";
                var zipcode = reader.GetString(zipcodeIdx)?.Trim() ?? "";
                var scope = reader.GetString(scopeIdx)?.Trim() ?? "";
                var department = reader.GetString(deptIdx)?.Trim() ?? "";
                var office = reader.GetString(officeIdx)?.Trim() ?? "";

                // 過濾無效資料
                if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(zipcode))
                {
                    continue;
                }

                // 格式：郵遞區號,縣市,區域,路名,範圍,部門/大樓,郵局
                var row = new[] { zipcode, city, area, road, scope, department, office };
                rows.Add(row);

                count++;
                if (count % 10000 == 0)
                {
                    Console.Write($"\r正在讀取 DBF 資料... {count} 筆");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n警告: 讀取第 {count + 1} 筆記錄失敗: {ex.Message}");
            }
        }

        return rows;
    }

    /// <summary>
    /// 讀取 DBF 並插入所有結構化欄位到 postal_rules 資料表
    /// </summary>
    static List<PostalRuleData> ReadDbfFileStructured(string dbfPath)
    {
        var rules = new List<PostalRuleData>();

        using var stream = File.OpenRead(dbfPath);
        using var reader = new DbfDataReader.DbfDataReader(stream, new DbfDataReader.DbfDataReaderOptions
        {
            Encoding = Encoding.GetEncoding("big5")
        });

        int count = 0;
        while (reader.Read())
        {
            try
            {
                var city = reader.GetString(reader.GetOrdinal("CITY"))?.Trim() ?? "";
                var area = reader.GetString(reader.GetOrdinal("AREA"))?.Trim() ?? "";
                var zipcode = reader.GetString(reader.GetOrdinal("ZIPCODE"))?.Trim() ?? "";

                // 過濾無效資料
                if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(area) || string.IsNullOrEmpty(zipcode))
                {
                    continue;
                }

                var rule = new PostalRuleData
                {
                    ZipCode = zipcode,
                    City = city,
                    Area = area,
                    Road = reader.GetString(reader.GetOrdinal("ROAD"))?.Trim() ?? "",

                    LaneStart = GetNullableInt(reader, "LANE"),
                    LaneEnd = GetNullableInt(reader, "LANE1"),
                    AlleyStart = GetNullableInt(reader, "ALLEY"),
                    AlleyEnd = GetNullableInt(reader, "ALLEY1"),

                    NumberStart = GetNullableInt(reader, "NO_BGN"),
                    NumberStartSub = GetNullableInt(reader, "NO_BGN1"),
                    NumberEnd = GetNullableInt(reader, "NO_END"),
                    NumberEndSub = GetNullableInt(reader, "NO_END1"),

                    EvenOdd = GetNullableInt(reader, "EVEN"),
                    FloorStart = GetNullableInt(reader, "FLOOR"),
                    FloorEnd = GetNullableInt(reader, "FLOOR1"),

                    Scope = reader.GetString(reader.GetOrdinal("SCOOP"))?.Trim(),
                    Department = reader.GetString(reader.GetOrdinal("DEPARTMENT"))?.Trim(),
                    Office = reader.GetString(reader.GetOrdinal("OFFICE"))?.Trim(),
                    Remark = reader.GetString(reader.GetOrdinal("RMK"))?.Trim(),

                    RoadNo = reader.GetString(reader.GetOrdinal("ROAD_NO"))?.Trim(),
                    Road1 = reader.GetString(reader.GetOrdinal("ROAD1"))?.Trim(),
                    Lane11 = GetNullableInt(reader, "LANE11"),
                    Lane22 = GetNullableInt(reader, "LANE22"),
                    Alley11 = GetNullableInt(reader, "ALLEY11"),
                    Alley22 = GetNullableInt(reader, "ALLEY22")
                };

                rules.Add(rule);

                count++;
                if (count % 10000 == 0)
                {
                    Console.Write($"\r正在讀取結構化資料... {count} 筆");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n警告: 讀取第 {count + 1} 筆記錄失敗: {ex.Message}");
            }
        }

        Console.WriteLine($"\r正在讀取結構化資料... {count} 筆 完成");
        return rules;
    }

    /// <summary>
    /// 輔助方法：從 DBF 讀取 nullable int
    /// </summary>
    static int? GetNullableInt(DbfDataReader.DbfDataReader reader, string columnName)
    {
        try
        {
            var idx = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(idx))
            {
                return null;
            }

            var value = reader.GetInt32(idx);
            return value == 0 ? null : value; // 將 0 視為 NULL
        }
        catch
        {
            return null;
        }
    }

    static List<string[]> ReadJsonFile(string jsonPath)
    {
        var rows = new List<string[]>();

        var json = File.ReadAllText(jsonPath, Encoding.UTF8);
        using var doc = JsonDocument.Parse(json);

        // 遍歷所有縣市
        foreach (var cityProp in doc.RootElement.EnumerateObject())
        {
            var cityName = cityProp.Name;

            if (!cityProp.Value.TryGetProperty("areas", out var areas))
            {
                continue;
            }

            // 遍歷所有區域
            foreach (var areaProp in areas.EnumerateObject())
            {
                var areaName = areaProp.Name;

                if (!areaProp.Value.TryGetProperty("roads", out var roads))
                {
                    continue;
                }

                // 遍歷所有路名
                foreach (var roadProp in roads.EnumerateObject())
                {
                    var roadName = roadProp.Name;

                    if (!roadProp.Value.TryGetProperty("scopes", out var scopes))
                    {
                        continue;
                    }

                    // 遍歷所有範圍規則
                    foreach (var scope in scopes.EnumerateArray())
                    {
                        if (!scope.TryGetProperty("scope", out var scopeValue) ||
                            !scope.TryGetProperty("zipcode", out var zipcodeValue))
                        {
                            continue;
                        }

                        var scopeStr = scopeValue.GetString() ?? "";
                        var zipcode = zipcodeValue.GetInt32().ToString();

                        // 格式：郵遞區號,縣市,區域,路名,範圍
                        var row = new[] {
                            zipcode,
                            cityName,
                            areaName,
                            roadName,
                            scopeStr
                        };
                        rows.Add(row);
                    }
                }
            }
        }

        return rows;
    }

    static List<string[]> ReadCsvFile(string csvPath)
    {
        var rows = new List<string[]>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        };

        // 嘗試不同的編碼
        Encoding[] encodings = [Encoding.UTF8, Encoding.GetEncoding("Big5")];

        foreach (var encoding in encodings)
        {
            try
            {
                using var reader = new StreamReader(csvPath, encoding);
                using var csv = new CsvReader(reader, config);

                // 跳過標題行
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    // 根據 BIG5_ACJ370zip33.csv 格式調整
                    // 欄位: office, zip3a, zipcode, city, area, area1, road, scope, ...
                    var fullZipcode = csv.GetField<string>(2)?.Trim(); // zipcode (5碼)
                    var city = csv.GetField<string>(3)?.Trim();        // city
                    var area = csv.GetField<string>(4)?.Trim();        // area
                    var road = csv.GetField<string>(6)?.Trim();        // road
                    var scope = csv.GetField<string>(7)?.Trim();       // scope

                    // 過濾無效資料
                    if (string.IsNullOrWhiteSpace(fullZipcode) ||
                        string.IsNullOrWhiteSpace(city) ||
                        string.IsNullOrWhiteSpace(area))
                    {
                        continue;
                    }

                    // 格式：郵遞區號,縣市,區域,路名,範圍
                    var row = new[] {
                        fullZipcode,
                        city,
                        area,
                        road ?? "",
                        scope ?? ""
                    };
                    rows.Add(row);
                }

                break; // 成功讀取，跳出迴圈
            }
            catch
            {
                rows.Clear();
                continue;
            }
        }

        return rows;
    }

    static string GetGitCommitSha()
    {
        try
        {
            // 從 AssemblyInformationalVersion 解析 Git Commit SHA
            // 格式範例: "1.0.0+df4139bcbc86c5f118bc2e1086723df4300a4a81"
            var assembly = typeof(Program).Assembly;
            var versionAttr = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            if (versionAttr?.InformationalVersion != null)
            {
                var parts = versionAttr.InformationalVersion.Split('+');
                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    return parts[1];
                }
            }

            return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    // ── Nested-switch codegen ────────────────────────────────────────────────

    static void WriteNestedSwitchFile(
        StreamWriter sw,
        List<(string key, int count,
            int[] ns, int[] ne, byte[] hlc, int[] ls, int[] le,
            byte[] hac, int[] als, int[] ale,
            byte[] eo, int[] nss, int[] nse,
            int[] zi, int[] di, int[] oi, int[] sci)> groupData,
        string generatedDate, int recordCount)
    {
        // Build city → district → road hierarchy from flat groupData
        var entries = groupData.Select((g, i) => {
            var k  = g.key;
            var p1 = k.IndexOf('|');
            var p2 = k.IndexOf('|', p1 + 1);
            return (idx: i, city: k[..p1], district: k[(p1 + 1)..p2], road: k[(p2 + 1)..]);
        }).ToList();

        var cities = entries
            .GroupBy(e => e.city, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select((g, ci) => (ci, name: g.Key,
                districts: g
                    .GroupBy(e => e.district, StringComparer.Ordinal)
                    .OrderBy(d => d.Key, StringComparer.Ordinal)
                    .Select((d, di) => (di, name: d.Key,
                        roads: d.OrderBy(r => r.road, StringComparer.Ordinal).ToList()))
                    .ToList()))
            .ToList();

        // Header
        sw.WriteLine("// <auto-generated/>");
        sw.WriteLine("// SPDX-License-Identifier: MIT");
        sw.WriteLine("// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0");
        sw.WriteLine("// This file is generated by: dotnet run --project tools/postal/Postal.Builder -- codegen [dbf] [output] --nested");
        sw.WriteLine("// DO NOT EDIT MANUALLY.");
        sw.WriteLine($"// Generated: {generatedDate} | Records: {recordCount:N0}");
        sw.WriteLine();
        sw.WriteLine("#nullable enable");
        sw.WriteLine();
        sw.WriteLine("namespace TaiwanUtilities.Internals;");
        sw.WriteLine();
        sw.WriteLine("internal static class PostalLookup");
        sw.WriteLine("{");

        // s_Rules array backed by split InitSets methods (same split trick as InitRulesN)
        const int SETS_PER_METHOD = 80;
        int setMethodCount = (groupData.Count + SETS_PER_METHOD - 1) / SETS_PER_METHOD;

        sw.WriteLine($"    private static readonly PostalRuleSet[] s_Rules = CreateRuleSets();");
        sw.WriteLine();
        sw.WriteLine("    private static PostalRuleSet[] CreateRuleSets()");
        sw.WriteLine("    {");
        sw.WriteLine($"        var r = new PostalRuleSet[{groupData.Count}];");
        for (int m = 0; m < setMethodCount; m++)
            sw.WriteLine($"        InitSets{m}(r);");
        sw.WriteLine("        return r;");
        sw.WriteLine("    }");
        sw.WriteLine();

        for (int m = 0; m < setMethodCount; m++)
        {
            sw.WriteLine($"    private static void InitSets{m}(PostalRuleSet[] r)");
            sw.WriteLine("    {");
            int start = m * SETS_PER_METHOD;
            int end   = Math.Min(start + SETS_PER_METHOD, groupData.Count);
            for (int gi = start; gi < end; gi++)
            {
                var (_, cnt, ns, ne, hlc, ls, le, hac, als, ale, eo, nss, nse, zi, di, oi, sci) = groupData[gi];
                sw.WriteLine($"        r[{gi}] = new PostalRuleSet(");
                sw.WriteLine($"            {cnt},");
                sw.WriteLine($"            new int[] {{ {IntArray(ns)} }},");
                sw.WriteLine($"            new int[] {{ {IntArrayMaxValue(ne)} }},");
                sw.WriteLine($"            new byte[] {{ {ByteArray(hlc)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(ls)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(le)} }},");
                sw.WriteLine($"            new byte[] {{ {ByteArray(hac)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(als)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(ale)} }},");
                sw.WriteLine($"            new byte[] {{ {ByteArray(eo)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(nss)} }},");
                sw.WriteLine($"            new int[] {{ {IntArrayMaxValue(nse)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(zi)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(di)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(oi)} }},");
                sw.WriteLine($"            new int[] {{ {IntArray(sci)} }}");
                sw.WriteLine("        );");
            }
            sw.WriteLine("    }");
            sw.WriteLine();
        }

        // TryFind entry point (city switch)
        sw.WriteLine("    internal static bool TryFind(string? city, string? district, string? road, out PostalRuleSet ruleSet)");
        sw.WriteLine("    {");
        sw.WriteLine("        switch (city)");
        sw.WriteLine("        {");
        foreach (var (ci, cityName, _) in cities)
            sw.WriteLine($"            case \"{EscapeString(cityName)}\": return TryFind_C{ci}(district, road, out ruleSet);");
        sw.WriteLine("        }");
        sw.WriteLine("        ruleSet = default;");
        sw.WriteLine("        return false;");
        sw.WriteLine("    }");
        sw.WriteLine();

        // Per-city methods (district switch)
        foreach (var (ci, cityName, districts) in cities)
        {
            sw.WriteLine($"    private static bool TryFind_C{ci}(string? district, string? road, out PostalRuleSet ruleSet)");
            sw.WriteLine("    {");
            sw.WriteLine("        switch (district)");
            sw.WriteLine("        {");
            foreach (var (di, distName, _) in districts)
                sw.WriteLine($"            case \"{EscapeString(distName)}\": return TryFind_C{ci}_D{di}(road, out ruleSet);");
            sw.WriteLine("        }");
            sw.WriteLine("        ruleSet = default;");
            sw.WriteLine("        return false;");
            sw.WriteLine("    }");
            sw.WriteLine();
        }

        // Per-city-district methods (road switch → leaf)
        foreach (var (ci, _, districts) in cities)
        {
            foreach (var (di, _, roads) in districts)
            {
                sw.WriteLine($"    private static bool TryFind_C{ci}_D{di}(string? road, out PostalRuleSet ruleSet)");
                sw.WriteLine("    {");
                sw.WriteLine("        switch (road)");
                sw.WriteLine("        {");
                foreach (var (idx, _, _, roadName) in roads)
                    sw.WriteLine($"            case \"{EscapeString(roadName)}\": ruleSet = s_Rules[{idx}]; return true;");
                sw.WriteLine("        }");
                sw.WriteLine("        ruleSet = default;");
                sw.WriteLine("        return false;");
                sw.WriteLine("    }");
                sw.WriteLine();
            }
        }

        sw.WriteLine("}");
    }

    static void WriteDatabaseInfo(string dbPath, int recordCount, string sourceFile)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // 建立版本資訊表
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS database_info (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }

        // 寫入版本資訊
        var now = DateTime.UtcNow;
        var version = now.ToString("yyyy-MM-dd");
        var createdAt = now.ToString("o"); // ISO 8601 格式
        var sourceName = Path.GetFileName(sourceFile);
        var commitSha = GetGitCommitSha();

        var info = new Dictionary<string, string>
        {
            { "version", version },
            { "created_at", createdAt },
            { "source_file", sourceName },
            { "record_count", recordCount.ToString() },
            { "builder_version", "1.0.0" },
            { "commit_sha", commitSha }
        };

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR REPLACE INTO database_info (key, value) VALUES (@key, @value)";

            foreach (var (key, value) in info)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@value", value);
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine($"✓ 寫入資料庫版本資訊: {version} ({createdAt})");
        if (commitSha != "unknown")
        {
            Console.WriteLine($"  Commit SHA: {commitSha}");
        }
    }
}
