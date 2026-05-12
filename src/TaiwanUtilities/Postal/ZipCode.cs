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
/// 台灣郵遞區號查詢工具
/// </summary>
public static class ZipCode
{
    /// <summary>
    /// 查詢地址的郵遞區號
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <returns>完整的查詢結果，包含郵遞區號、地址組件、匹配規則等資訊</returns>
    public static ZipCodeResult Find(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return ZipCodeResult.NotFound(address ?? string.Empty);
        }

        var addr = PostalAddress.Parse(address);
        var result = PostalRulesEngine.Find(addr);
        return result ?? ZipCodeResult.NotFound(address);
    }

    /// <summary>
    /// 驗證地址是否合法且門牌號碼在合理範圍內
    /// </summary>
    /// <param name="address">完整地址字串</param>
    /// <returns>驗證結果，包含是否有效、郵遞區號、錯誤訊息等</returns>
    public static PostalValidationResult ValidateAddress(string address)
    {
        var addr = PostalAddress.Parse(address);
        var normalized = addr?.NormalizedAddress ?? address;

        if (addr == null || string.IsNullOrEmpty(addr.City))
        {
            return Fail(PostalValidationFailureReason.InvalidFormat, normalized, "地址格式無效");
        }

        var key = $"{addr.City}|{addr.District}|{addr.Road}";

        if (!PostalData.Rules.TryGetValue(key, out var ruleSet))
        {
            // 逐步檢查縣市→行政區→街道
            bool hasCity = false;
            bool hasDistrict = false;
            var cityPrefix = addr.City + "|";
            var districtPrefix = $"{addr.City}|{addr.District}|";

            foreach (var k in PostalData.Rules.Keys)
            {
                if (k.StartsWith(cityPrefix, StringComparison.Ordinal))
                {
                    hasCity = true;
                    if (k.StartsWith(districtPrefix, StringComparison.Ordinal))
                    {
                        hasDistrict = true;
                        break;
                    }
                }
            }

            if (!hasCity)
            {
                return Fail(PostalValidationFailureReason.AddressNotFound, normalized, "找不到此地址");
            }

            if (!hasDistrict)
            {
                return Fail(PostalValidationFailureReason.DistrictNotFound, normalized, "找不到此行政區");
            }

            return Fail(PostalValidationFailureReason.StreetNotFound, normalized, "找不到此街道");
        }

        if (!addr.Number.HasValue)
        {
            return new PostalValidationResult
            {
                IsValid = true,
                ZipCode = PostalData.ZipCodePool[ruleSet.ZipCodeIndices[0]],
                NormalizedAddress = normalized
            };
        }

        int lane      = PostalRulesEngine.ParseNumericPrefix(addr.Lane);
        int alley     = PostalRulesEngine.ParseNumericPrefix(addr.Alley);
        int subNumber = (addr.SubNumbers != null && addr.SubNumbers.Count > 0)
                        ? addr.SubNumbers[0] : 0;

        if (!ruleSet.TryMatch(addr.Number.Value, subNumber, lane, alley,
            out int zipIdx, out _, out _, out _))
        {
            return Fail(PostalValidationFailureReason.NumberOutOfRange, normalized, "門牌號碼不在任何投遞規則範圍內");
        }

        return new PostalValidationResult
        {
            IsValid = true,
            ZipCode = PostalData.ZipCodePool[zipIdx],
            NormalizedAddress = normalized,
            Messages = new List<string> { "驗證通過" }
        };
    }

    /// <summary>
    /// 取得地址的所有可能投遞規則
    /// </summary>
    /// <param name="address">地址字串</param>
    /// <returns>郵遞區號和投遞規則的清單</returns>
    public static List<ZipCodeDeliveryRule> GetDeliveryRules(string address)
    {
        var addr = PostalAddress.Parse(address);
        if (addr == null || string.IsNullOrEmpty(addr.Road))
        {
            return new List<ZipCodeDeliveryRule>();
        }

        var key = $"{addr.City}|{addr.District}|{addr.Road}";
        if (!PostalData.Rules.TryGetValue(key, out var ruleSet))
        {
            return new List<ZipCodeDeliveryRule>();
        }

        var result = new List<ZipCodeDeliveryRule>(ruleSet.Count);
        for (int i = 0; i < ruleSet.Count; i++)
        {
            var zipCode  = PostalData.ZipCodePool[ruleSet.ZipCodeIndices[i]];
            var scopeStr = ruleSet.ScopeIndices[i] > 0 ? PostalData.Scopes[ruleSet.ScopeIndices[i]] : "全";
            try
            {
                result.Add(new ZipCodeDeliveryRule(zipCode, PostalDeliveryRule.Parse(scopeStr)));
            }
            catch
            {
                // 忽略規則解析失敗
            }
        }

        return result;
    }

    /// <summary>
    /// 取得地址候選清單（詳細版，包含郵遞區號和地址組件）
    /// </summary>
    /// <param name="partialAddress">部分地址</param>
    /// <param name="maxResults">最大返回數量（預設 10）</param>
    /// <returns>候選地址清單（包含郵遞區號和組件）</returns>
    public static List<PostalAddressSuggestion> GetSuggestions(string partialAddress, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(partialAddress))
        {
            return new List<PostalAddressSuggestion>();
        }

        var normalized = AddressTokenizer.Normalize(partialAddress);
        var results = new List<PostalAddressSuggestion>();

        foreach (var kvp in PostalData.Rules)
        {
            if (results.Count >= maxResults)
            {
                break;
            }

            var parts = kvp.Key.Split('|');
            if (parts.Length < 3)
            {
                continue;
            }

            var fullAddr = parts[0] + parts[1] + parts[2];
            if (fullAddr.StartsWith(normalized, StringComparison.Ordinal) ||
                normalized.StartsWith(fullAddr, StringComparison.Ordinal))
            {
                var zipCode = PostalData.ZipCodePool[kvp.Value.ZipCodeIndices[0]];
                results.Add(new PostalAddressSuggestion(fullAddr, zipCode, PostalAddress.Parse(fullAddr)));
            }
        }

        return results;
    }

    private static PostalValidationResult Fail(PostalValidationFailureReason reason, string normalized, string message)
        => new PostalValidationResult
        {
            IsValid = false,
            NormalizedAddress = normalized,
            FailureReason = reason,
            Messages = new List<string> { message }
        };
}
