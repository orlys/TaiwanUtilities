namespace TaiwanUtilities.Builder;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Data.Sqlite;

/// <summary>
/// 從資料庫隨機產生符合投遞規則的地址
/// </summary>
public class AddressGenerator
{
    private readonly string _dbPath;
    private readonly Random _random = new Random();

    public AddressGenerator(string dbPath)
    {
        _dbPath = dbPath;
    }

    /// <summary>
    /// 從資料庫隨機挑選規則並生成地址
    /// </summary>
    public List<GeneratedAddress> GenerateRandomAddresses(int count = 10)
    {
        var addresses = new List<GeneratedAddress>();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        // 檢查資料表是否存在
        var hasPostalRules = CheckTableExists(connection, "postal_rules");

        if (hasPostalRules)
        {
            addresses.AddRange(GenerateFromPostalRules(connection, count));
        }
        else
        {
            addresses.AddRange(GenerateFromPreciseTable(connection, count));
        }

        return addresses;
    }

    /// <summary>
    /// 從 postal_rules 資料表生成地址（新架構）
    /// </summary>
    private List<GeneratedAddress> GenerateFromPostalRules(SqliteConnection connection, int count)
    {
        var addresses = new List<GeneratedAddress>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT id, zipcode, city, area, road,
                   lane_start, lane_end, alley_start, alley_end,
                   number_start, number_start_sub, number_end, number_end_sub,
                   even_odd, scope
            FROM postal_rules
            ORDER BY RANDOM()
            LIMIT @count";
        cmd.Parameters.AddWithValue("@count", count * 2); // 多取一些以防某些規則無法生成

        using var reader = cmd.ExecuteReader();
        while (reader.Read() && addresses.Count < count)
        {
            try
            {
                var zipcode = reader.GetString(1);
                var city = reader.GetString(2);
                var area = reader.GetString(3);
                var road = reader.GetString(4);

                var laneStart = GetNullableInt(reader, 5);
                var laneEnd = GetNullableInt(reader, 6);
                var alleyStart = GetNullableInt(reader, 7);
                var alleyEnd = GetNullableInt(reader, 8);

                var numberStart = GetNullableInt(reader, 9);
                var numberStartSub = GetNullableInt(reader, 10);
                var numberEnd = GetNullableInt(reader, 11);
                var numberEndSub = GetNullableInt(reader, 12);

                var evenOdd = GetNullableInt(reader, 13);
                var scope = reader.IsDBNull(14) ? "" : reader.GetString(14);

                // 生成符合規則的門牌號碼
                var address = GenerateAddressFromRule(
                    city, area, road, scope,
                    laneStart, laneEnd,
                    alleyStart, alleyEnd,
                    numberStart, numberStartSub,
                    numberEnd, numberEndSub,
                    evenOdd
                );

                if (address != null)
                {
                    addresses.Add(new GeneratedAddress
                    {
                        FullAddress = address,
                        ExpectedZipCode = zipcode,
                        City = city,
                        Area = area,
                        Road = road,
                        RuleScope = scope,
                        Source = "postal_rules"
                    });
                }
            }
            catch
            {
                // 跳過無法生成的規則
                continue;
            }
        }

        return addresses;
    }

    /// <summary>
    /// 從 precise 資料表生成地址（舊架構向後相容）
    /// </summary>
    private List<GeneratedAddress> GenerateFromPreciseTable(SqliteConnection connection, int count)
    {
        var addresses = new List<GeneratedAddress>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT addr_str, zipcode
            FROM precise
            WHERE addr_str LIKE '%路%' OR addr_str LIKE '%街%' OR addr_str LIKE '%道%'
            ORDER BY RANDOM()
            LIMIT @count";
        cmd.Parameters.AddWithValue("@count", count);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var addrStr = reader.GetString(0);
            var zipcode = reader.GetString(1);

            addresses.Add(new GeneratedAddress
            {
                FullAddress = addrStr + "1號",
                ExpectedZipCode = zipcode,
                RuleScope = "從 precise 表生成",
                Source = "precise"
            });
        }

