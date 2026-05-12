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
            !addr.Number.HasValue)
        {
            return null;
        }

        var road = addr.Road ?? string.Empty;
        if (!string.IsNullOrEmpty(addr.Section))
            road = road + ToChineseSection(addr.Section);

        bool found = PostalData.Rules.TryGetValue(
            string.Concat(addr.City, "|", addr.District, "|", road), out var ruleSet);
        if (!found && !string.IsNullOrEmpty(road))
        {
            // Road may contain Arabic digits where DBF stores Chinese ordinals (e.g., "四維3路" → "四維三路")
            var chineseRoad = ArabicToChineseInRoad(road);
            if (!ReferenceEquals(chineseRoad, road))
                found = PostalData.Rules.TryGetValue(
                    string.Concat(addr.City, "|", addr.District, "|", chineseRoad),
                    out ruleSet);
        }

        // Fallback: when Road is unparsed, try Locality or Village+Locality
        // e.g., "花蓮縣秀林鄉富世村富世291號" → Locality="富世" → key "花蓮縣|秀林鄉|富世"
        // e.g., "彰化縣永靖鄉一村巷1號" → Village="一村" + Locality="巷" → key "彰化縣|永靖鄉|一村巷"
        if (!found && string.IsNullOrEmpty(addr.Road))
        {
            if (!string.IsNullOrEmpty(addr.Locality))
                found = PostalData.Rules.TryGetValue(
                    string.Concat(addr.City, "|", addr.District, "|", addr.Locality), out ruleSet);
            if (!found && !string.IsNullOrEmpty(addr.Village) && !string.IsNullOrEmpty(addr.Locality))
                found = PostalData.Rules.TryGetValue(
                    string.Concat(addr.City, "|", addr.District, "|", addr.Village + addr.Locality), out ruleSet);
        }

        if (!found) return null;

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

    internal static bool CityExists(string city)
    {
        var prefix = city + "|";
        foreach (var k in PostalData.Rules.Keys)
            if (k.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static bool DistrictExists(string city, string district)
    {
        var prefix = city + "|" + district + "|";
        foreach (var k in PostalData.Rules.Keys)
            if (k.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// 檢查路街是否在資料庫中存在（不需門牌）
    /// </summary>
    internal static bool RoadExists(string city, string district, string road, string? section)
    {
        var r = road;
        if (!string.IsNullOrEmpty(section))
            r = r + ToChineseSection(section);

        var key = string.Concat(city, "|", district, "|", r);
        if (PostalData.Rules.ContainsKey(key)) return true;

        var chineseR = ArabicToChineseInRoad(r);
        return !ReferenceEquals(chineseR, r) &&
               PostalData.Rules.ContainsKey(string.Concat(city, "|", district, "|", chineseR));
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

    internal static string ArabicToChineseInRoad(string road)
    {
        // Convert single Arabic digits before 路/街/巷/弄 to Chinese ordinals
        // "四維3路" → "四維三路", "龍岡路3段" already handled by ToChineseSection
        for (int i = 1; i < road.Length; i++)
        {
            char unit = road[i];
            if ((unit == '路' || unit == '街' || unit == '巷' || unit == '弄') && char.IsDigit(road[i - 1]))
            {
                var sb = new System.Text.StringBuilder(road.Length);
                sb.Append(road, 0, i - 1);
                sb.Append("○一二三四五六七八九"[road[i - 1] - '0']);
                sb.Append(road, i, road.Length - i);
                return sb.ToString();
            }
        }
        return road;
    }

    internal static string ToChineseSection(string section)
    {
        // "1段" → "一段", "2段" → "二段", ...  (DBF keys use Chinese ordinals)
        if (section.Length >= 2 && section[section.Length - 1] == '段'
            && int.TryParse(section.Substring(0, section.Length - 1), out var n))
        {
            return n switch
            {
                1 => "一段", 2 => "二段", 3 => "三段", 4 => "四段", 5 => "五段",
                6 => "六段", 7 => "七段", 8 => "八段", 9 => "九段", 10 => "十段",
                _ => section
            };
        }
        return section;
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
