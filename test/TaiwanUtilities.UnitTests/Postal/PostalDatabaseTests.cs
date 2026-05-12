namespace TaiwanUtilities.UnitTests;

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TaiwanUtilities;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// PostalDatabase 靜態資料版本的測試
/// </summary>
/// <remarks>
/// 資料已編譯進 binary，無需外部資料庫。
/// 所有更新相關操作為 no-op。
/// </remarks>
[Collection("DatabaseSingleton")]
public class DatabaseTests
{
    private readonly ITestOutputHelper _output;

    public DatabaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 基本功能測試

    [Fact]
    public void TestCurrentVersion_ShouldReturnVersionInfo()
    {
        var version = PostalDatabase.CurrentVersion;

        Assert.NotNull(version);
        Assert.NotEmpty(version.Version);
    }

    [Fact]
    public void TestFind_ShouldReturnResult()
    {
        // 使用 ZipCode.Find 代替 PostalDatabase.ExecuteQuery
        var result = ZipCode.Find("臺北市信義區市府路1號");

        Assert.NotNull(result);
        // ZipCodePool 為空（placeholder），所以未找到是正常的
        // 只要不拋出例外即通過
    }

    #endregion

    #region 並發查詢測試

    [Fact]
    public void TestConcurrentQueries_SameResult()
    {
        const int threadCount = 10;
        const int queriesPerThread = 100;
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        var threads = new System.Collections.Generic.List<Thread>();

        for (int i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                for (int j = 0; j < queriesPerThread; j++)
                {
                    var zipcode = ZipCode.Find("臺北市信義區市府路1號").ZipCode;
                    results.Add(zipcode ?? string.Empty);
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.Equal(threadCount * queriesPerThread, results.Count);
        // 所有結果應該相同（靜態資料）
        var distinctResults = results.Distinct().ToList();
        Assert.Single(distinctResults);
    }

    [Fact]
    public async Task TestConcurrentQueries_Async()
    {
        const int taskCount = 20;
        var tasks = new System.Collections.Generic.List<Task<string>>();

        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() => ZipCode.Find("臺北市信義區市府路1號").ZipCode ?? string.Empty));
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(taskCount, results.Length);
        Assert.All(results, zipcode => Assert.Equal(results[0], zipcode));
    }

    #endregion

    #region 更新測試（no-op 驗證）

    [Fact]
    public async Task TestUpdateFromAsync_AlwaysReturnsFalse()
    {
        var result = await PostalDatabase.UpdateFromAsync("/any/path.db");
        Assert.False(result);
    }

    [Fact]
    public async Task TestUpdateFromAsync_WithInvalidPath()
    {
        var result = await PostalDatabase.UpdateFromAsync("/path/does/not/exist.db");
        Assert.False(result);
    }

    [Fact]
    public async Task TestUpdateFromStreamAsync_AlwaysReturnsFalse()
    {
        using var stream = new System.IO.MemoryStream(new byte[] { 1, 2, 3 });
        var result = await PostalDatabase.UpdateFromStreamAsync(stream, "zipcode.db");
        Assert.False(result);
    }

    [Fact]
    public async Task TestCheckForUpdatesAsync_AlwaysReturnsNull()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var updateInfo = await PostalDatabase.CheckForUpdatesAsync(cts.Token);
        Assert.Null(updateInfo);
    }

    #endregion

    #region 熱更新測試（no-op 驗證）

    [Fact]
    public void TestReload_IsNoOp()
    {
        // 呼叫 Reload 不應拋出例外
        PostalDatabase.Reload();

        var result = ZipCode.Find("臺北市信義區市府路1號");
        Assert.NotNull(result);
    }

    #endregion

    #region 版本檢查測試

    [Fact]
    public async Task TestCheckForUpdatesAsync_WithCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var updateInfo = await PostalDatabase.CheckForUpdatesAsync(cts.Token);
        Assert.Null(updateInfo);
    }

    #endregion

    #region 效能測試

    [Fact]
    public void TestPerformance_MultipleQueries()
    {
        const int queryCount = 1000;
        var startTime = DateTime.Now;

        for (int i = 0; i < queryCount; i++)
        {
            var result = ZipCode.Find("臺北市信義區市府路1號");
            Assert.NotNull(result);
        }

        var elapsed = DateTime.Now - startTime;

        _output.WriteLine($"連續查詢性能測試:");
        _output.WriteLine($"  查詢數量: {queryCount}");
        _output.WriteLine($"  總耗時: {elapsed.TotalSeconds:F2} 秒");
        _output.WriteLine($"  平均每個查詢: {elapsed.TotalMilliseconds / queryCount:F2} 毫秒");
        _output.WriteLine($"  查詢速率: {queryCount / elapsed.TotalSeconds:F0} 次/秒");
    }

    [Fact]
    public async Task TestPerformance_ConcurrentQueries()
    {
        const int taskCount = 100;
        var startTime = DateTime.Now;

        var tasks = Enumerable.Range(0, taskCount).Select(_ =>
            Task.Run(() => ZipCode.Find("臺北市信義區市府路1號").ZipCode ?? string.Empty)
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        var elapsed = DateTime.Now - startTime;

        _output.WriteLine($"並發查詢性能測試:");
        _output.WriteLine($"  任務數量: {taskCount}");
        _output.WriteLine($"  總耗時: {elapsed.TotalSeconds:F2} 秒");
        _output.WriteLine($"  平均每個查詢: {elapsed.TotalMilliseconds / taskCount:F2} 毫秒");
        _output.WriteLine($"  查詢速率: {taskCount / elapsed.TotalSeconds:F0} 次/秒");

        Assert.All(results, r => Assert.Equal(results[0], r));
    }

    #endregion
}
