// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;

/// <summary>
/// 台灣郵遞區號查詢工具
/// </summary>
/// <remarks>
/// 此類別提供台灣郵遞區號的查詢、驗證、解析等功能。
/// 內部使用單例的 PostalDatabase 類別，無需手動管理資料庫連接。
/// </remarks>
public static class ZipCode
{

    /// <summary>
    /// 查詢地址的郵遞區號
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <returns>完整的查詢結果，包含郵遞區號、地址組件、匹配規則等資訊</returns>
    /// <remarks>
    /// <para>
    /// 此方法優先使用預載入規則引擎（純記憶體，延遲 &lt; 0.5ms），
    /// 若無匹配則自動降級到 SQLite 查詢（漸進式匹配）。
    /// </para>
    /// <para>
    /// 預載入引擎僅支援精確匹配（需要完整的縣市、區、路和門牌號碼），
    /// 部分地址查詢（如僅提供"臺北市"或"臺北市信義區"）會使用 SQLite 查詢。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = ZipCode.Find("臺北市信義區市府路1號");
    /// Console.WriteLine($"郵遞區號: {result.ZipCode}");          // 110204
    /// Console.WriteLine($"類型: {result.ResultType}");          // ExactMatch
    /// Console.WriteLine($"縣市: {result.Address.City}");        // 臺北市
    /// Console.WriteLine($"區: {result.Address.District}");      // 信義區
    /// if (result.MatchedRule != null)
    ///     Console.WriteLine($"規則: {result.MatchedRule.GetDescription()}");
    /// </code>
    /// </example>
    public static ZipCodeResult Find(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return ZipCodeResult.NotFound(address ?? string.Empty);

        // 檢查環境變數，允許用戶選擇是否使用預載入引擎
        var enablePreloaded = Environment.GetEnvironmentVariable("PEAK_ENABLE_PRELOADED") != "0";

        if (enablePreloaded)
        {
            // 優先使用預載入引擎（純記憶體，無 SQLite I/O）
            var addr = PostalAddress.Parse(address);
            var preloadedResult = PostalRulesEngine.Find(addr);

            if (preloadedResult != null)
                return preloadedResult;
        }

        // 降級到 SQLite 查詢（支援漸進式匹配和其他複雜場景）
        return PostalDatabase.ExecuteQuery(dir => dir.FindDetailed(address));
    }

    /// <summary>
    /// 驗證地址是否合法且門牌號碼在合理範圍內
    /// </summary>
    /// <param name="address">完整地址字串</param>
    /// <returns>驗證結果，包含是否有效、郵遞區號、錯誤訊息等</returns>
    /// <example>
    /// <code>
    /// // 有效地址
    /// var result = ZipCode.ValidateAddress("臺北市信義區市府路1號");
    /// // result.IsValid = true, result.ZipCode = "110204"
    ///
    /// // 門牌號碼超出範圍
    /// var result2 = ZipCode.ValidateAddress("臺北市信義區市府路99999號");
    /// // result2.IsValid = false, result2.FailureReason = NumberOutOfRange
    ///
    /// // 不存在的地址
    /// var result3 = ZipCode.ValidateAddress("臺北市不存在區某某路1號");
    /// // result3.IsValid = false, result3.FailureReason = AddressNotFound
    /// </code>
    /// </example>
    public static PostalValidationResult ValidateAddress(string address)
    {
        return PostalDatabase.ExecuteQuery(dir => dir.ValidateAddress(address));
    }

    /// <summary>
    /// 取得地址的所有可能投遞規則
    /// </summary>
    /// <param name="address">地址字串</param>
    /// <returns>郵遞區號和投遞規則的清單</returns>
    /// <example>
    /// <code>
    /// var rules = ZipCode.GetDeliveryRules("臺北市中正區三元街");
    ///
    /// foreach (var item in rules)
    /// {
    ///     Console.WriteLine($"郵遞區號: {item.ZipCode}");
    ///     Console.WriteLine($"規則類型: {item.Rule.Type}");
    ///     Console.WriteLine($"規則描述: {item.Rule.GetDescription()}");
    /// }
    /// </code>
    /// </example>
    public static List<ZipCodeDeliveryRule> GetDeliveryRules(string address)
    {
        return PostalDatabase.ExecuteQuery(dir => dir.GetDeliveryRules(address));
    }

    /// <summary>
    /// 取得地址候選清單（詳細版，包含郵遞區號和地址組件）
    /// </summary>
    /// <param name="partialAddress">部分地址</param>
    /// <param name="maxResults">最大返回數量（預設 10）</param>
    /// <returns>候選地址清單（包含郵遞區號和組件）</returns>
    /// <example>
    /// <code>
    /// var suggestions = ZipCode.GetSuggestions("臺北市中正區中", 5);
    ///
    /// foreach (var suggestion in suggestions)
    /// {
    ///     Console.WriteLine($"地址: {suggestion.AddressText}");
    ///     Console.WriteLine($"郵遞區號: {suggestion.ZipCode}");
    ///     Console.WriteLine($"縣市: {suggestion.Address?.City}");
    /// }
    /// </code>
    /// </example>
    public static List<PostalAddressSuggestion> GetSuggestions(string partialAddress, int maxResults = 10)
    {
        return PostalDatabase.ExecuteQuery(dir => dir.GetSuggestionsDetailed(partialAddress, maxResults));
    }
}
