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
                RegionMatchesTaiEquivalent(text, index, names[d], len))
            {
                best = len;
            }
        }
        return best;
    }

    /// <summary>
    /// 在指定縣市行政區底下，找出從 text[index] 起與已知路名最長匹配的長度
    /// （消耗的輸入字元數，0 = 無匹配）。路名以資料庫 RoadBlob 為準，
    /// 避免 regex 在「鐵路街」「忠孝東路一段」這類內部含路/街/道/段
    /// 的名稱中途切斷。比對時接受 Normalize 後的阿拉伯數字序號。
    /// </summary>
    internal static int MatchLongestRoad(string? city, string? district, string text, int index)
        => MatchLongestRoadCore(city, district, text, index, out _);

    internal static string? GetLongestRoadName(string? city, string? district, string text, int index)
    {
        var consumed = MatchLongestRoadCore(city, district, text, index, out var group);
        return consumed > 0 && group >= 0 ? GetRoad(group) : null;
    }

    private static int MatchLongestRoadCore(string? city, string? district, string text, int index, out int bestGroup)
    {
        bestGroup = -1;
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district) ||
            string.IsNullOrEmpty(text) || index >= text.Length)
        {
            return 0;
        }

        int c = FindName(PostalData.CityNames, 0, PostalData.CityNames.Length, city!);
        if (c < 0) return 0;

        int d = FindName(PostalData.DistrictNames,
            PostalData.CityDistrictOffsets[c], PostalData.CityDistrictOffsets[c + 1], district!);
        if (d < 0) return 0;

        int lo = PostalData.DistrictGroupOffsets[d];
        int hi = PostalData.DistrictGroupOffsets[d + 1];
        int best = 0;

        for (int g = lo; g < hi; g++)
        {
            if (TryMatchBlobRoadPrefix(text, index, g, out int consumed) && consumed > best)
            {
                best = consumed;
                bestGroup = g;
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

    /// <summary>比對 RoadBlob 中第 group 組是否為 text[index] 的前綴，零配置。</summary>
    private static bool TryMatchBlobRoadPrefix(string text, int index, int group, out int consumed)
    {
        var blob = PostalData.RoadBlob;
        int start = PostalData.RoadOffsets[group];
        int end = PostalData.RoadOffsets[group + 1];
        int textIndex = index;

        for (int blobIndex = start; blobIndex < end;)
        {
            if (textIndex >= text.Length)
            {
                consumed = 0;
                return false;
            }

            char expected = blob[blobIndex];
            char actual = text[textIndex];
            if (expected == actual || (expected == '臺' && actual == '台'))
            {
                textIndex++;
                blobIndex++;
                continue;
            }

            if (TryMatchArabicOrdinal(text, textIndex, blob, blobIndex, end, out int digitLength, out int chineseLength))
            {
                textIndex += digitLength;
                blobIndex += chineseLength;
                continue;
            }

            consumed = 0;
            return false;
        }

        consumed = textIndex - index;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRoadOrdinalUnit(char ch)
        => ch is '段' or '路' or '街' or '巷' or '弄';

    private static bool TryMatchArabicOrdinal(
        string text,
        int index,
        string blob,
        int blobIndex,
        int blobEnd,
        out int length,
        out int chineseLength)
    {
        length = 0;
        chineseLength = 0;

        if (index >= text.Length || text[index] < '0' || text[index] > '9')
        {
            return false;
        }

        var ordinalEnd = blobIndex;
        while (ordinalEnd < blobEnd && IsChineseOrdinalChar(blob[ordinalEnd]))
        {
            ordinalEnd++;
        }

        if (ordinalEnd == blobIndex || ordinalEnd >= blobEnd || !IsRoadOrdinalUnit(blob[ordinalEnd]))
        {
            return false;
        }

        if (!TryParseChineseOrdinal(blob, blobIndex, ordinalEnd - blobIndex, out var expected))
        {
            return false;
        }

        var digitEnd = index;
        var actual = 0;
        while (digitEnd < text.Length && text[digitEnd] >= '0' && text[digitEnd] <= '9')
        {
            actual = actual * 10 + (text[digitEnd] - '0');
            digitEnd++;
        }

        if (actual != expected)
        {
            return false;
        }

        length = digitEnd - index;
        chineseLength = ordinalEnd - blobIndex;
        return true;
    }

    private static bool TryParseChineseOrdinal(string text, int index, int length, out int value)
    {
        value = 0;
        if (length <= 0)
        {
            return false;
        }

        if (length == 1)
        {
            value = ChineseOrdinalToDigit(text[index]);
            return value >= 0;
        }

        var tenOffset = -1;
        for (int i = 0; i < length; i++)
        {
            if (text[index + i] == '十')
            {
                tenOffset = i;
                break;
            }
        }

        if (tenOffset < 0)
        {
            return false;
        }

        var tens = tenOffset == 0 ? 1 : ChineseOrdinalToDigit(text[index]);
        if (tens <= 0)
        {
            return false;
        }

        var ones = 0;
        if (tenOffset + 1 < length)
        {
            if (tenOffset + 2 != length)
            {
                return false;
            }

            ones = ChineseOrdinalToDigit(text[index + tenOffset + 1]);
            if (ones < 0)
            {
                return false;
            }
        }

        value = tens * 10 + ones;
        return true;
    }

    private static bool RegionMatchesTaiEquivalent(string text, int textIndex, string expected, int length)
    {
        for (int i = 0; i < length; i++)
        {
            var actual = text[textIndex + i];
            var want = expected[i];
            if (actual != want && !(actual is '台' or '臺' && want is '台' or '臺'))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsChineseOrdinalChar(char ch)
        => ch is '○' or '一' or '二' or '三' or '四' or '五' or '六' or '七' or '八' or '九' or '十';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ChineseOrdinalToDigit(char ch)
    {
        return ch switch
        {
            '○' => 0,
            '一' => 1,
            '二' => 2,
            '三' => 3,
            '四' => 4,
            '五' => 5,
            '六' => 6,
            '七' => 7,
            '八' => 8,
            '九' => 9,
            _ => -1
        };
    }
}
