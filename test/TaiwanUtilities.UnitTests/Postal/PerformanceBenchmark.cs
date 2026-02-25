// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys

namespace TaiwanUtilities.UnitTests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// 效能基準測試（非正式，僅供參考）
/// </summary>
[Collection("DatabaseSingleton")]
public class PerformanceBenchmark
{
    private readonly ITestOutputHelper _output;

    public PerformanceBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_PreloadedVsSQLite_SingleQuery()
    {
        var testAddress = "臺北市信義區市府路1號";

        // 預熱
        PostalRulesEngine.Warmup();
        ZipCode.Find(testAddress);

        // 測試預載入引擎
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var addr = PostalAddress.Parse(testAddress);
            var result = PostalRulesEngine.Find(addr);
        }
        sw1.Stop();

        // 測試 SQLite（禁用預載入）
        Environment.SetEnvironmentVariable("PEAK_ENABLE_PRELOADED", "0");
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var result = ZipCode.Find(testAddress);
        }
        sw2.Stop();
        Environment.SetEnvironmentVariable("PEAK_ENABLE_PRELOADED", "1");

        var preloadedAvg = sw1.ElapsedMilliseconds / 1000.0;
        var sqliteAvg = sw2.ElapsedMilliseconds / 1000.0;
        var improvement = sqliteAvg / preloadedAvg;

        _output.WriteLine($"預載入引擎：平均 {preloadedAvg:F3} ms/query");
        _output.WriteLine($"SQLite 查詢：平均 {sqliteAvg:F3} ms/query");
        _output.WriteLine($"效能提升：{improvement:F1}x");

        // 僅記錄效能數據，不斷言快慢（結果受執行環境影響）
    }

    [Fact]
    public void Benchmark_ConcurrentQueries()
    {
        var testAddress = "臺北市信義區市府路1號";

        // 預熱
        PostalRulesEngine.Warmup();

        // 100 個並發查詢 - 預載入引擎
        var sw1 = Stopwatch.StartNew();
        Parallel.For(0, 100, _ =>
        {
            var addr = PostalAddress.Parse(testAddress);
            var result = PostalRulesEngine.Find(addr);
        });
        sw1.Stop();

        // 100 個並發查詢 - SQLite
        Environment.SetEnvironmentVariable("PEAK_ENABLE_PRELOADED", "0");
        var sw2 = Stopwatch.StartNew();
        Parallel.For(0, 100, _ =>
        {
            var result = ZipCode.Find(testAddress);
        });
        sw2.Stop();
        Environment.SetEnvironmentVariable("PEAK_ENABLE_PRELOADED", "1");

        var preloadedTotal = sw1.ElapsedMilliseconds;
        var sqliteTotal = sw2.ElapsedMilliseconds;
        var improvement = (double)sqliteTotal / preloadedTotal;

        _output.WriteLine($"預載入引擎（100 併發）：{preloadedTotal} ms");
        _output.WriteLine($"SQLite 查詢（100 併發）：{sqliteTotal} ms");
        _output.WriteLine($"效能提升：{improvement:F1}x");

        // 僅記錄效能數據，不斷言快慢（結果受執行環境影響）
    }

    [Fact]
    public void Benchmark_MemoryUsage()
    {
        // 預熱
        PostalRulesEngine.Warmup();

        var (keys, rules, bytes) = PostalRulesEngine.GetStats();
        var mb = bytes / 1024.0 / 1024.0;

        _output.WriteLine($"索引鍵數量：{keys:N0}");
        _output.WriteLine($"規則總數：{rules:N0}");
        _output.WriteLine($"記憶體占用：{mb:F2} MB");

        Assert.True(mb < 50, "記憶體占用應小於 50 MB");
        Assert.True(rules > 0, "規則數應大於 0");
    }
}
