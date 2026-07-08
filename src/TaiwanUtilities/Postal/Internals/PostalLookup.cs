// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities.Internals;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 郵遞規則查詢入口：對 PostalData 的階層資料做三層二分搜尋
/// （縣市 → 行政區 → 路名）。熱路徑零配置：路名比對直接掃
/// RoadBlob 字元，不建立字串、不串接鍵、不算 hash。
/// </summary>
internal static class PostalLookup
{
    internal static int GroupCount => PostalData.RoadOffsets.Length - 1;

    /// <summary>取得第 group 組規則的視圖。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static PostalRuleSet GetRuleSet(int group)
    {
        int start = PostalData.GroupRuleOffsets[group];
        return new PostalRuleSet(start, PostalData.GroupRuleOffsets[group + 1] - start);
    }

    /// <summary>取得第 group 組的路名（配置字串；僅供列舉／除錯，熱路徑不呼叫）。</summary>
    internal static string GetRoad(int group)
    {
        int start = PostalData.RoadOffsets[group];
        return PostalData.RoadBlob.Substring(start, PostalData.RoadOffsets[group + 1] - start);
    }

    /// <summary>取得第 group 組的官方英文路名（配置字串；英文地址格式化用）。</summary>
    internal static string GetEnglishRoad(int group)
    {
        int start = PostalData.EnglishRoadOffsets[group];
        return PostalData.EnglishRoadBlob.Substring(start, PostalData.EnglishRoadOffsets[group + 1] - start);
    }

    internal static bool TryFind(string? city, string? district, string? road, out PostalRuleSet ruleSet)
    {
        int g = FindGroup(city, district, road);
        if (g < 0)
        {
            ruleSet = default;
            return false;
        }
        ruleSet = GetRuleSet(g);
        return true;
    }

    /// <summary>回傳群組索引，-1 = 找不到。</summary>
    internal static int FindGroup(string? city, string? district, string? road)
    {
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district) || road == null)
        {
            return -1;
        }

        int c = FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city!);
        if (c < 0) return -1;

        int d = FindName(PostalData.DistrictNames,
            PostalData.CityDistrictOffsets[c], PostalData.CityDistrictOffsets[c + 1], district!);
        if (d < 0) return -1;

        return FindRoad(PostalData.DistrictGroupOffsets[d], PostalData.DistrictGroupOffsets[d + 1], road);
    }

    /// <summary>回傳縣市、行政區、群組索引；false = 找不到。</summary>
    internal static bool TryFindIndexed(
        string? city,
        string? district,
        string? road,
        out int cityIdx,
        out int districtIdx,
        out int groupIdx)
    {
        cityIdx = -1;
        districtIdx = -1;
        groupIdx = -1;

        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district) || road == null)
        {
            return false;
        }

        cityIdx = FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city!);
        if (cityIdx < 0) return false;

        districtIdx = FindName(PostalData.DistrictNames,
            PostalData.CityDistrictOffsets[cityIdx], PostalData.CityDistrictOffsets[cityIdx + 1], district!);
        if (districtIdx < 0) return false;

        groupIdx = FindRoad(PostalData.DistrictGroupOffsets[districtIdx], PostalData.DistrictGroupOffsets[districtIdx + 1], road);
        return groupIdx >= 0;
    }

    /// <summary>
    /// 在指定縣市底下，找出從 text[index] 起與已知區名最長匹配的長度（字元數，0 = 無匹配）。
    /// 斷詞時以資料庫的區清單為準切出行政區，避免「前鎮區」「新市區」這類內部含
    /// 市/鎮/鄉 的區名被 regex 在內部單位字處誤切。零配置比對。
    /// </summary>
    internal static int MatchLongestDistrict(string? city, string text, int index)
    {
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(text) || index >= text.Length)
        {
            return 0;
        }

        int c = FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city!);
        if (c < 0) return 0;

        var names = PostalData.DistrictNames;
        int lo = PostalData.CityDistrictOffsets[c];
        int hi = PostalData.CityDistrictOffsets[c + 1];
        int avail = text.Length - index;
        int best = 0;

        for (int d = lo; d < hi; d++)
        {
            int len = names[d].Length;
            if (len > best && len <= avail &&
                string.CompareOrdinal(text, index, names[d], 0, len) == 0)
            {
                best = len;
            }
        }
        return best;
    }

    internal static bool CityExists(string city)
        => FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city) >= 0;

    internal static bool DistrictExists(string city, string district)
    {
        int c = FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city);
        return c >= 0 && FindName(PostalData.DistrictNames,
            PostalData.CityDistrictOffsets[c], PostalData.CityDistrictOffsets[c + 1], district) >= 0;
    }

    /// <summary>
    /// 依序列舉所有群組（縣市、行政區、路名、群組索引）。
    /// 路名字串於列舉時才物化。
    /// </summary>
    internal static IEnumerable<(string City, string District, string Road, int GroupIndex)> EnumerateGroups()
    {
        var cityDistrictOffsets  = PostalData.CityDistrictOffsets;
        var districtGroupOffsets = PostalData.DistrictGroupOffsets;

        for (int c = 0; c < PostalData.CityNames.Length; c++)
        {
            var city = PostalData.CityNames[c];
            for (int d = cityDistrictOffsets[c]; d < cityDistrictOffsets[c + 1]; d++)
            {
                var district = PostalData.DistrictNames[d];
                for (int g = districtGroupOffsets[d]; g < districtGroupOffsets[d + 1]; g++)
                {
                    yield return (city, district, GetRoad(g), g);
                }
            }
        }
    }

    /// <summary>
    /// 查詢結構化投遞規則
    /// </summary>
    internal static List<PostalRule> QueryPostalRules(string city, string area, string road)
    {
        if (!TryFind(city, area, road, out var ruleSet))
        {
            return new List<PostalRule>();
        }

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

    // ── 二分搜尋（ordinal 序，與 Builder 的排序一致）──────────────────

    private static int FindName(string[] names, int lo, int hi, string key)
    {
        hi--;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = string.CompareOrdinal(key, names[mid]);
            if (cmp == 0) return mid;
            if (cmp < 0) hi = mid - 1; else lo = mid + 1;
        }
        return -1;
    }

    private static int FindRoad(int lo, int hi, string road)
    {
        hi--;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            int cmp = CompareToBlobRoad(road, mid);
            if (cmp == 0) return mid;
            if (cmp < 0) hi = mid - 1; else lo = mid + 1;
        }
        return -1;
    }

    /// <summary>ordinal 比對 road 與 RoadBlob 中第 group 組的路名，零配置。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CompareToBlobRoad(string road, int group)
    {
        var blob  = PostalData.RoadBlob;
        int start = PostalData.RoadOffsets[group];
        int len   = PostalData.RoadOffsets[group + 1] - start;

        int n = road.Length < len ? road.Length : len;
        for (int i = 0; i < n; i++)
        {
            int diff = road[i] - blob[start + i];
            if (diff != 0) return diff;
        }
        return road.Length - len;
    }
}
