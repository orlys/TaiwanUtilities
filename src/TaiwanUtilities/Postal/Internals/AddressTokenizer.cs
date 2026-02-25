// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


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
    private static HashSet<string>? _specialRoadNames;
    private static readonly object _specialRoadNamesLock = new object();
    private static bool _specialRoadNamesInitialized = false;

    /// <summary>
    /// 台灣所有縣市名稱（包含特殊行政區）
    /// 按長度降序排列以支援最長前綴匹配
    /// </summary>
    private static readonly string[] AllCities = new[]
    {
        // 直轄市（6個）
        "臺北市", "新北市", "桃園市", "臺中市", "臺南市", "高雄市",
        // 市（3個）
        "基隆市", "新竹市", "嘉義市",
        // 縣（13個）
        "新竹縣", "苗栗縣", "彰化縣", "南投縣", "雲林縣", "嘉義縣", "屏東縣",
        "宜蘭縣", "花蓮縣", "臺東縣", "澎湖縣", "金門縣", "連江縣",
        // 特殊行政區（2個）
        "南海諸", "釣魚臺"
    };

    /// <summary>
    /// Token 正規表達式：匹配地址中的各個組成部分
    /// 支援多層附號（如：150之1之1之1）
    /// </summary>
    private static readonly Regex TokenRegex = new Regex(
        @"(?:(?<no>\d+)(?<subno>(?:之\d+)+)?(?=[巷弄衖號樓室層線]|$)|(?<name>.+?))(?:(?<unit>[縣市鄉鎮市區村里鄰路街段巷弄衖號樓室層線])|(?=\d+(?:之\d+)*[巷弄衖號樓室層線]|$))",
        RegexOptions.Compiled);

    /// <summary>
    /// 需要替換的字元正規表達式
    /// 支援小寫中文數字（一二三...九十）和大寫中文數字（壹貳參...玖拾佰）
    /// 必須以數字字元（非位數字元如十/拾/百/千）結尾，避免匹配路名中的「二十路」等
    /// </summary>
    private static readonly Regex ToReplaceRegex = new Regex(
        @"[ 　,，台~-]|[０-９]|[一二三四五六七八九十壹貳參叁肆伍陸柒捌玖拾佰仟百千]*[一二三四五六七八九壹貳參叁肆伍陸柒捌玖](?=[段路街巷弄號樓層])",
        RegexOptions.Compiled);

    /// <summary>
    /// 字元替換映射表
    /// </summary>
    private static readonly Dictionary<string, string> ToReplaceMap = new()
    {
        ["-"] = "之", ["~"] = "之", ["台"] = "臺",
        ["１"] = "1", ["２"] = "2", ["３"] = "3", ["４"] = "4", ["５"] = "5",
        ["６"] = "6", ["７"] = "7", ["８"] = "8", ["９"] = "9", ["０"] = "0",
        ["一"] = "1", ["二"] = "2", ["三"] = "3", ["四"] = "4", ["五"] = "5",
        ["六"] = "6", ["七"] = "7", ["八"] = "8", ["九"] = "9"
    };

    /// <summary>
    /// 中文數字集合（小寫+大寫+位數）
    /// </summary>
    private static readonly HashSet<char> ChineseNumeralsSet = new("一二三四五六七八九十壹貳參叁肆伍陸柒捌玖拾佰仟百千");

    /// <summary>
    /// 初始化特殊路名快取（延遲載入）
    /// </summary>
    private static void EnsureSpecialRoadNamesLoaded()
    {
        if (_specialRoadNamesInitialized)
            return;

        lock (_specialRoadNamesLock)
        {
            if (_specialRoadNamesInitialized)
                return;

            try
            {
                _specialRoadNames = Database.LoadSpecialRoadNames();
                _specialRoadNamesInitialized = true;
            }
            catch
            {
                // 如果載入失敗（例如資料庫尚未初始化），使用空集合
                _specialRoadNames = new HashSet<string>();
                _specialRoadNamesInitialized = true;
            }
        }
    }

    /// <summary>
    /// 檢查地址是否包含特殊路名
    /// </summary>
    private static bool ContainsSpecialRoadName(string address)
    {
        EnsureSpecialRoadNamesLoaded();

        if (_specialRoadNames == null || _specialRoadNames.Count == 0)
            return false;

        // 檢查地址是否包含任何特殊路名
        foreach (var specialRoad in _specialRoadNames)
        {
            if (address.Contains(specialRoad))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 正規化地址字串：統一格式、轉換數字等
    /// </summary>
    internal static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;

        // 正則表達式已經限制了只在 [段路街巷弄號樓層] 之前才轉換中文數字
        // 所以"一村巷"中的"一"不會被轉換（因為後面是"村"）
        // 但"一段"中的"一"會被轉換（因為後面是"段"）
        return ToReplaceRegex.Replace(s, m =>
        {
            var found = m.Value;

            if (ToReplaceMap.TryGetValue(found, out var replacement))
            {
                return replacement;
            }

            // 處理中文數字：使用 TaiwanUtilities.ChineseNumeric 作為通用轉換器
            // 支援小寫（一二三...九十）、大寫（壹貳參...玖拾佰）和複合格式
            if (found.Length > 0 && ChineseNumeralsSet.Contains(found[0]))
            {
                if (ChineseNumeric.TryParse(found, out var parsed))
                {
                    return ((int)(decimal)parsed).ToString();
                }
            }

            return string.Empty;
        });
    }

    /// <summary>
    /// 將地址字串分詞成 token 陣列
    /// </summary>
    internal static List<string[]> Tokenize(string addrStr)
    {
        var normalized = Normalize(addrStr);
        var tokens = new List<string[]>();

        // 先識別城市名稱（前綴匹配）
        string? cityName = null;
        string remainingAddress = normalized;

        foreach (var city in AllCities)
        {
            if (normalized.StartsWith(city))
            {
                cityName = city;
                remainingAddress = normalized.Substring(city.Length);
                break;
            }
        }

        // 如果識別到城市，加入城市 token
        if (cityName != null)
        {
            // 對於特殊行政區（不以縣/市結尾），使用完整名稱作為 NAME+UNIT
            if (!cityName.EndsWith("縣") && !cityName.EndsWith("市"))
            {
                // 特殊行政區：name 為完整名稱，unit 為空（以便與標準縣市區分）
                tokens.Add(new[] { "", "", cityName, "" });
            }
            // 標準縣市：保持原有的 name+unit 結構
            else
            {
                var cityNameWithoutUnit = cityName.Substring(0, cityName.Length - 1);
                var unit = cityName.Substring(cityName.Length - 1);
                tokens.Add(new[] { "", "", cityNameWithoutUnit, unit });
            }
        }

        // 對剩餘地址進行正常 tokenization
        var matches = TokenRegex.Matches(remainingAddress);
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

    internal AddressTokenizer(string addrStr)
    {
        Tokens = Tokenize(addrStr);
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

        return string.Concat(
            Tokens.Skip(startIdx)
                  .Take(Math.Max(0, endIdx - startIdx))
                  .Select(t => string.Concat(t))
        );
    }

    /// <summary>
    /// 挑選特定索引的 tokens 並扁平化
    /// </summary>
    internal string PickToFlat(params int[] idxs)
    {
        return string.Concat(idxs.Select(idx => string.Concat(Tokens[idx])));
    }

    /// <summary>
    /// 解析指定位置的門牌號碼（包含所有附號）
    /// </summary>
    /// <returns>(號碼, 附號陣列) 的元組，如 150之1之1之1 返回 (150, [1, 1, 1])</returns>
    internal (int No, List<int> SubNos) Parse(int idx)
    {
        if (idx < 0)
            idx = Tokens.Count + idx;

        if (idx < 0 || idx >= Tokens.Count)
            return (0, new List<int>());

        var token = Tokens[idx];
        var no = int.TryParse(token[NO], out var n) ? n : 0;
        var subnos = new List<int>();

        // 解析所有附號：之1之1之1 → [1, 1, 1]
        if (!string.IsNullOrEmpty(token[SUBNO]))
        {
            var subnoStr = token[SUBNO];
            var parts = subnoStr.Split(new[] { '之' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (int.TryParse(part, out var sn))
                    subnos.Add(sn);
            }
        }

        return (no, subnos);
    }

    public override string ToString()
    {
        return $"AddressTokenizer('{Flat()}')";
    }
}
