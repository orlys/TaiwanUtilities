// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

/// <summary>
/// 資料庫版本資訊
/// </summary>
public record PostalDatabaseVersionInfo
{
    /// <summary>版本號（yyyy-MM-dd 格式）</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>建立時間（ISO 8601 格式）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>來源檔案名稱</summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>記錄數量</summary>
    public int RecordCount { get; set; }

    /// <summary>建置工具版本</summary>
    public string BuilderVersion { get; set; } = string.Empty;

    /// <summary>Git commit SHA</summary>
    public string CommitSha { get; set; } = string.Empty;
}

/// <summary>
/// 資料庫更新資訊
/// </summary>
/// <param name="RemoteVersion">遠端版本資訊</param>
/// <param name="LocalVersion">本地版本資訊</param>
/// <param name="HasUpdate">是否有可用更新</param>
/// <param name="DownloadUrl">下載 URL</param>
public record PostalDatabaseUpdateInfo(
    PostalDatabaseVersionInfo RemoteVersion,
    PostalDatabaseVersionInfo? LocalVersion,
    bool HasUpdate,
    string DownloadUrl
);

/// <summary>
/// 郵遞區號資料庫管理類別（單例模式，執行緒安全）
/// </summary>
/// <remarks>
/// 此類別負責管理郵遞區號資料庫的生命週期，包括：
/// - 自動從內嵌資源載入資料庫
/// - 檢查和下載資料庫更新
/// - 提供執行緒安全的查詢介面
/// - 支援熱更新（無需重啟應用程式）
///
/// 使用 ThreadLocal 儲存每個執行緒的 Directory 實例，實現無鎖並發查詢。
/// </remarks>
public sealed class PostalDatabase : IDisposable
{
    private static string? _externalDatabasePath = null;
    private static readonly Lazy<PostalDatabase> _instance = new(() => new PostalDatabase());
    private static PostalDatabase Instance => _instance.Value;

    private readonly ThreadLocal<DirectoryHolder> _threadLocalDirectory;
    private string _currentDatabasePath;
    private int _databaseVersion = 0;
    private PostalDatabaseVersionInfo? _currentVersion;
    private readonly object _updateLock = new object();

    private const string GitHubRepository = "orlys/TaiwanUtilities";
    private const string ReleaseTag = "database-latest";

    /// <summary>
    /// 下載大小上限（100 MB），防止資源耗盡攻擊
    /// </summary>
    private const long MaxDownloadSize = 100 * 1024 * 1024;

    /// <summary>
    /// 內部類別：持有 ZipCodeRepository 實例和版本號
    /// </summary>
    private class DirectoryHolder
    {
        public ZipCodeRepository Directory { get; }
        public int Version { get; }

        public DirectoryHolder(ZipCodeRepository directory, int version)
        {
            Directory = directory;
            Version = version;
        }
    }

    /// <summary>
    /// 私有建構子（單例模式）
    /// </summary>
    private PostalDatabase()
    {
        // 初始化資料庫路徑（優先使用外部路徑，其次是內嵌資源）
        if (!string.IsNullOrEmpty(_externalDatabasePath) && File.Exists(_externalDatabasePath))
        {
            _currentDatabasePath = _externalDatabasePath!;
        }
        else if (EmbeddedDatabaseHelper.HasEmbeddedDatabase())
        {
            _currentDatabasePath = EmbeddedDatabaseHelper.ExtractEmbeddedDatabase();
        }
        else
        {
            throw new FileNotFoundException(
                "找不到內嵌的郵遞區號資料庫。請先建立索引或更新資料庫。");
        }

        // 載入版本資訊
        _currentVersion = LoadVersionInfo(_currentDatabasePath);

        // 初始化 ThreadLocal（每個執行緒一個 Directory 實例）
        _threadLocalDirectory = new ThreadLocal<DirectoryHolder>(() =>
        {
            var currentVersion = _databaseVersion;
            var directory = new ZipCodeRepository(_currentDatabasePath, keepAlive: true);
            return new DirectoryHolder(directory, currentVersion);
        });

        // 背景預熱規則引擎（不阻塞啟動）
        Task.Run(() =>
        {
            try
            {
                PostalRulesEngine.Warmup();
            }
            catch (Exception ex)
            {
                // 記錄錯誤但不影響啟動
                System.Diagnostics.Debug.WriteLine($"Rules warmup failed: {ex}");
            }
        });
    }

