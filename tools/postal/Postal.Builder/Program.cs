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

                case "analyze-roads":
                case "roads":
                    return AnalyzeRoadsCommand(args);

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

        Console.WriteLine("=== 生成 PostalData.g.cs ===");
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

            // 5. 階層排序（縣市 → 行政區 → 路名，各層 Ordinal，需與 PostalLookup 的二分搜尋一致）
            //    並攤平為階層索引 + 全域 SoA（primitive initializer → RVA blob，啟動零配置）
            var flatGroups = groups
                .Select(kvp =>
                {
                    var k  = kvp.Key;
                    var p1 = k.IndexOf('|');
                    var p2 = k.IndexOf('|', p1 + 1);
                    return (city: k[..p1], district: k[(p1 + 1)..p2], road: k[(p2 + 1)..], rules: kvp.Value);
                })
                .OrderBy(e => e.city, StringComparer.Ordinal)
                .ThenBy(e => e.district, StringComparer.Ordinal)
                .ThenBy(e => e.road, StringComparer.Ordinal)
                .ToList();

            var cityNames            = new List<string>();
            var cityDistrictOffsets  = new List<int> { 0 };
            var districtNames        = new List<string>();
            var districtGroupOffsets = new List<int> { 0 };
            var roadBlob             = new StringBuilder();
            var roadOffsets          = new List<int> { 0 };
            var groupRuleOffsets     = new List<int> { 0 };

            int total = rules.Count;
            var ns  = new List<int>(total); var ne  = new List<int>(total);
            var ls  = new List<int>(total); var le  = new List<int>(total);
            var als = new List<int>(total); var ale = new List<int>(total);
            var nss = new List<int>(total); var nse = new List<int>(total);
            var ruleFlags = new List<int>(total);
            var zi  = new List<int>(total); var di  = new List<int>(total);
            var oi  = new List<int>(total); var sci = new List<int>(total);

            foreach (var cityGroup in flatGroups.GroupBy(e => e.city))
            {
                cityNames.Add(cityGroup.Key);
                foreach (var distGroup in cityGroup.GroupBy(e => e.district))
                {
                    districtNames.Add(distGroup.Key);
                    foreach (var entry in distGroup)
                    {
                        roadBlob.Append(entry.road);
                        roadOffsets.Add(roadBlob.Length);

                        foreach (var r in entry.rules)
                        {
                            ns.Add(r.NumberStart ?? 0);
                            ne.Add(r.NumberEnd ?? int.MaxValue);

                            // RuleFlags 位元佈局：bit0 HasLane, bit1 HasAlley, bits2-3 EvenOdd
                            int f = 0;
                            if (r.LaneStart.HasValue)
                            {
                                f |= 1;
                                ls.Add(r.LaneStart.Value);
                                le.Add(r.LaneEnd ?? r.LaneStart.Value);
                            }
                            else { ls.Add(0); le.Add(0); }

                            if (r.AlleyStart.HasValue)
                            {
                                f |= 2;
                                als.Add(r.AlleyStart.Value);
                                ale.Add(r.AlleyEnd ?? r.AlleyStart.Value);
                            }
                            else { als.Add(0); ale.Add(0); }

                            int eoVal = r.EvenOdd ?? 0;
                            if (eoVal < 0 || eoVal > 2)
                                throw new InvalidOperationException($"EvenOdd 值 {eoVal} 超出 flags 編碼範圍（0-2）");
                            f |= eoVal << 2;
                            ruleFlags.Add(f);

                            nss.Add(r.NumberStartSub ?? 0);
                            nse.Add(r.NumberEndSub ?? int.MaxValue);
                            zi.Add(GetOrAddZip(r.ZipCode));
                            di.Add(GetOrAddPool(departments, deptIndex, r.Department));
                            oi.Add(GetOrAddPool(offices, officeIndex, r.Office));
                            sci.Add(GetOrAddPool(scopes, scopeIndex, r.Scope));
                        }
                        groupRuleOffsets.Add(ns.Count);
                    }
                    districtGroupOffsets.Add(roadOffsets.Count - 1);
                }
                cityDistrictOffsets.Add(districtNames.Count);
            }

            int groupCount = roadOffsets.Count - 1;

            // 6. Write the generated file
            Console.Write("生成 C# 原始碼...");
            var generatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var outDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            using var sw = new StreamWriter(outputPath, false, Encoding.UTF8);

            sw.WriteLine("// <auto-generated/>");
            sw.WriteLine("// SPDX-License-Identifier: MIT");
            sw.WriteLine("// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0");
            sw.WriteLine("// This file is generated by: dotnet run --project tools/postal/Postal.Builder -- codegen [dbf] [output]");
            sw.WriteLine("// DO NOT EDIT MANUALLY.");
            sw.WriteLine($"// Generated: {generatedDate} | Records: {rules.Count:N0} | Groups: {groupCount:N0}");
            sw.WriteLine();
            sw.WriteLine("#nullable enable");
            sw.WriteLine();
            sw.WriteLine("namespace TaiwanUtilities.Internals;");
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

            // ── 階層索引（ordinal 排序，供 PostalLookup 二分搜尋）──
            WriteStringArray(sw, "CityNames", cityNames);
            WriteNumericArray(sw, "int", "CityDistrictOffsets", cityDistrictOffsets);
            WriteStringArray(sw, "DistrictNames", districtNames);
            WriteNumericArray(sw, "int", "DistrictGroupOffsets", districtGroupOffsets);
            WriteRoadBlob(sw, roadBlob.ToString());
            WriteNumericArray(sw, "int", "RoadOffsets", roadOffsets);
            WriteNumericArray(sw, "int", "GroupRuleOffsets", groupRuleOffsets);

            // ── 規則 SoA（全域陣列，PostalRuleSet 以 Offset/Count 切片檢視）──
            WriteNumericArray(sw, "int", "NumberStarts", ns);
            WriteNumericArray(sw, "int", "NumberEnds", ne);
            WriteNumericArray(sw, PickType(ls),  "LaneStarts", ls);
            WriteNumericArray(sw, PickType(le),  "LaneEnds", le);
            WriteNumericArray(sw, PickType(als), "AlleyStarts", als);
            WriteNumericArray(sw, PickType(ale), "AlleyEnds", ale);
            WriteNumericArray(sw, PickType(nss), "SubStarts", nss);
            WriteNumericArray(sw, "int", "SubEnds", nse);
            WriteNumericArray(sw, "byte", "RuleFlags", ruleFlags);
            WriteNumericArray(sw, PickType(zi),  "ZipIdx", zi);
            WriteNumericArray(sw, PickType(di),  "DeptIdx", di);
            WriteNumericArray(sw, PickType(oi),  "OfficeIdx", oi);
            WriteNumericArray(sw, PickType(sci), "ScopeIdx", sci);

            sw.WriteLine("}");

            Console.WriteLine(" 完成！");

            var fi = new FileInfo(outputPath);
            Console.WriteLine($"輸出大小: {fi.Length / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"路索引鍵數: {groupCount:N0}");
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

    /// <summary>依最大值選擇最小可容納的元素型別（RVA blob 尺寸最佳化）。</summary>
    static string PickType(List<int> values)
    {
        int max = 0;
        foreach (var v in values) if (v > max) max = v;
        return max <= ushort.MaxValue ? "ushort" : "int";
    }

    static void WriteNumericArray(StreamWriter sw, string type, string name, List<int> values)
    {
        const int PER_LINE = 120;
        sw.WriteLine($"    internal static readonly {type}[] {name} = new {type}[]");
        sw.WriteLine("    {");
        var sb = new StringBuilder(1024);
        for (int i = 0; i < values.Count; i += PER_LINE)
        {
            sb.Clear();
            sb.Append("        ");
            int end = Math.Min(i + PER_LINE, values.Count);
            for (int j = i; j < end; j++)
            {
                if (type == "int" && values[j] == int.MaxValue) sb.Append("int.MaxValue");
                else sb.Append(values[j]);
                sb.Append(", ");
            }
            sw.WriteLine(sb.ToString());
        }
        sw.WriteLine("    };");
        sw.WriteLine();
    }

    static void WriteStringArray(StreamWriter sw, string name, List<string> values)
    {
        const int PER_LINE = 20;
        sw.WriteLine($"    internal static readonly string[] {name} = new string[]");
        sw.WriteLine("    {");
        for (int i = 0; i < values.Count; i += PER_LINE)
        {
            int end = Math.Min(i + PER_LINE, values.Count);
            sw.WriteLine("        " + string.Join(", ", values.Skip(i).Take(end - i).Select(v => $"\"{EscapeString(v)}\"")) + ",");
        }
        sw.WriteLine("    };");
        sw.WriteLine();
    }

    static void WriteRoadBlob(StreamWriter sw, string blob)
    {
        // 相鄰字面值以 + 串接，由編譯器常數折疊為單一 US-heap 條目
        const int CHUNK = 4000;
        sw.WriteLine("    internal static readonly string RoadBlob =");
        int i = 0;
        while (true)
        {
            int len = Math.Min(CHUNK, blob.Length - i);
            if (i + len < blob.Length && char.IsHighSurrogate(blob[i + len - 1])) len++;
            var piece = blob.Substring(i, len);
            i += len;
            sw.WriteLine($"        \"{EscapeString(piece)}\"{(i < blob.Length ? " +" : ";")}");
            if (i >= blob.Length) break;
        }
        sw.WriteLine();
    }

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
}
