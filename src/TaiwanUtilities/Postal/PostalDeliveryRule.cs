// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 郵遞投遞規則類型
/// </summary>
public enum PostalRuleType
{
    /// <summary>
    /// 全部門牌
    /// </summary>
    All,

    /// <summary>
    /// 單號
    /// </summary>
    Odd,

    /// <summary>
    /// 雙號
    /// </summary>
    Even,

    /// <summary>
    /// 指定號碼
    /// </summary>
    Specific,

    /// <summary>
    /// 號碼範圍
    /// </summary>
    Range,

    /// <summary>
    /// 大於等於
    /// </summary>
    GreaterOrEqual,

    /// <summary>
    /// 小於等於
    /// </summary>
    LessOrEqual,

    /// <summary>
    /// 含附號
    /// </summary>
    WithSubNumber,

    /// <summary>
    /// 僅附號
    /// </summary>
    SubNumberOnly,

    /// <summary>
    /// 附號以上
    /// </summary>
    SubNumberAbove,

    /// <summary>
    /// 附號以下
    /// </summary>
    SubNumberBelow
}

/// <summary>
/// 代表郵遞投遞規則
/// </summary>
public class PostalDeliveryRule
{
    /// <summary>
    /// 規則類型
    /// </summary>
    public PostalRuleType Type { get; set; }

    /// <summary>
    /// 起始號碼
    /// </summary>
    public int? StartNumber { get; set; }

    /// <summary>
    /// 結束號碼
    /// </summary>
    public int? EndNumber { get; set; }

    /// <summary>
    /// 指定號碼
    /// </summary>
    public int? SpecificNumber { get; set; }

    /// <summary>
    /// 指定附號
    /// </summary>
    public int? SpecificSubNumber { get; set; }

    /// <summary>
    /// 原始規則字串
    /// </summary>
    public string RawRule { get; set; } = string.Empty;

    /// <summary>
    /// 內部規則物件
    /// </summary>
    private DeliveryRuleMatcher? _internalRule;

    /// <summary>
    /// 從規則字串解析投遞規則
    /// </summary>
    /// <param name="fullRuleString">完整規則字串（如：「臺北市中正區三元街單147號以下」）</param>
    /// <returns>解析後的投遞規則</returns>
    /// <example>
    /// <code>
    /// var rule = PostalDeliveryRule.Parse("臺北市中正區三元街單147號以下");
    /// Console.WriteLine(rule.Type);          // LessOrEqual
    /// Console.WriteLine(rule.EndNumber);     // 147
    /// Console.WriteLine(rule.GetDescription()); // "單號，147號以下"
    /// </code>
    /// </example>
    public static PostalDeliveryRule Parse(string fullRuleString)
    {
        if (string.IsNullOrWhiteSpace(fullRuleString))
        {
            return new PostalDeliveryRule { RawRule = fullRuleString ?? string.Empty };
        }

        var rule = new DeliveryRuleMatcher(fullRuleString);
        var deliveryRule = new PostalDeliveryRule
        {
            RawRule = fullRuleString,
            _internalRule = rule
        };

        // 解析規則 tokens
        var ruleTokens = rule.RuleTokens;

        // 解析號碼
        var lastTokenNoPair = rule.Parse(-1);
        var secondLastNoPair = rule.Parse(-2);

        // 判斷規則類型 - 優先處理範圍，然後才是單雙號
        if (ruleTokens.Contains("至"))
        {
            deliveryRule.Type = PostalRuleType.Range;
            deliveryRule.StartNumber = secondLastNoPair.No;
            deliveryRule.EndNumber = lastTokenNoPair.No;
        }
        else if (ruleTokens.Contains("以上"))
        {
            // 可能同時有單雙號限制
            if (ruleTokens.Contains("單"))
                deliveryRule.Type = PostalRuleType.Odd;
            else if (ruleTokens.Contains("雙"))
                deliveryRule.Type = PostalRuleType.Even;
            else
                deliveryRule.Type = PostalRuleType.GreaterOrEqual;

            deliveryRule.StartNumber = lastTokenNoPair.No;
        }
        else if (ruleTokens.Contains("以下"))
        {
            // 可能同時有單雙號限制
            if (ruleTokens.Contains("單"))
                deliveryRule.Type = PostalRuleType.Odd;
            else if (ruleTokens.Contains("雙"))
                deliveryRule.Type = PostalRuleType.Even;
            else
                deliveryRule.Type = PostalRuleType.LessOrEqual;

            deliveryRule.EndNumber = lastTokenNoPair.No;
        }
        else if (ruleTokens.Contains("全"))
        {
            deliveryRule.Type = PostalRuleType.All;
        }
        else if (ruleTokens.Contains("單"))
        {
            deliveryRule.Type = PostalRuleType.Odd;
        }
        else if (ruleTokens.Contains("雙"))
        {
            deliveryRule.Type = PostalRuleType.Even;
        }
        else if (ruleTokens.Contains("含附號全") || ruleTokens.Contains("附號全"))
        {
            deliveryRule.Type = PostalRuleType.SubNumberOnly;
            // For "附號全", the number might be in the last or second-to-last token
            // because "附號全" gets replaced with "號"
            deliveryRule.SpecificNumber = lastTokenNoPair.No > 0 ? lastTokenNoPair.No : secondLastNoPair.No;
        }
        else if (ruleTokens.Contains("含附號"))
        {
            deliveryRule.Type = PostalRuleType.WithSubNumber;
            deliveryRule.SpecificNumber = lastTokenNoPair.No;
        }
        else if (ruleTokens.Contains("及以上附號"))
        {
            deliveryRule.Type = PostalRuleType.SubNumberAbove;
            deliveryRule.SpecificNumber = lastTokenNoPair.No;
            deliveryRule.SpecificSubNumber = lastTokenNoPair.SubNos.Count > 0 ? lastTokenNoPair.SubNos[0] : 0;
        }
        else if (ruleTokens.Contains("含附號以下"))
        {
            deliveryRule.Type = PostalRuleType.SubNumberBelow;
            deliveryRule.SpecificNumber = lastTokenNoPair.No;
            deliveryRule.SpecificSubNumber = lastTokenNoPair.SubNos.Count > 0 ? lastTokenNoPair.SubNos[0] : 0;
        }
        else if (ruleTokens.Count == 0 && lastTokenNoPair.No > 0)
        {
            // 沒有特殊規則，但有號碼，視為指定號碼
            deliveryRule.Type = PostalRuleType.Specific;
            deliveryRule.SpecificNumber = lastTokenNoPair.No;
            deliveryRule.SpecificSubNumber = lastTokenNoPair.SubNos.Count > 0 ? lastTokenNoPair.SubNos[0] : (int?)null;
        }

        return deliveryRule;
    }