    /// <summary>
    /// 取得當前資料庫版本資訊
    /// </summary>
    public static PostalDatabaseVersionInfo? CurrentVersion => Instance._currentVersion;

    /// <summary>
    /// 使用外部資料庫路徑（用於測試）
    /// </summary>
    /// <param name="dbPath">外部資料庫檔案路徑</param>
    /// <remarks>
    /// 此方法必須在首次使用 PostalDatabase 單例之前呼叫。
    /// 如果單例已經初始化，此方法將無效果。
    /// 僅供測試專案使用（透過 InternalsVisibleTo 暴露）。
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void UseExternalDatabase(string dbPath)
    {
        if (_instance.IsValueCreated)
        {
            throw new InvalidOperationException(
                "PostalDatabase 單例已經初始化，無法變更資料庫路徑。請在使用任何查詢方法之前呼叫此方法。");
        }

        _externalDatabasePath = dbPath;
    }

    /// <summary>
    /// 檢查是否有可用的資料庫更新
    /// </summary>
    /// <param name="ct">取消權杖</param>
    /// <returns>更新資訊，如果無法連線或檢查失敗則返回 null</returns>
    public static Task<PostalDatabaseUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
        => Instance.CheckForUpdatesInternalAsync(ct);

    /// <summary>
    /// 從 GitHub Release 更新資料庫
    /// </summary>
    /// <param name="ct">取消權杖</param>
    /// <returns>是否成功更新</returns>
    public static Task<bool> UpdateAsync(CancellationToken ct = default)
        => Instance.UpdateInternalAsync(ct);

    /// <summary>
    /// 從本地檔案更新資料庫
    /// </summary>
    /// <param name="dbPath">資料庫檔案路徑</param>
    /// <param name="ct">取消權杖</param>
    /// <returns>是否成功更新</returns>
    public static Task<bool> UpdateFromAsync(string dbPath, CancellationToken ct = default)
        => Instance.UpdateFromInternalAsync(dbPath, ct);

    /// <summary>
    /// 從串流更新資料庫
    /// </summary>
    /// <param name="stream">包含資料庫內容的串流</param>
    /// <param name="fileName">檔案名稱（用於驗證）</param>
    /// <param name="ct">取消權杖</param>
    /// <returns>是否成功更新</returns>
    public static Task<bool> UpdateFromStreamAsync(Stream stream, string fileName, CancellationToken ct = default)
        => Instance.UpdateFromStreamInternalAsync(stream, fileName, ct);

    /// <summary>
    /// 強制重新載入資料庫（清除所有執行緒的快取）
    /// </summary>
    public static void Reload()
        => Instance.ReloadInternal();

    /// <summary>
    /// 內部方法：執行查詢（供 ZipCode 使用）
    /// </summary>
    internal static T ExecuteQuery<T>(Func<ZipCodeRepository, T> query)
        => Instance.ExecuteQueryInternal(query);

    /// <summary>
    /// 查詢結構化投遞規則
    /// </summary>
    /// <param name="city">縣市</param>
    /// <param name="area">行政區</param>
    /// <param name="road">路街名</param>
    /// <returns>匹配的投遞規則清單</returns>
    internal static List<PostalRule> QueryPostalRules(string city, string area, string road)
        => Instance.ExecuteQueryInternal(repo => repo.QueryPostalRules(city, area, road));

    /// <summary>
    /// 載入所有特殊路名（不以 路/街/道 結尾）
    /// </summary>
    /// <returns>特殊路名集合</returns>
    internal static HashSet<string> LoadSpecialRoadNames()
        => Instance.ExecuteQueryInternal(repo => repo.LoadSpecialRoadNames());

    /// <summary>
    /// 內部實作：檢查更新
    /// </summary>
    private async Task<PostalDatabaseUpdateInfo?> CheckForUpdatesInternalAsync(CancellationToken ct)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TaiwanUtilities-PostalDatabase-Updater/1.0");

