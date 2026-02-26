// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;

/// <summary>
/// 郵遞區號查詢結果類型
/// </summary>
public enum ZipCodeResultType
{
    /// <summary>
    /// 完整匹配（有明確規則）
    /// </summary>
    ExactMatch,

    /// <summary>
    /// 部分匹配（漸進式查詢）
    /// </summary>
    PartialMatch,

    /// <summary>
    /// 未找到
    /// </summary>
    NotFound
}

/// <summary>
/// 郵遞區號查詢的完整結果
/// </summary>
public record ZipCodeResult
{
    /// <summary>
    /// 結果類型
    /// </summary>
    public ZipCodeResultType ResultType { get; set; }

    /// <summary>
    /// 郵遞區號（3 或 5 碼）
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// 3 碼郵遞區號
    /// </summary>
    public string ZipCode3
    {
        get
        {
            if (string.IsNullOrEmpty(ZipCode))
            {
                return string.Empty;
            }

            return ZipCode.Length >= 3 ? ZipCode.Substring(0, 3) : ZipCode;
        }
    }

    /// <summary>
    /// 5 碼郵遞區號（如果有）。實際上台灣郵遞區號是3+2+1格式（6碼）
    /// </summary>
    public string? ZipCode5
    {
        get
        {
            if (string.IsNullOrEmpty(ZipCode))
            {
                return null;
            }

            return ZipCode.Length > 3 ? ZipCode : null;
        }
    }

    /// <summary>
    /// 原始地址
    /// </summary>
    public string OriginalAddress { get; set; } = string.Empty;

    /// <summary>
    /// 正規化地址
    /// </summary>
    public string NormalizedAddress { get; set; } = string.Empty;

    /// <summary>
    /// 解析的郵政地址
    /// </summary>
    public PostalAddress? Address { get; set; }

    /// <summary>
    /// 匹配的投遞規則
    /// </summary>
    public PostalDeliveryRule? MatchedRule { get; set; }

    /// <summary>
    /// 匹配的地址範圍
    /// </summary>
    public string? MatchedScope { get; set; }

    /// <summary>
    /// 部門/大樓名稱（如：匯大大廈、國土管理署）
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// 投遞郵局名稱（如：臺北郵局郵件投遞科中山投遞股-91區）
    /// </summary>
    public string? Office { get; set; }

    /// <summary>
    /// 是否找到郵遞區號
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(ZipCode);

    /// <summary>
    /// 是否為完整匹配
    /// </summary>
    public bool IsExactMatch => ResultType == ZipCodeResultType.ExactMatch;

    /// <summary>
    /// 額外訊息
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// 建議的地址候選
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 建立「未找到」結果
    /// </summary>
    /// <param name="address">原始地址</param>
    /// <returns>未找到結果</returns>
    public static ZipCodeResult NotFound(string address)
    {
        var addr = new AddressTokenizer(address);
        return new ZipCodeResult
        {
            ResultType = ZipCodeResultType.NotFound,
            OriginalAddress = address,
            NormalizedAddress = addr.Flat(),
            Address = PostalAddress.Parse(address),
            Messages = new List<string> { "找不到此地址的郵遞區號" }
        };
    }

    /// <summary>
    /// 建立「部分匹配」結果
    /// </summary>
    /// <param name="address">原始地址</param>
    /// <param name="zipcode">部分匹配的郵遞區號</param>
    /// <param name="scope">匹配的地址範圍</param>
    /// <returns>部分匹配結果</returns>
    public static ZipCodeResult PartialMatch(string address, string zipcode, string scope)
    {
        var addr = new AddressTokenizer(address);
        return new ZipCodeResult
        {
            ResultType = ZipCodeResultType.PartialMatch,
            ZipCode = zipcode,
            OriginalAddress = address,
            NormalizedAddress = addr.Flat(),
            Address = PostalAddress.Parse(address),
            MatchedScope = scope,
            Messages = new List<string> { $"找到部分匹配：{scope}" }
        };
    }

    /// <summary>
    /// 建立「完整匹配」結果
    /// </summary>
    /// <param name="address">原始地址</param>
    /// <param name="zipcode">郵遞區號</param>
    /// <param name="ruleStr">匹配的規則字串</param>
    /// <returns>完整匹配結果</returns>
    public static ZipCodeResult ExactMatch(string address, string zipcode, string ruleStr)
    {
        var addr = new AddressTokenizer(address);
        var components = PostalAddress.Parse(address);
        PostalDeliveryRule? rule = null;

        try
        {
            rule = PostalDeliveryRule.Parse(ruleStr);
        }
        catch
        {
            // 如果規則解析失敗，不影響結果
        }

        return new ZipCodeResult
        {
            ResultType = ZipCodeResultType.ExactMatch,
            ZipCode = zipcode,
            OriginalAddress = address,
            NormalizedAddress = addr.Flat(),
            Address = components,
            MatchedRule = rule,
            MatchedScope = ruleStr,
            Messages = new List<string> { $"完整匹配：{zipcode}" }
        };
    }

}