    /// <summary>
    /// 檢查地址是否符合規則
    /// </summary>
    /// <param name="components">地址組件</param>
    /// <returns>是否符合規則</returns>
    /// <example>
    /// <code>
    /// var rule = PostalDeliveryRule.Parse("臺北市中正區三元街單147號以下");
    /// var addr = PostalAddress.Parse("臺北市中正區三元街145號");
    /// Console.WriteLine(rule.Matches(addr)); // true（145是單號且小於147）
    /// </code>
    /// </example>
    public bool Matches(PostalAddress components)
    {
        if (_internalRule == null)
            return false;

        // 使用內部規則進行匹配
        var addr = new AddressTokenizer(components.NormalizedAddress);
        return _internalRule.Match(addr);
    }

    /// <summary>
    /// 取得人類可讀的規則描述
    /// </summary>
    /// <returns>規則描述字串</returns>
    /// <example>
    /// <code>
    /// var rule1 = PostalDeliveryRule.Parse("臺北市中正區三元街單147號以下");
    /// Console.WriteLine(rule1.GetDescription()); // "單號，147號以下"
    ///
    /// var rule2 = PostalDeliveryRule.Parse("臺北市信義區市府路1號至45號");
    /// Console.WriteLine(rule2.GetDescription()); // "1號至45號"
    ///
    /// var rule3 = PostalDeliveryRule.Parse("臺北市大安區復興南路雙200號以上");
    /// Console.WriteLine(rule3.GetDescription()); // "雙號，200號以上"
    /// </code>
    /// </example>
    public string GetDescription()
    {
        var parts = new List<string>();

        // 添加單雙號描述 - 當類型是 Odd/Even 時總是添加
        if (Type == PostalRuleType.Odd)
        {
            parts.Add("單號");
            // 如果有範圍限制，也添加範圍描述
            if (StartNumber.HasValue)
                parts.Add($"{StartNumber}號以上");
            if (EndNumber.HasValue)
                parts.Add($"{EndNumber}號以下");
        }
        else if (Type == PostalRuleType.Even)
        {
            parts.Add("雙號");
            // 如果有範圍限制，也添加範圍描述
            if (StartNumber.HasValue)
                parts.Add($"{StartNumber}號以上");
            if (EndNumber.HasValue)
                parts.Add($"{EndNumber}號以下");
        }
        else if (Type == PostalRuleType.All)
        {
            parts.Add("全部門牌");
        }
        else
        {
            // 其他類型的範圍描述
            switch (Type)
            {
                case PostalRuleType.Range:
                    parts.Add($"{StartNumber}號至{EndNumber}號");
                    break;

                case PostalRuleType.GreaterOrEqual:
                    parts.Add($"{StartNumber}號以上");
                    break;

                case PostalRuleType.LessOrEqual:
                    parts.Add($"{EndNumber}號以下");
                    break;

                case PostalRuleType.Specific:
                    if (SpecificSubNumber.HasValue)
                        parts.Add($"{SpecificNumber}之{SpecificSubNumber}號");
                    else
                        parts.Add($"{SpecificNumber}號");
                    break;

                case PostalRuleType.WithSubNumber:
                    parts.Add($"{SpecificNumber}號含附號");
                    break;

                case PostalRuleType.SubNumberOnly:
                    parts.Add($"{SpecificNumber}號僅附號");
                    break;

                case PostalRuleType.SubNumberAbove:
                    if (SpecificSubNumber.HasValue)
                        parts.Add($"{SpecificNumber}之{SpecificSubNumber}號及以上附號");
                    else
                        parts.Add($"{SpecificNumber}號及以上附號");
                    break;

                case PostalRuleType.SubNumberBelow:
                    if (SpecificSubNumber.HasValue)
                        parts.Add($"{SpecificNumber}之{SpecificSubNumber}號含附號以下");
                    else
                        parts.Add($"{SpecificNumber}號含附號以下");
                    break;
            }
        }

        return parts.Count > 0 ? string.Join("，", parts) : "無特殊規則";
    }

    public override string ToString()
    {
        return $"PostalDeliveryRule({GetDescription()})";
    }
}