            var releaseUrl = $"https://api.github.com/repos/{GitHubRepository}/releases/tags/{ReleaseTag}";
            var response = await httpClient.GetAsync(releaseUrl, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // 簡單的 JSON 解析（避免引入額外依賴）
            var publishedAt = ExtractJsonValue(content, "published_at");
            var downloadUrl = ExtractJsonValue(content, "browser_download_url");

            if (string.IsNullOrEmpty(publishedAt) || string.IsNullOrEmpty(downloadUrl))
                return null;

            var remoteVersion = new PostalDatabaseVersionInfo
            {
                Version = publishedAt.Substring(0, 10), // yyyy-MM-dd
                CreatedAt = DateTime.Parse(publishedAt)
            };

            var updateInfo = new PostalDatabaseUpdateInfo(
                RemoteVersion: remoteVersion,
                LocalVersion: _currentVersion,
                HasUpdate: _currentVersion == null ||
                           DateTime.Parse(remoteVersion.Version) > DateTime.Parse(_currentVersion.Version),
                DownloadUrl: downloadUrl
            );

            return updateInfo;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"CheckForUpdates failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 內部實作：從 GitHub 更新
    /// </summary>
    private async Task<bool> UpdateInternalAsync(CancellationToken ct)
    {
        var updateInfo = await CheckForUpdatesInternalAsync(ct).ConfigureAwait(false);
        if (updateInfo == null || !updateInfo.HasUpdate)
            return false;

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TaiwanUtilities-PostalDatabase-Updater/1.0");

            var response = await httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // 檢查 Content-Length 是否超過上限
            if (response.Content.Headers.ContentLength > MaxDownloadSize)
                return false;

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return await UpdateFromStreamInternalAsync(stream, "zipcode.db", ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateInternal failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 內部實作：從本地檔案更新
    /// </summary>
    private async Task<bool> UpdateFromInternalAsync(string dbPath, CancellationToken ct)
    {
        if (!File.Exists(dbPath))
            return false;

        try
        {
            using var stream = File.OpenRead(dbPath);
            return await UpdateFromStreamInternalAsync(stream, Path.GetFileName(dbPath), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateFromInternal failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 內部實作：從串流更新
    /// </summary>
    private async Task<bool> UpdateFromStreamInternalAsync(Stream stream, string fileName, CancellationToken ct)
    {
        string? newDbPath = null;
        try
        {
            // === Phase 1: I/O 操作在 lock 外執行，不阻塞查詢執行緒 ===

            // 使用時間戳記確保檔名唯一，避免累積垃圾檔案
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var counter = Volatile.Read(ref _databaseVersion) + 1;
            newDbPath = Path.Combine(Path.GetTempPath(), "TaiwanUtilities", $"zipcode_{timestamp}_{counter}.db");
            var tempDir = Path.GetDirectoryName(newDbPath);

            if (!System.IO.Directory.Exists(tempDir))
                System.IO.Directory.CreateDirectory(tempDir!);

            // 複製串流到新檔案（限制最大大小）
            using (var fileStream = File.Create(newDbPath))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    totalBytes += bytesRead;
                    if (totalBytes > MaxDownloadSize)
                    {
                        fileStream.Dispose();
                        try { File.Delete(newDbPath); } catch { }
                        return false;
                    }
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                }
            }

            // 驗證資料庫（嘗試開啟並讀取版本資訊）
            var newVersion = LoadVersionInfo(newDbPath);
            if (newVersion == null)
            {
                try { File.Delete(newDbPath); } catch { }
                return false;
            }

            // === Phase 2: 僅 pointer swap 需要 lock，極短暫 ===
            string? oldDbPath;
            lock (_updateLock)
            {
                oldDbPath = _currentDatabasePath;
                _currentDatabasePath = newDbPath;
                _currentVersion = newVersion;

                // 遞增版本號（觸發 ThreadLocal 重新載入）
                Interlocked.Increment(ref _databaseVersion);
            }

            // 標記成功，避免 finally 清理新檔案
            newDbPath = null;

            // 清理舊的臨時檔案（延遲刪除，避免其他執行緒仍在使用）
            if (oldDbPath.Contains(Path.GetTempPath()))
            {
                _ = Task.Delay(5000, CancellationToken.None).ContinueWith(_ =>
                {
                    try { File.Delete(oldDbPath); } catch { }
                }, TaskScheduler.Default);
            }

            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateFromStream failed: {ex.Message}");

            // 清理失敗時產生的暫存檔
            if (newDbPath != null)
            {
                try { File.Delete(newDbPath); } catch { }
            }

            return false;
        }
    }

    /// <summary>
    /// 內部實作：重新載入
    /// </summary>
    private void ReloadInternal()
    {
        lock (_updateLock)
        {
            Interlocked.Increment(ref _databaseVersion);
        }
    }

    /// <summary>
    /// 內部實作：執行查詢
    /// </summary>
    private T ExecuteQueryInternal<T>(Func<ZipCodeRepository, T> query)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var holder = _threadLocalDirectory.Value!;
        var currentVersion = _databaseVersion;

        // 如果版本號已變更，重新建立 Directory
        if (holder.Version != currentVersion)
        {
            holder.Directory.Dispose();
            var newDirectory = new ZipCodeRepository(_currentDatabasePath, keepAlive: true);
            var newHolder = new DirectoryHolder(newDirectory, currentVersion);
            _threadLocalDirectory.Value = newHolder;
            holder = newHolder;
        }

        return query(holder.Directory);
    }

    /// <summary>
    /// 從資料庫載入版本資訊
    /// </summary>
    private static PostalDatabaseVersionInfo? LoadVersionInfo(string dbPath)
    {
        SqliteConnection? connection = null;
        try
        {
            // 使用明確的連接參數以改善跨平台兼容性
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly,  // 只讀模式減少鎖定問題
                Cache = SqliteCacheMode.Shared   // 共享快取模式
            }.ToString();

            connection = new SqliteConnection(connectionString);
            connection.Open();

            // 檢查 database_info 表是否存在
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='database_info'";
                var tableExists = checkCmd.ExecuteScalar();
                if (tableExists == null)
                {
                    // 表不存在，返回預設版本資訊
                    return new PostalDatabaseVersionInfo
                    {
                        Version = "unknown",
                        CreatedAt = DateTime.MinValue,
                        SourceFile = "unknown",
                        RecordCount = 0,
                        BuilderVersion = "unknown"
                    };
                }
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT key, value FROM database_info";

            var info = new PostalDatabaseVersionInfo();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var key = reader.GetString(0);
                var value = reader.GetString(1);

                switch (key)
                {
                    case "version":
                        info.Version = value;
                        break;
                    case "created_at":
                        if (DateTime.TryParse(value, out var createdAt))
                            info.CreatedAt = createdAt;
                        break;
                    case "source_file":
                        info.SourceFile = value;
                        break;
                    case "record_count":
                        if (int.TryParse(value, out var count))
                            info.RecordCount = count;
                        break;
                    case "builder_version":
                        info.BuilderVersion = value;
                        break;
                    case "commit_sha":
                        info.CommitSha = value;
                        break;
                }
            }

            return string.IsNullOrEmpty(info.Version) ? null : info;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"LoadVersionInfo failed: {ex.Message}");
            return null;
        }
        finally
        {
            // 明確關閉和釋放連接，改善跨平台兼容性
            if (connection != null)
            {
                try
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    connection.Dispose();

                    // 在某些平台上，明確觸發 GC 有助於釋放檔案鎖定
                    SqliteConnection.ClearAllPools();
                }
                catch
                {
                    // 忽略清理錯誤
                }
            }
        }
    }

    /// <summary>
    /// 簡單的 JSON 值提取（避免引入額外依賴）
    /// </summary>
    private static string ExtractJsonValue(string json, string key)
    {
        var searchKey = $"\"{key}\":\"";
        var startIndex = json.IndexOf(searchKey);
        if (startIndex == -1)
            return string.Empty;

        startIndex += searchKey.Length;
        var endIndex = json.IndexOf("\"", startIndex);
        if (endIndex == -1)
            return string.Empty;

        return json.Substring(startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// 清理舊的臨時資料庫檔案
    /// </summary>
    private static void CleanupOldTempDatabases()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "TaiwanUtilities");
            if (!System.IO.Directory.Exists(tempDir))
                return;

            // 尋找所有 zipcode_*.db 檔案
            var dbFiles = System.IO.Directory.GetFiles(tempDir, "zipcode_*.db");

            foreach (var file in dbFiles)
            {
                try
                {
                    // 檢查檔案是否正在使用（嘗試開啟獨佔模式）
                    using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        // 如果能開啟，表示沒有其他程序使用，可以刪除
                    }

                    File.Delete(file);
                }
                catch
                {
                    // 檔案正在使用中或無法刪除，跳過
                }
            }
        }
        catch
        {
            // 清理失敗不影響程式運作
        }
    }

    /// <summary>
    /// 明確介面實作：釋放資源
    /// </summary>
    void IDisposable.Dispose()
    {
        _threadLocalDirectory?.Dispose();
    }
}
