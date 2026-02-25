namespace TaiwanUtilities.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.Sqlite;

/// <summary>
/// 從資料庫中隨機抽取測試資料
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// 從資料庫中隨機抽取指定百分比的地址
    /// </summary>
    public static List<string> GetRandomAddresses(string dbPath, double percentage = 0.1, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var addresses = new List<string>();

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // 從 gradual 表中讀取所有地址
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT addr_str FROM gradual ORDER BY addr_str";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var addr = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(addr))
            {
                addresses.Add(addr);
            }
        }

        // 隨機選擇指定百分比的地址
        var sampleSize = (int)(addresses.Count * percentage);
        var selectedAddresses = addresses
            .OrderBy(_ => random.Next())
            .Take(sampleSize)
            .OrderBy(a => a) // 排序以便於閱讀
            .ToList();

        return selectedAddresses;
    }

    /// <summary>
    /// 生成測試資料檔案
    /// </summary>
    public static void GenerateTestDataFile(string dbPath, string outputPath, double percentage = 0.1)
    {
        var addresses = GetRandomAddresses(dbPath, percentage, seed: 42); // 使用固定 seed 確保可重現

        var lines = new List<string>
        {
            "// 此檔案由 TestDataGenerator 自動生成",
            "// 包含從資料庫中隨機抽取的 " + percentage * 100 + "% 地址",
            "// 總計: " + addresses.Count + " 筆",
            ""
        };

        lines.AddRange(addresses.Select(a => $"\"{a}\","));

        File.WriteAllLines(outputPath, lines);
    }
}
