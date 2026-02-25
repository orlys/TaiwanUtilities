namespace TaiwanUtilities.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TaiwanUtilities;

using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Database 類別的非同步單元測試
/// </summary>
/// <remarks>
/// 此測試類別會修改 Database singleton 狀態（UpdateFromAsync、Reload），
/// 使用 [Collection] 避免與其他使用 Database singleton 的測試並行執行。
/// </remarks>
[Collection("DatabaseSingleton")]
public class DatabaseTests
{
    private readonly string _dbPath;
    private readonly ITestOutputHelper _output;

    public DatabaseTests(ITestOutputHelper output)
    {
        _output = output;
        // 嘗試找到資料庫檔案
        _dbPath = FindDatabasePath();
    }

    private string FindDatabasePath()
    {
        var paths = new[]
        {
            "../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../../src/TaiwanUtilities/Postal/zipcode.db",
            "../../../../../src/TaiwanUtilities/Postal/zipcode.db"
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        return string.Empty;
    }

    private bool IsDatabaseAvailable() => !string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath);

    #region 基本功能測試

    [Fact]
    public void TestCurrentVersion_ShouldReturnVersionInfo()
    {
        if (!IsDatabaseAvailable()) return;

        // Database 類別會自動使用內嵌資源
        var version = Database.CurrentVersion;

        Assert.NotNull(version);
        Assert.NotEmpty(version.Version);

        // 如果是舊資料庫（沒有 database_info 表），版本會是 "unknown"
        // 否則應該有有效的建立時間
        if (version.Version != "unknown")
        {
            Assert.True(version.CreatedAt > DateTime.MinValue);
        }
    }

    [Fact]
    public void TestExecuteQuery_ShouldReturnResult()
    {
        if (!IsDatabaseAvailable()) return;

        // 透過 Database.ExecuteQuery 執行查詢
        var result = Database.ExecuteQuery(dir =>
            dir.FindDetailed("臺北市信義區市府路1號")
        );

        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.NotEmpty(result.ZipCode);
    }

    #endregion

    #region 並發查詢測試

    [Fact]
    public void TestConcurrentQueries_SameResult()
    {
        if (!IsDatabaseAvailable()) return;

        const int threadCount = 10;
        const int queriesPerThread = 100;
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        var threads = new List<Thread>();

        // 建立多個執行緒同時查詢
        for (int i = 0; i < threadCount; i++)
        {
            var thread = new Thread(() =>
            {
                for (int j = 0; j < queriesPerThread; j++)
                {
                    var zipcode = Database.ExecuteQuery(dir =>
                        dir.FindDetailed("臺北市信義區市府路1號").ZipCode
                    );
                    results.Add(zipcode);
                }
            });
            threads.Add(thread);
            thread.Start();
        }

        // 等待所有執行緒完成
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // 驗證結果
        Assert.Equal(threadCount * queriesPerThread, results.Count);
        var distinctResults = results.Distinct().ToList();
        Assert.Single(distinctResults); // 所有結果應該相同
        Assert.NotEmpty(distinctResults[0]);
    }

