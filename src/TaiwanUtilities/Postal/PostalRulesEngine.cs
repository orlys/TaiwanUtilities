// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;

using TaiwanUtilities.Internals;

/// <summary>
/// 郵遞規則引擎（靜態資料，已編譯進 binary）
/// </summary>
/// <remarks>
/// <para>
/// 此引擎使用編譯時生成的靜態資料（PostalData.g.cs），不需要 SQLite。
/// 規則以 Struct-of-Arrays (SoA) 格式儲存，支援 SIMD 比對。
/// </para>
/// </remarks>
public static class PostalRulesEngine
{
    /// <summary>
    /// 預熱引擎（no-op：資料已編譯進 binary）
    /// </summary>
    public static void Warmup() { }

    /// <summary>
    /// 重新載入規則（no-op：資料已編譯進 binary）
    /// </summary>
    public static void Reload() { }

    /// <summary>
    /// 引擎是否已初始化（靜態資料永遠 true）
    /// </summary>
    public static bool IsInitialized => true;

    /// <summary>
    /// 查詢郵遞區號
    /// </summary>
    /// <param name="addr">結構化地址</param>
    /// <returns>郵遞區號查詢結果，若無匹配規則則返回 null</returns>
    public static ZipCodeResult? Find(PostalAddress addr)
    {
        if (string.IsNullOrEmpty(addr.City) ||
            string.IsNullOrEmpty(addr.District) ||
            string.IsNullOrEmpty(addr.Road) ||
            !addr.Number.HasValue)
        {
            return null;
        }

        var key = string.Concat(addr.City, "|", addr.District, "|", addr.Road);

        if (!PostalData.Rules.TryGetValue(key, out var ruleSet))
        {
            return null;
        }

        int lane      = ParseNumericPrefix(addr.Lane);
        int alley     = ParseNumericPrefix(addr.Alley);
        int subNumber = (addr.SubNumbers != null && addr.SubNumbers.Count > 0)
                        ? addr.SubNumbers[0] : 0;

        if (!ruleSet.TryMatch(addr.Number.Value, subNumber, lane, alley,
            out int zipIdx, out int deptIdx, out int officeIdx, out int scopeIdx))
        {
            return null;
        }

        var zipCode = PostalData.ZipCodePool[zipIdx];
        var scope   = scopeIdx > 0 ? PostalData.Scopes[scopeIdx] : null;
        var ruleDescription = BuildRuleDescription(addr, scope);

        var result = ZipCodeResult.ExactMatch(addr.NormalizedAddress, zipCode, ruleDescription);
        result.Department = deptIdx > 0 ? PostalData.Departments[deptIdx] : null;
        result.Office     = officeIdx > 0 ? PostalData.Offices[officeIdx] : null;
        return result;
    }

    /// <summary>
    /// 從地址欄位字串中解析數字前綴（如 "243巷" → 243, "53弄" → 53）
    /// </summary>
    internal static int ParseNumericPrefix(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return 0;
        }

        int end = 0;
        while (end < field!.Length && field[end] >= '0' && field[end] <= '9')
        {
            end++;
        }

        if (end == 0)
        {
            return 0;
        }

#if NET8_0_OR_GREATER
        if (int.TryParse(field.AsSpan(0, end), out var value))
        {
            return value;
        }
#else
        if (int.TryParse(field[..end], out var value))
        {
            return value;
        }
#endif

        return 0;
    }

    private static string BuildRuleDescription(PostalAddress addr, string? scope)
    {
        var parts = new List<string>
        {
            addr.City ?? string.Empty,
            addr.District ?? string.Empty,
            addr.Road ?? string.Empty
        };

        if (!string.IsNullOrEmpty(scope))
        {
            parts.Add(scope!);
        }

        return string.Concat(parts);
    }

    /// <summary>
    /// 取得引擎統計資訊（除錯用）
    /// </summary>
    public static (int TotalKeys, int TotalRules, long MemoryBytes) GetStats()
    {
        int keys = PostalData.Rules.Count;
        int rules = 0;
        foreach (var rs in PostalData.Rules.Values)
        {
            rules += rs.Count;
        }
        long mem = rules * 60L + keys * 50L;
        return (keys, rules, mem);
    }
}