        return addresses;
    }

    /// <summary>
    /// 根據規則生成地址字串
    /// </summary>
    private string? GenerateAddressFromRule(
        string city, string area, string road, string scope,
        int? laneStart, int? laneEnd,
        int? alleyStart, int? alleyEnd,
        int? numberStart, int? numberStartSub,
        int? numberEnd, int? numberEndSub,
        int? evenOdd)
    {
        var parts = new List<string> { city, area, road };

        // 生成巷號（如果有範圍）
        if (laneStart.HasValue)
        {
            var lane = GenerateNumberInRange(laneStart.Value, laneEnd);
            parts.Add($"{lane}巷");
        }

        // 生成弄號（如果有範圍）
        if (alleyStart.HasValue)
        {
            var alley = GenerateNumberInRange(alleyStart.Value, alleyEnd);
            parts.Add($"{alley}弄");
        }

        // 生成門牌號碼
        if (numberStart.HasValue)
        {
            var number = GenerateNumberInRange(numberStart.Value, numberEnd, evenOdd);

            if (number == null)
            {
                return null; // 無法生成符合單雙號規則的號碼
            }

            var numberStr = number.Value.ToString();

            // 生成附號（如果有範圍）
            if (numberStartSub.HasValue && numberStartSub.Value > 0)
            {
                var subNumber = GenerateNumberInRange(numberStartSub.Value, numberEndSub);
                numberStr += $"之{subNumber}";
            }

            parts.Add($"{numberStr}號");
        }
        else
        {
            // 沒有門牌號碼規則，使用預設值
            parts.Add("1號");
        }

        return string.Join("", parts);
    }

    /// <summary>
    /// 在範圍內生成隨機數字（考慮單雙號規則）
    /// </summary>
    private int? GenerateNumberInRange(int start, int? end, int? evenOdd = null)
    {
        var max = end ?? start;
        if (max < start)
        {
            max = start;
        }

        // 限制範圍避免生成過大的數字
        if (max - start > 1000)
        {
            max = start + 1000;
        }

        // 如果有單雙號規則
        if (evenOdd.HasValue && evenOdd.Value != 0)
        {
            var isEven = evenOdd.Value == 2;
            var candidates = new List<int>();

            for (int i = start; i <= max && candidates.Count < 100; i++)
            {
                if (isEven && i % 2 == 0)
                {
                    candidates.Add(i);
                }
                else if (!isEven && i % 2 == 1)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[_random.Next(candidates.Count)];
        }

        // 沒有單雙號規則，直接隨機
        var range = Math.Min(max - start + 1, 100); // 限制範圍
        return start + _random.Next(range);
    }

    /// <summary>
    /// 檢查資料表是否存在
    /// </summary>
    private bool CheckTableExists(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);

        var result = cmd.ExecuteScalar();
        return result != null;
    }

    /// <summary>
    /// 輔助方法：讀取 nullable int
    /// </summary>
    private int? GetNullableInt(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetInt32(ordinal);
        return value == 0 ? null : value;
    }

    /// <summary>
    /// 統計資料庫中的規則分佈
    /// </summary>
    public DatabaseStatistics GetDatabaseStatistics()
    {
        var stats = new DatabaseStatistics();

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        var hasPostalRules = CheckTableExists(connection, "postal_rules");

        if (hasPostalRules)
        {
            using var cmd = connection.CreateCommand();

            // 總記錄數
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules";
            stats.TotalRules = Convert.ToInt32(cmd.ExecuteScalar());

            // 有巷號的記錄數
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE lane_start IS NOT NULL";
            stats.RulesWithLane = Convert.ToInt32(cmd.ExecuteScalar());

            // 有弄號的記錄數
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE alley_start IS NOT NULL";
            stats.RulesWithAlley = Convert.ToInt32(cmd.ExecuteScalar());

            // 有單雙號規則的記錄數
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE even_odd IS NOT NULL AND even_odd != 0";
            stats.RulesWithEvenOdd = Convert.ToInt32(cmd.ExecuteScalar());

            // 單號規則
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE even_odd = 1";
            stats.RulesOddOnly = Convert.ToInt32(cmd.ExecuteScalar());

            // 雙號規則
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE even_odd = 2";
            stats.RulesEvenOnly = Convert.ToInt32(cmd.ExecuteScalar());

            // 有附號的記錄數
            cmd.CommandText = "SELECT COUNT(*) FROM postal_rules WHERE number_start_sub IS NOT NULL AND number_start_sub > 0";
            stats.RulesWithSubNumber = Convert.ToInt32(cmd.ExecuteScalar());

            // 不同的路名數量
            cmd.CommandText = "SELECT COUNT(DISTINCT road) FROM postal_rules";
            stats.UniqueRoads = Convert.ToInt32(cmd.ExecuteScalar());

            // 特殊路名（不以 路/街/道 結尾）
            cmd.CommandText = @"
                SELECT COUNT(DISTINCT road)
                FROM postal_rules
                WHERE road NOT LIKE '%路'
                  AND road NOT LIKE '%街'
                  AND road NOT LIKE '%道'";
            stats.SpecialRoadNames = Convert.ToInt32(cmd.ExecuteScalar());
        }

        return stats;
    }
}

/// <summary>
/// 生成的地址資訊
/// </summary>
public record GeneratedAddress
{
    public string FullAddress { get; init; } = string.Empty;
    public string ExpectedZipCode { get; init; } = string.Empty;
    public string? City { get; init; }
    public string? Area { get; init; }
    public string? Road { get; init; }
    public string RuleScope { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}

/// <summary>
/// 資料庫統計資訊
/// </summary>
public record DatabaseStatistics
{
    public int TotalRules { get; set; }
    public int RulesWithLane { get; set; }
    public int RulesWithAlley { get; set; }
    public int RulesWithEvenOdd { get; set; }
    public int RulesOddOnly { get; set; }
    public int RulesEvenOnly { get; set; }
    public int RulesWithSubNumber { get; set; }
    public int UniqueRoads { get; set; }
    public int SpecialRoadNames { get; set; }
}
