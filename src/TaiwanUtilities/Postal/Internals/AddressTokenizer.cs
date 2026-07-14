// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using TaiwanUtilities.Internals;


/// <summary>
/// 台灣地址的分詞器（Tokenizer）
/// 負責將地址字串正規化並分解為結構化的 tokens
/// </summary>
internal class AddressTokenizer
{
    // Token 索引常數
    internal const int NO = 0;
    internal const int SUBNO = 1;
    internal const int NAME = 2;
    internal const int UNIT = 3;

    /// <summary>
    /// 地址的 token 陣列，每個 token 為 (號碼, 附號, 名稱, 單位) 的四元組
    /// </summary>
    internal List<string[]> Tokens { get; set; }

    /// <summary>
    /// 特殊路名快取（不以 路/街/道 結尾的路名，避免中文數字轉換）
    /// </summary>
    private static HashSet<string>? s_specialRoadNames;
    private static readonly object s_specialRoadNamesLock = new object();
    private static volatile bool s_specialRoadNamesInitialized = false;

    /// <summary>
    /// 台灣所有縣市名稱（包含特殊行政區）
    /// 按長度降序排列以支援最長前綴匹配
    /// </summary>
    private static readonly string[] s_allCities =
    [
        // 直轄市（6個）
        "臺北市", "新北市", "桃園市", "臺中市", "臺南市", "高雄市",
        // 市（3個）
        "基隆市", "新竹市", "嘉義市",
        // 縣（13個）
        "新竹縣", "苗栗縣", "彰化縣", "南投縣", "雲林縣", "嘉義縣", "屏東縣",
        "宜蘭縣", "花蓮縣", "臺東縣", "澎湖縣", "金門縣", "連江縣",
        // 特殊行政區（2個）
        "南海諸", "釣魚臺"
    ];