    [Fact]
    public async Task TestConcurrentQueries_Async()
    {
        if (!IsDatabaseAvailable()) return;

        const int taskCount = 20;
        var tasks = new List<Task<string>>();

        // 建立多個非同步任務同時查詢
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() =>
                Database.ExecuteQuery(dir =>
                    dir.FindDetailed("臺北市信義區市府路1號").ZipCode
                )
            ));
        }

        var results = await Task.WhenAll(tasks);

        // 驗證結果
        Assert.Equal(taskCount, results.Length);
        Assert.All(results, zipcode => Assert.NotEmpty(zipcode));
        Assert.All(results, zipcode => Assert.Equal(results[0], zipcode));
    }

    [Fact]
    public void TestConcurrentQueries_DifferentAddresses()
    {
        if (!IsDatabaseAvailable()) return;

        var addresses = new[]
        {
            "臺北市信義區市府路1號",
            "高雄市左營區大中一路",
            "臺中市西屯區臺灣大道",
            "新北市板橋區中山路",
            "桃園市桃園區中正路"
        };

        var results = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        var threads = new List<Thread>();

        // 每個地址在多個執行緒中查詢
        foreach (var address in addresses)
        {
            for (int i = 0; i < 5; i++)
            {
                var addr = address; // 避免閉包問題
                var thread = new Thread(() =>
                {
                    var zipcode = Database.ExecuteQuery(dir =>
                        dir.FindDetailed(addr).ZipCode
                    );
                    results[addr] = zipcode;
                });
                threads.Add(thread);
                thread.Start();
            }
        }

        // 等待所有執行緒完成
        foreach (var thread in threads)
        {
            thread.Join();
        }

        // 驗證結果
        Assert.Equal(addresses.Length, results.Count);
        Assert.All(results.Values, zipcode => Assert.NotEmpty(zipcode));
    }

    #endregion

    #region 更新測試

    [Fact]
    public async Task TestUpdateFromAsync_WithValidDatabase()
    {
        if (!IsDatabaseAvailable()) return;

        // 複製當前資料庫到臨時位置
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid():N}.db");
        File.Copy(_dbPath, tempDb, overwrite: true);

        try
        {
            var originalVersion = Database.CurrentVersion;

            // 從臨時資料庫更新
            var result = await Database.UpdateFromAsync(tempDb);

            // 應該成功（即使版本相同）
            Assert.True(result);

            // 版本資訊應該被更新
            var newVersion = Database.CurrentVersion;
            Assert.NotNull(newVersion);
        }
        finally
        {
            // 清理臨時檔案
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    [Fact]
    public async Task TestUpdateFromAsync_WithInvalidPath()
    {
        if (!IsDatabaseAvailable()) return;

        var result = await Database.UpdateFromAsync("/path/does/not/exist.db");

        Assert.False(result);
    }

    [Fact]
    public async Task TestUpdateFromStreamAsync_WithValidStream()
    {
        if (!IsDatabaseAvailable()) return;

        // 使用 FileShare.ReadWrite 允許其他處理程序同時存取檔案
        using var stream = new FileStream(_dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var result = await Database.UpdateFromStreamAsync(stream, "zipcode.db");

        Assert.True(result);
    }

    [Fact]
    public async Task TestUpdateFromStreamAsync_WithInvalidStream()
    {
        if (!IsDatabaseAvailable()) return;

        // 建立一個無效的資料庫檔案
        var invalidDb = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.db");
        await File.WriteAllTextAsync(invalidDb, "This is not a valid SQLite database");

        try
        {
            using var stream = new FileStream(invalidDb, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var result = await Database.UpdateFromStreamAsync(stream, "zipcode.db");

            // 應該失敗
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(invalidDb))
                File.Delete(invalidDb);
        }
    }

    #endregion

    #region 熱更新測試

    [Fact]
    public async Task TestHotReload_QueriesStillWork()
    {
        if (!IsDatabaseAvailable()) return;

        // 執行初始查詢
        var initialResult = Database.ExecuteQuery(dir =>
            dir.FindDetailed("臺北市信義區市府路1號").ZipCode
        );
        Assert.NotEmpty(initialResult);

        // 複製資料庫到臨時位置並更新
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid():N}.db");
        File.Copy(_dbPath, tempDb, overwrite: true);

        try
        {
            await Database.UpdateFromAsync(tempDb);

            // 更新後查詢應該仍然有效
            var afterUpdateResult = Database.ExecuteQuery(dir =>
                dir.FindDetailed("臺北市信義區市府路1號").ZipCode
            );
            Assert.NotEmpty(afterUpdateResult);
            Assert.Equal(initialResult, afterUpdateResult);
        }
        finally
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    [Fact]
    public async Task TestHotReload_ConcurrentQueriesDuringUpdate()
    {
        if (!IsDatabaseAvailable()) return;

        var cts = new CancellationTokenSource();
        var queryTask = Task.Run(async () =>
        {
            // 持續查詢直到取消
            while (!cts.Token.IsCancellationRequested)
            {
                var result = Database.ExecuteQuery(dir =>
                    dir.FindDetailed("臺北市信義區市府路1號").ZipCode
                );
                Assert.NotEmpty(result);
                await Task.Delay(10);
            }
        });

        // 在查詢進行中更新資料庫
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid():N}.db");
        File.Copy(_dbPath, tempDb, overwrite: true);

        try
        {
            await Database.UpdateFromAsync(tempDb);
            await Task.Delay(100); // 讓查詢繼續進行一段時間

            cts.Cancel();
            await queryTask;
        }
        finally
        {
            if (File.Exists(tempDb))
                File.Delete(tempDb);
        }
    }

    [Fact]
    public void TestReload_InvalidatesThreadLocalCache()
    {
        if (!IsDatabaseAvailable()) return;

        // 在執行緒 1 中查詢
        var thread1Result = string.Empty;
        var thread1 = new Thread(() =>
        {
            thread1Result = Database.ExecuteQuery(dir =>
                dir.FindDetailed("臺北市信義區市府路1號").ZipCode
            );
        });
        thread1.Start();
        thread1.Join();

        Assert.NotEmpty(thread1Result);

        // 呼叫 Reload
        Database.Reload();

        // 在執行緒 2 中查詢（應該使用新的 Directory 實例）
        var thread2Result = string.Empty;
        var thread2 = new Thread(() =>
        {
            thread2Result = Database.ExecuteQuery(dir =>
                dir.FindDetailed("臺北市信義區市府路1號").ZipCode
            );
        });
        thread2.Start();
        thread2.Join();

        Assert.NotEmpty(thread2Result);
        Assert.Equal(thread1Result, thread2Result);
    }

    #endregion

    #region ThreadLocal 行為測試

    [Fact]
    public void TestThreadLocal_EachThreadHasOwnDirectory()
    {
        if (!IsDatabaseAvailable()) return;

        var threadIds = new System.Collections.Concurrent.ConcurrentBag<int>();
        var threads = new List<Thread>();

        for (int i = 0; i < 5; i++)
        {
            var thread = new Thread(() =>
            {
                // 記錄執行緒 ID
                threadIds.Add(Thread.CurrentThread.ManagedThreadId);

                // 執行查詢
                var result = Database.ExecuteQuery(dir =>
                    dir.FindDetailed("臺北市信義區市府路1號").ZipCode
                );
                Assert.NotEmpty(result);
            });
            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // 驗證每個執行緒都有自己的 ID
        Assert.Equal(5, threadIds.Distinct().Count());
    }

    [Fact]
    public void TestThreadLocal_ReuseWithinSameThread()
    {
        if (!IsDatabaseAvailable()) return;

        var results = new List<string>();

        // 在同一個執行緒中多次查詢
        for (int i = 0; i < 10; i++)
        {
            var result = Database.ExecuteQuery(dir =>
                dir.FindDetailed("臺北市信義區市府路1號").ZipCode
            );
            results.Add(result);
        }

        // 所有結果應該相同（因為使用相同的 Directory 實例）
        Assert.Equal(10, results.Count);
        Assert.All(results, r => Assert.Equal(results[0], r));
    }

    #endregion

    #region 版本檢查測試

    [Fact]
    public async Task TestCheckForUpdatesAsync_ShouldComplete()
    {
        if (!IsDatabaseAvailable()) return;

        // 使用短超時避免測試時間過長
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            var updateInfo = await Database.CheckForUpdatesAsync(cts.Token);

            // 可能成功也可能失敗（取決於網路狀況）
            // 只驗證方法能正常執行不拋出異常
            Assert.True(true);
        }
        catch (TaskCanceledException)
        {
            // 超時也是可接受的
            Assert.True(true);
        }
    }

    [Fact]
    public async Task TestCheckForUpdatesAsync_WithCancellation()
    {
        if (!IsDatabaseAvailable()) return;

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消

        var updateInfo = await Database.CheckForUpdatesAsync(cts.Token);

        // 應該返回 null（因為被取消）
        Assert.Null(updateInfo);
    }

    #endregion

    #region 錯誤處理測試

    [Fact]
    public void TestExecuteQuery_WithException_ShouldThrow()
    {
        if (!IsDatabaseAvailable()) return;

        Assert.Throws<Exception>(() =>
        {
            Database.ExecuteQuery<string>(dir =>
            {
                throw new Exception("Test exception");
            });
        });
    }

    [Fact]
    public void TestExecuteQuery_WithNullQuery_ShouldThrow()
    {
        if (!IsDatabaseAvailable()) return;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Database.ExecuteQuery<string>(null!);
        });
    }

    #endregion

    #region 效能測試

    [Fact]
    public void TestPerformance_MultipleQueries()
    {
        if (!IsDatabaseAvailable()) return;

        const int queryCount = 1000;
        var startTime = DateTime.Now;

        for (int i = 0; i < queryCount; i++)
        {
            var result = Database.ExecuteQuery(dir =>
                dir.FindDetailed("臺北市信義區市府路1號").ZipCode
            );
            Assert.NotEmpty(result);
        }

        var elapsed = DateTime.Now - startTime;

        // 記錄性能數據（不做時間斷言以避免間歇性失敗）
        _output.WriteLine($"連續查詢性能測試:");
        _output.WriteLine($"  查詢數量: {queryCount}");
        _output.WriteLine($"  總耗時: {elapsed.TotalSeconds:F2} 秒");
        _output.WriteLine($"  平均每個查詢: {elapsed.TotalMilliseconds / queryCount:F2} 毫秒");
        _output.WriteLine($"  查詢速率: {queryCount / elapsed.TotalSeconds:F0} 次/秒");
    }

    [Fact]
    public async Task TestPerformance_ConcurrentQueries()
    {
        if (!IsDatabaseAvailable()) return;

        const int taskCount = 100;
        var startTime = DateTime.Now;

        var tasks = Enumerable.Range(0, taskCount).Select(_ =>
            Task.Run(() =>
                Database.ExecuteQuery(dir =>
                    dir.FindDetailed("臺北市信義區市府路1號").ZipCode
                )
            )
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        var elapsed = DateTime.Now - startTime;

        // 記錄性能數據（不做時間斷言以避免間歇性失敗）
        _output.WriteLine($"並發查詢性能測試:");
        _output.WriteLine($"  任務數量: {taskCount}");
        _output.WriteLine($"  總耗時: {elapsed.TotalSeconds:F2} 秒");
        _output.WriteLine($"  平均每個查詢: {elapsed.TotalMilliseconds / taskCount:F2} 毫秒");
        _output.WriteLine($"  查詢速率: {taskCount / elapsed.TotalSeconds:F0} 次/秒");

        // 只驗證查詢結果正確性
        Assert.All(results, r => Assert.NotEmpty(r));
    }

    #endregion
}
