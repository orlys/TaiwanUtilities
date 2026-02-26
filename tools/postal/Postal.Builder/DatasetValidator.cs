namespace TaiwanUtilities.Builder;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>
/// 驗證郵遞區號資料集的工具類別
/// </summary>
public class DatasetValidator
{
    private readonly List<ValidationIssue> _issues = new();
    private readonly HashSet<string> _seenAddresses = new();
    private int _totalEntries = 0;
    private int _totalCities = 0;
    private int _totalAreas = 0;
    private int _totalRoads = 0;

    public class ValidationIssue
    {
        public string Severity { get; set; } = string.Empty; // Error, Warning, Info
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public Dictionary<string, object> Statistics { get; set; } = new();
    }

    /// <summary>
    /// 驗證 JSON 資料集檔案
    /// </summary>
    public ValidationResult Validate(string jsonPath)
    {
        _issues.Clear();
        _seenAddresses.Clear();
        _totalEntries = 0;
        _totalCities = 0;
        _totalAreas = 0;
        _totalRoads = 0;

        if (!File.Exists(jsonPath))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "File",
                Message = $"檔案不存在: {jsonPath}"
            });

            return new ValidationResult
            {
                IsValid = false,
                Issues = _issues
            };
        }

        try
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);

            ValidateJsonStructure(doc.RootElement);

            var stats = new Dictionary<string, object>
            {
                ["TotalCities"] = _totalCities,
                ["TotalAreas"] = _totalAreas,
                ["TotalRoads"] = _totalRoads,
                ["TotalEntries"] = _totalEntries,
                ["TotalIssues"] = _issues.Count,
                ["Errors"] = _issues.Count(i => i.Severity == "Error"),
                ["Warnings"] = _issues.Count(i => i.Severity == "Warning"),
                ["Info"] = _issues.Count(i => i.Severity == "Info")
            };

            return new ValidationResult
            {
                IsValid = !_issues.Any(i => i.Severity == "Error"),
                Issues = _issues,
                Statistics = stats
            };
        }
        catch (JsonException ex)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "JSON",
                Message = $"JSON 解析失敗: {ex.Message}"
            });

            return new ValidationResult
            {
                IsValid = false,
                Issues = _issues
            };
        }
    }

    private void ValidateJsonStructure(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "根元素必須是物件"
            });
            return;
        }

        foreach (var cityProp in root.EnumerateObject())
        {
            _totalCities++;
            var cityName = cityProp.Name;

            ValidateCity(cityName, cityProp.Value);
        }
    }

    private void ValidateCity(string cityName, JsonElement cityElement)
    {
        if (cityElement.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "縣市元素必須是物件",
                Location = cityName
            });
            return;
        }

        // 檢查必要欄位
        if (!cityElement.TryGetProperty("en", out _))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "MissingField",
                Message = "缺少英文名稱 (en)",
                Location = cityName
            });
        }

        if (!cityElement.TryGetProperty("areas", out var areas))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "MissingField",
                Message = "缺少區域資料 (areas)",
                Location = cityName
            });
            return;
        }

        if (areas.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "areas 必須是物件",
                Location = cityName
            });
            return;
        }

        foreach (var areaProp in areas.EnumerateObject())
        {
            _totalAreas++;
            var areaName = areaProp.Name;
            ValidateArea(cityName, areaName, areaProp.Value);
        }
    }

    private void ValidateArea(string cityName, string areaName, JsonElement areaElement)
    {
        var location = $"{cityName}/{areaName}";

        if (areaElement.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "區域元素必須是物件",
                Location = location
            });
            return;
        }

        if (!areaElement.TryGetProperty("en", out _))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "MissingField",
                Message = "缺少英文名稱 (en)",
                Location = location
            });
        }

        if (!areaElement.TryGetProperty("roads", out var roads))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "MissingField",
                Message = "缺少道路資料 (roads)",
                Location = location
            });
            return;
        }

        if (roads.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "roads 必須是物件",
                Location = location
            });
            return;
        }

        foreach (var roadProp in roads.EnumerateObject())
        {
            _totalRoads++;
            var roadName = roadProp.Name;
            ValidateRoad(cityName, areaName, roadName, roadProp.Value);
        }
    }

    private void ValidateRoad(string cityName, string areaName, string roadName, JsonElement roadElement)
    {
        var location = $"{cityName}/{areaName}/{roadName}";

        if (roadElement.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "道路元素必須是物件",
                Location = location
            });
            return;
        }

        if (!roadElement.TryGetProperty("en", out _))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "MissingField",
                Message = "缺少英文名稱 (en)",
                Location = location
            });
        }

        if (!roadElement.TryGetProperty("scopes", out var scopes))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "MissingField",
                Message = "缺少範圍資料 (scopes)",
                Location = location
            });
            return;
        }

        if (scopes.ValueKind != JsonValueKind.Array)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "scopes 必須是陣列",
                Location = location
            });
            return;
        }

        int scopeIndex = 0;
        foreach (var scope in scopes.EnumerateArray())
        {
            ValidateScope(cityName, areaName, roadName, scopeIndex, scope);
            scopeIndex++;
        }
    }

    private void ValidateScope(string cityName, string areaName, string roadName, int scopeIndex, JsonElement scope)
    {
        var location = $"{cityName}/{areaName}/{roadName}[{scopeIndex}]";

        _totalEntries++;

        if (scope.ValueKind != JsonValueKind.Object)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "Structure",
                Message = "範圍元素必須是物件",
                Location = location
            });
            return;
        }

        // 檢查必要欄位
        if (!scope.TryGetProperty("scope", out var scopeValue))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "MissingField",
                Message = "缺少 scope 欄位",
                Location = location
            });
        }
        else if (scopeValue.ValueKind != JsonValueKind.String)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "InvalidValue",
                Message = "scope 必須是字串",
                Location = location
            });
        }

        if (!scope.TryGetProperty("zipcode", out var zipcodeValue))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "MissingField",
                Message = "缺少 zipcode 欄位",
                Location = location
            });
            return;
        }

        if (zipcodeValue.ValueKind != JsonValueKind.Number)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "InvalidValue",
                Message = "zipcode 必須是數字",
                Location = location
            });
            return;
        }

        var zipcode = zipcodeValue.GetInt32();
        var zipcodeStr = zipcode.ToString();

        // 驗證郵遞區號格式（只能是 3, 5, 或 6 碼）
        if (zipcodeStr.Length != 3 && zipcodeStr.Length != 5 && zipcodeStr.Length != 6)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "InvalidZipcode",
                Message = $"無效的郵遞區號長度: {zipcodeStr} ({zipcodeStr.Length}碼，應為3/5/6碼)",
                Location = location
            });
        }

        // 驗證郵遞區號範圍
        if (zipcode < 100 || zipcode > 999999)
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Error",
                Category = "InvalidZipcode",
                Message = $"郵遞區號超出有效範圍: {zipcode}",
                Location = location
            });
        }

        // 檢查重複地址
        var fullAddress = $"{cityName}{areaName}{roadName}{scopeValue.GetString()}";
        if (_seenAddresses.Contains(fullAddress))
        {
            _issues.Add(new ValidationIssue
            {
                Severity = "Warning",
                Category = "Duplicate",
                Message = $"重複的地址: {fullAddress}",
                Location = location
            });
        }
        else
        {
            _seenAddresses.Add(fullAddress);
        }
    }

    /// <summary>
    /// 輸出驗證報告
    /// </summary>
    public static void PrintReport(ValidationResult result, bool verbose = false)
    {
        Console.WriteLine("=== 資料集驗證報告 ===\n");

        Console.WriteLine("統計資訊:");
        foreach (var kvp in result.Statistics)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }

        Console.WriteLine($"\n驗證結果: {(result.IsValid ? "✅ 通過" : "❌ 失敗")}");

        if (result.Issues.Any())
        {
            Console.WriteLine($"\n發現 {result.Issues.Count} 個問題:\n");

            var errors = result.Issues.Where(i => i.Severity == "Error").ToList();
            var warnings = result.Issues.Where(i => i.Severity == "Warning").ToList();
            var infos = result.Issues.Where(i => i.Severity == "Info").ToList();

            if (errors.Any())
            {
                Console.WriteLine($"❌ 錯誤 ({errors.Count}):");
                foreach (var issue in errors.Take(verbose ? int.MaxValue : 10))
                {
                    Console.WriteLine($"  [{issue.Category}] {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.Location))
                    {
                        Console.WriteLine($"    位置: {issue.Location}");
                    }
                }
                if (!verbose && errors.Count > 10)
                {
                    Console.WriteLine($"  ... 還有 {errors.Count - 10} 個錯誤（使用 --verbose 查看全部）");
                }
            }

            if (warnings.Any())
            {
                Console.WriteLine($"\n⚠️  警告 ({warnings.Count}):");
                foreach (var issue in warnings.Take(verbose ? int.MaxValue : 10))
                {
                    Console.WriteLine($"  [{issue.Category}] {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.Location))
                    {
                        Console.WriteLine($"    位置: {issue.Location}");
                    }
                }
                if (!verbose && warnings.Count > 10)
                {
                    Console.WriteLine($"  ... 還有 {warnings.Count - 10} 個警告（使用 --verbose 查看全部）");
                }
            }

            if (infos.Any())
            {
                Console.WriteLine($"\nℹ️  資訊 ({infos.Count}):");
                foreach (var issue in infos.Take(verbose ? int.MaxValue : 10))
                {
                    Console.WriteLine($"  [{issue.Category}] {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.Location))
                    {
                        Console.WriteLine($"    位置: {issue.Location}");
                    }
                }
                if (!verbose && infos.Count > 10)
                {
                    Console.WriteLine($"  ... 還有 {infos.Count - 10} 則資訊（使用 --verbose 查看全部）");
                }
            }
        }
        else
        {
            Console.WriteLine("\n✅ 未發現任何問題");
        }
    }
}