    /// <summary>
    /// Token 正規表達式：匹配地址中的各個組成部分
    /// 支援多層附號（如：150之1之1之1）
    /// </summary>
    private static readonly Regex s_tokenRegex = new Regex(
        @"(?:(?<no>\d+)(?<subno>(?:之\d+)+)?(?=[巷弄衖號樓室層線]|$)|(?<name>.+?))(?:(?<unit>[縣市鄉鎮市區村里鄰路街段巷弄衖號樓室層線])|(?=\d+(?:之\d+)*[巷弄衖號樓室層線]|$))",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// 需要替換的字元正規表達式
    /// 支援全形數字、空白標點與門牌尾段數值正規化。
    /// 地理 key（縣市/行政區/路街段）不可在前置階段改寫成資料庫不存在的形式。
    /// </summary>
    private static readonly Regex s_toReplaceRegex = new Regex(
        @"[ 　,，鄕~-]|[０-９]|[一二三四五六七八九十壹貳參叁肆伍陸柒捌玖拾佰仟百千]*[一二三四五六七八九壹貳參叁肆伍陸柒捌玖](?=[巷弄號樓層室])",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// 門牌附號慣寫重排：「2號之3」→ 官方形式「2之3號」。
    /// 僅限地址結尾，避免誤動「5樓之3」與「2號之3樓」等歧義形；
    /// 附號允許中文數字（如 2號之三），重排時一併轉為阿拉伯數字。
    /// </summary>
    private static readonly Regex s_trailingSubNoRegex = new Regex(
        @"(\d+)號((?:之(?:\d+|[一二三四五六七八九十壹貳參叁肆伍陸柒捌玖拾佰仟百千]+))+)$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// 字元替換映射表
    /// </summary>
    private static readonly Dictionary<string, string> s_toReplaceMap = new()
    {
        ["-"] = "之",
        ["~"] = "之",
        ["鄕"] = "鄉",
        ["１"] = "1",
        ["２"] = "2",
        ["３"] = "3",
        ["４"] = "4",
        ["５"] = "5",
        ["６"] = "6",
        ["７"] = "7",
        ["８"] = "8",
        ["９"] = "9",
        ["０"] = "0",
        ["一"] = "1",
        ["二"] = "2",
        ["三"] = "3",
        ["四"] = "4",
        ["五"] = "5",
        ["六"] = "6",
        ["七"] = "7",
        ["八"] = "8",
        ["九"] = "9"
    };

    /// <summary>
    /// 中文數字集合（小寫+大寫+位數）
    /// </summary>
    private static readonly HashSet<char> s_chineseNumeralsSet = new("一二三四五六七八九十壹貳參叁肆伍陸柒捌玖拾佰仟百千");

    /// <summary>
    /// 初始化特殊路名快取（延遲載入）
    /// </summary>
    private static void EnsureSpecialRoadNamesLoaded()
    {
        if (s_specialRoadNamesInitialized)
        {
            return;
        }

        lock (s_specialRoadNamesLock)
        {
            if (s_specialRoadNamesInitialized)
            {
                return;
            }

            try
            {
                s_specialRoadNames = PostalData.SpecialRoadNames;
                s_specialRoadNamesInitialized = true;
            }
            catch
            {
                // 如果載入失敗（例如資料庫尚未初始化），使用空集合
                s_specialRoadNames = new HashSet<string>();
                s_specialRoadNamesInitialized = true;
            }
        }
    }

    /// <summary>
    /// 檢查地址是否包含特殊路名
    /// </summary>
    private static bool ContainsSpecialRoadName(string address)
    {
        EnsureSpecialRoadNamesLoaded();

        if (s_specialRoadNames == null || s_specialRoadNames.Count == 0)
        {
            return false;
        }

        // 使用 HashSet 的 Contains 逐段檢查（以滑動視窗減少比對次數）
        foreach (var specialRoad in s_specialRoadNames)
        {
            if (specialRoad.Length <= address.Length && address.IndexOf(specialRoad, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 正規化地址字串：統一格式、轉換數字等
    /// </summary>
    /// <summary>
    /// 地址最大長度限制，防止超長字串導致 CPU 高消耗
    /// </summary>
    private const int MAX_ADDRESS_LENGTH = 500;

    internal static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        if (s.Length > MAX_ADDRESS_LENGTH)
        {
            s = s[..MAX_ADDRESS_LENGTH];
        }

        var replaced = s_toReplaceRegex.Replace(s, m =>
        {
            var found = m.Value;

            if (s_toReplaceMap.TryGetValue(found, out var replacement))
            {
                return replacement;
            }

            // 處理中文數字：使用 TaiwanUtilities.ChineseNumeric 作為通用轉換器
            // 支援小寫（一二三...九十）、大寫（壹貳參...玖拾佰）和複合格式
            if (found.Length > 0 && s_chineseNumeralsSet.Contains(found[0]))
            {
                if (ChineseNumeric.TryParse(found, out var parsed))
                {
                    return ((int)(decimal)parsed).ToString();
                }
            }

            return string.Empty;
        });

        return s_trailingSubNoRegex.Replace(replaced, static m =>
        {
            var sb = new StringBuilder(m.Groups[1].Value);
            foreach (var part in m.Groups[2].Value.Split(['之'], StringSplitOptions.RemoveEmptyEntries))
            {
                sb.Append('之');
                sb.Append(s_chineseNumeralsSet.Contains(part[0]) && ChineseNumeric.TryParse(part, out var parsed)
                    ? ((int)(decimal)parsed).ToString()
                    : part);
            }

            return sb.Append('號').ToString();
        });
    }

    /// <summary>
    /// 將地址字串分詞成 token 陣列
    /// </summary>
    internal static List<string[]> Tokenize(string addrStr) => Tokenize(addrStr, out _);

    internal static List<string[]> Tokenize(string addrStr, out string? matchedRoadKey)
    {
        matchedRoadKey = null;
        var normalized = Normalize(addrStr);
        var tokens = new List<string[]>();

        // 先識別城市名稱（前綴匹配）
        string? cityName = null;
        string remainingAddress = normalized;

        foreach (var city in s_allCities)
        {
            if (StartsWithTaiEquivalent(normalized, city))
            {
                cityName = city;
                remainingAddress = normalized[city.Length..];
                break;
            }
        }

        // 如果識別到城市，加入城市 token
        if (cityName != null)
        {
            // 對於特殊行政區（不以縣/市結尾），使用完整名稱作為 NAME+UNIT
            if (!cityName.EndsWith('縣') && !cityName.EndsWith('市'))
            {
                // 特殊行政區：name 為完整名稱，unit 為空（以便與標準縣市區分）
                tokens.Add([string.Empty, string.Empty, cityName, string.Empty]);
            }
            // 標準縣市：保持原有的 name+unit 結構
            else
            {
                var cityNameWithoutUnit = cityName[..^1];
                var unit = cityName[^1..];
                tokens.Add([string.Empty, string.Empty, cityNameWithoutUnit, unit]);
            }

            // 以資料庫的區清單為準先切出行政區，避免 regex 在區名內部的
            // 市/鎮/鄉 處誤切（如「前鎮區」被切成「前鎮」+「區復興…路」）。
            // 查無對應區時（未知/改制區名）回退到 regex，不影響原有行為。
            var districtLen = PostalLookup.MatchLongestDistrict(cityName, remainingAddress, 0);
            string? district = null;
            if (districtLen > 0)
            {
                district = remainingAddress[..districtLen];
                tokens.Add([string.Empty, string.Empty, district[..^1], district[^1..]]);
                remainingAddress = remainingAddress[districtLen..];
            }

            if (district != null)
            {
                var roadLen = PostalLookup.MatchLongestRoad(cityName, district, remainingAddress, 0);
                string? matchedRoad = roadLen > 0
                    ? PostalLookup.GetLongestRoadName(cityName, district, remainingAddress, 0)
                    : null;
                if (roadLen > 0 && roadLen < remainingAddress.Length && remainingAddress[roadLen] is '村' or '里')
                {
                    roadLen = 0;
                    matchedRoad = null;
                }

                if (roadLen == 0)
                {
                    var villageLen = MatchVillagePrefix(remainingAddress);
                    if (villageLen > 0)
                    {
                        var village = remainingAddress[..villageLen];
                        tokens.Add([string.Empty, string.Empty, village[..^1], village[^1..]]);
                        remainingAddress = remainingAddress[villageLen..];
                        roadLen = PostalLookup.MatchLongestRoad(cityName, district, remainingAddress, 0);
                        matchedRoad = roadLen > 0
                            ? PostalLookup.GetLongestRoadName(cityName, district, remainingAddress, 0)
                            : null;
                    }
                }

                if (roadLen > 0 && matchedRoad != null)
                {
                    matchedRoadKey = matchedRoad;
                    AddRoadTokens(tokens, cityName, district, matchedRoad);
                    remainingAddress = remainingAddress[roadLen..];
                }
            }
        }

        // 對剩餘地址進行正常 tokenization
        var matches = s_tokenRegex.Matches(remainingAddress);
        foreach (Match match in matches)
        {
            var token = new string[4];
            token[NO] = match.Groups["no"].Value;
            token[SUBNO] = match.Groups["subno"].Value;
            token[NAME] = match.Groups["name"].Value;
            token[UNIT] = match.Groups["unit"].Value;
            tokens.Add(token);
        }

        return tokens;
    }

    private static int MatchVillagePrefix(string text)
    {
        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] is '村' or '里')
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// 依資料庫拆解命中的路名鍵。若該鍵有個「本身也是同區獨立規則」的路型前綴，
    /// 且尾段不是純段號（一段/東段），就拆成「前綴路段 ＋ 尾段附加資訊」：
    /// 尾段以巷/弄結尾歸 Lane/Alley，其餘歸 Locality（如 篤行三村、信和產業道）。
    /// 否則整串交由 <see cref="AddKnownRoadToken"/> 處理（保留原本的路＋段拆法）。
    /// </summary>
    private static void AddRoadTokens(List<string[]> tokens, string city, string district, string road)
    {
        int prefixLen = PostalLookup.LongestNestedRoadPrefix(city, district, road);
        if (prefixLen > 0)
        {
            var tail = road[prefixLen..];
            if (tail.Length >= 2 && !IsPureSection(tail) && !IsNumberedRoadBranch(tail))
            {
                AddKnownRoadToken(tokens, road[..prefixLen]);
                if (tail[^1] is '巷' or '弄')
                {
                    tokens.Add([string.Empty, string.Empty, tail[..^1], tail[^1..]]);
                }
                else
                {
                    tokens.Add([string.Empty, string.Empty, tail, string.Empty]);
                }
                return;
            }
        }

        AddKnownRoadToken(tokens, road);
    }

    /// <summary>
    /// 尾段是否為「編號分支路」（如 一街、二街、三號農路）：以序號起頭、以路/街/道結尾，
    /// 屬緊附於母路的編號支路，應整串留作路名，不拆成附加資訊。
    /// 相對地，非序號起頭的路型尾段（如 信和產業道）視為附加資訊拆出。
    /// </summary>
    private static bool IsNumberedRoadBranch(string tail)
    {
        if (tail[^1] is not ('路' or '街' or '道'))
        {
            return false;
        }

        var first = tail[0];
        return first is '一' or '二' or '三' or '四' or '五' or '六' or '七' or '八' or '九' or '十' or '○'
            || (first >= '0' && first <= '9');
    }

    /// <summary>尾段是否為純段號（如 一段、東段、中段），是則不應被當附加資訊拆走。</summary>
    private static bool IsPureSection(string tail)
    {
        if (tail.Length < 2 || tail[^1] != '段')
        {
            return false;
        }

        for (int i = 0; i < tail.Length - 1; i++)
        {
            var ch = tail[i];
            var ordinal = ch is '一' or '二' or '三' or '四' or '五' or '六' or '七' or '八' or '九' or '十' or '○';
            var direction = ch is '東' or '西' or '南' or '北' or '中';
            if (!ordinal && !direction)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddKnownRoadToken(List<string[]> tokens, string road)
    {
        if (road.EndsWith('段'))
        {
            var sectionStart = LastRoadUnitIndex(road, road.Length - 2);
            if (sectionStart >= 0 && sectionStart + 1 < road.Length - 1)
            {
                var roadName = road[..(sectionStart + 1)];
                tokens.Add([string.Empty, string.Empty, roadName[..^1], roadName[^1..]]);
                tokens.Add([string.Empty, string.Empty, road[(sectionStart + 1)..^1], road[^1..]]);
                return;
            }
        }

        if (road.Length > 0 && IsTokenUnit(road[road.Length - 1]) && road[road.Length - 1] is not ('村' or '里'))
        {
            tokens.Add([string.Empty, string.Empty, road[..^1], road[^1..]]);
        }
        else
        {
            tokens.Add([string.Empty, string.Empty, road, string.Empty]);
        }
    }

    private static bool IsTokenUnit(char ch)
        => ch is '縣' or '市' or '鄉' or '鎮' or '區' or '村' or '里' or '鄰' or '路' or '街' or '道' or '段' or '巷' or '弄' or '衖' or '號' or '樓' or '室' or '層' or '線';

    private static bool IsAllAsciiDigits(string s)
    {
        if (s.Length == 0) return false;
        foreach (var ch in s)
            if (ch < '0' || ch > '9') return false;
        return true;
    }

    private static int LastRoadUnitIndex(string text, int startIndex)
    {
        for (int i = startIndex; i >= 0; i--)
        {
            if (text[i] is '路' or '街' or '道')
            {
                return i;
            }
        }

        return -1;
    }

    private static bool StartsWithTaiEquivalent(string text, string prefix)
    {
        if (text.Length < prefix.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Length; i++)
        {
            if (!CharsEqualTaiEquivalent(text[i], prefix[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CharsEqualTaiEquivalent(char a, char b)
        => a == b || (a is '台' or '臺' && b is '台' or '臺');

    /// <summary>命中資料庫的完整原生路名鍵（未拆解），未匹配時為 null。</summary>
    internal string? MatchedRoadKey { get; private set; }

    internal AddressTokenizer(string addrStr)
    {
        Tokens = Tokenize(addrStr, out var roadKey);
        MatchedRoadKey = roadKey;
    }

    /// <summary>
    /// 將所有 tokens 扁平化為字串（對應 Python 的 tokens[:]）
    /// </summary>
    internal string Flat()
    {
        return Flat(0, Tokens.Count);
    }

    /// <summary>
    /// 將前 n 個 tokens 扁平化為字串（對應 Python 的 tokens[:n]）
    /// </summary>
    internal string Flat(int end)
    {
        return Flat(0, end);
    }

    /// <summary>
    /// 將指定範圍的 tokens 扁平化為字串（對應 Python 的 tokens[start:end]）
    /// </summary>
    internal string Flat(int start, int end)
    {
        var startIdx = start < 0 ? Tokens.Count + start : start;
        var endIdx = end < 0 ? Tokens.Count + end : end;
        var count = Math.Max(0, endIdx - startIdx);

        if (count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        for (var i = startIdx; i < startIdx + count && i < Tokens.Count; i++)
        {
            var t = Tokens[i];
            sb.Append(t[0]).Append(t[1]).Append(t[2]).Append(t[3]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 挑選特定索引的 tokens 並扁平化
    /// </summary>
    internal string PickToFlat(params int[] idxs)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var idx in idxs)
        {
            var t = Tokens[idx];
            sb.Append(t[0]).Append(t[1]).Append(t[2]).Append(t[3]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 解析指定位置的門牌號碼（包含所有附號）
    /// </summary>
    /// <returns>(號碼, 附號陣列) 的元組，如 150之1之1之1 返回 (150, [1, 1, 1])</returns>
    internal (int No, List<int> SubNos) Parse(int idx)
    {
        if (idx < 0)
        {
            idx = Tokens.Count + idx;
        }

        if (idx < 0 || idx >= Tokens.Count)
        {
            return (0, new List<int>());
        }

        var token = Tokens[idx];
        var no = int.TryParse(token[NO], out var n) ? n : 0;
        var subnos = new List<int>();

        // 解析所有附號：之1之1之1 → [1, 1, 1]
        if (!string.IsNullOrEmpty(token[SUBNO]))
        {
            var subnoStr = token[SUBNO];
            var parts = subnoStr.Split(['之'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var sn))
                {
                    subnos.Add(sn);
                }
            }
        }

        return (no, subnos);
    }

    public override string ToString()
    {
        return $"AddressTokenizer('{Flat()}')";
    }
}
