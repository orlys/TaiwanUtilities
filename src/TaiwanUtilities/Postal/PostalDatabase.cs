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
using System.Threading;
using System.Threading.Tasks;

using TaiwanUtilities.Internals;

/// <summary>
/// 資料庫版本資訊
/// </summary>
public record PostalDatabaseVersionInfo
{
    /// <summary>版本號（yyyy-MM-dd 格式）</summary>
    public string Version { get; internal set; } = string.Empty;

    /// <summary>建立時間（ISO 8601 格式）</summary>
    public DateTime CreatedAt { get; internal set; }

    /// <summary>來源檔案名稱</summary>
    public string SourceFile { get; internal set; } = string.Empty;

    /// <summary>記錄數量</summary>
    public int RecordCount { get; internal set; }

    /// <summary>建置工具版本</summary>
    public string BuilderVersion { get; internal set; } = string.Empty;

    /// <summary>Git commit SHA</summary>
    public string CommitSha { get; internal set; } = string.Empty;
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
/// 郵遞區號資料庫管理類別（靜態資料版本，資料已編譯進 binary）
/// </summary>
public sealed class PostalDatabase : IDisposable
{
    private static readonly PostalDatabaseVersionInfo s_version = new()
    {
        Version = PostalData.GeneratedDate,
        RecordCount = PostalData.RecordCount,
        BuilderVersion = "2.0.0"
    };

    /// <summary>
    /// 取得當前資料版本資訊
    /// </summary>
    public static PostalDatabaseVersionInfo? CurrentVersion => s_version;

    /// <summary>
    /// 供測試使用的 no-op（資料已編譯進 binary，無需外部資料庫）
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void UseExternalDatabase(string dbPath)
    {
        // no-op: data is compiled into the binary
    }

    /// <summary>
    /// 檢查是否有可用的資料庫更新（永遠返回 null：資料已編譯進 binary）
    /// </summary>
    public static Task<PostalDatabaseUpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
        => Task.FromResult<PostalDatabaseUpdateInfo?>(null);

    /// <summary>
    /// 從 GitHub Release 更新資料庫（no-op：資料已編譯進 binary）
    /// </summary>
    public static Task<bool> UpdateAsync(CancellationToken ct = default)
        => Task.FromResult(false);

    /// <summary>
    /// 從本地檔案更新資料庫（no-op：資料已編譯進 binary）
    /// </summary>
    public static Task<bool> UpdateFromAsync(string dbPath, CancellationToken ct = default)
        => Task.FromResult(false);

    /// <summary>
    /// 從串流更新資料庫（no-op：資料已編譯進 binary）
    /// </summary>
    public static Task<bool> UpdateFromStreamAsync(Stream stream, string fileName, CancellationToken ct = default)
        => Task.FromResult(false);

    /// <summary>
    /// 重新載入資料庫（no-op：資料已編譯進 binary）
    /// </summary>
    public static void Reload() { }

    /// <summary>
    /// 查詢結構化投遞規則
    /// </summary>
    internal static List<PostalRule> QueryPostalRules(string city, string area, string road)
    {
        if (!PostalLookup.TryFind(city, area, road, out var ruleSet))
        {
            return new List<PostalRule>();
        }

        return ConvertToPostalRules(ruleSet);
    }

    /// <summary>
    /// 載入所有特殊路名（不以 路/街/道 結尾）
    /// </summary>
    internal static HashSet<string> LoadSpecialRoadNames()
        => PostalData.SpecialRoadNames;

    private static List<PostalRule> ConvertToPostalRules(PostalRuleSet ruleSet)
    {
        var result = new List<PostalRule>(ruleSet.Count);
        for (int i = 0; i < ruleSet.Count; i++)
        {
            result.Add(new PostalRule
            {
                ZipCode    = PostalData.ZipCodePool[ruleSet.ZipCodeIndex(i)],
                LaneStart  = ruleSet.HasLane(i)  ? ruleSet.LaneStart(i)  : (int?)null,
                LaneEnd    = ruleSet.HasLane(i)  ? ruleSet.LaneEnd(i)    : (int?)null,
                AlleyStart = ruleSet.HasAlley(i) ? ruleSet.AlleyStart(i) : (int?)null,
                AlleyEnd   = ruleSet.HasAlley(i) ? ruleSet.AlleyEnd(i)   : (int?)null,
                NumberStart    = ruleSet.NumberStart(i) > 0            ? ruleSet.NumberStart(i) : (int?)null,
                NumberEnd      = ruleSet.NumberEnd(i) < int.MaxValue   ? ruleSet.NumberEnd(i)   : (int?)null,
                NumberStartSub = ruleSet.SubStart(i) > 0               ? ruleSet.SubStart(i)    : (int?)null,
                NumberEndSub   = ruleSet.SubEnd(i) < int.MaxValue      ? ruleSet.SubEnd(i)      : (int?)null,
                EvenOdd    = ruleSet.EvenOdd(i) != 0 ? (int?)ruleSet.EvenOdd(i) : null,
                Scope      = ruleSet.ScopeIndex(i)  > 0 ? PostalData.Scopes[ruleSet.ScopeIndex(i)]        : null,
                Department = ruleSet.DeptIndex(i)   > 0 ? PostalData.Departments[ruleSet.DeptIndex(i)]    : null,
                Office     = ruleSet.OfficeIndex(i) > 0 ? PostalData.Offices[ruleSet.OfficeIndex(i)]      : null,
            });
        }

        return result;
    }

    void IDisposable.Dispose() { }
}
