// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// Peak - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// 郵遞區號投遞規則的匹配器
/// 用於解析和匹配郵遞規則字串（如：單全、雙全、以上、以下、至等）
/// </summary>
internal class DeliveryRuleMatcher : AddressTokenizer
{
    /// <summary>
    /// 規則 token 集合（如：全、單、雙、以上、以下等）
    /// </summary>
    internal HashSet<string> RuleTokens { get; private set; }

    /// <summary>
    /// 規則 token 正規表達式
    /// </summary>
    private static readonly Regex RuleTokenRegex = new Regex(
        @"及以上附號|含附號以下|含附號全|含附號|以下|以上|附號全|[連至單雙全](?=[\d全]|$)",
        RegexOptions.Compiled);

    /// <summary>
    /// 從規則字串中提取規則 token 和地址部分
    /// </summary>
    internal static (HashSet<string> RuleTokens, string AddrStr) Part(string ruleStr)
    {
        var normalized = Normalize(ruleStr);
        var ruleTokens = new HashSet<string>();

        var addrStr = RuleTokenRegex.Replace(normalized, m =>
        {
            var token = m.Value;
            var retval = string.Empty;

            if (token == "連")
            {
                token = string.Empty;
            }
            else if (token == "附號全")
            {
                retval = "號";
            }

            if (!string.IsNullOrEmpty(token))
                ruleTokens.Add(token);

            return retval;
        });

        return (ruleTokens, addrStr);
    }

    internal DeliveryRuleMatcher(string ruleStr) : base(string.Empty)
    {
        (RuleTokens, var addrStr) = Part(ruleStr);
        Tokens = Tokenize(addrStr);
    }

    /// <summary>
    /// 比較兩個號碼對（包含主號和附號）
    /// </summary>
    /// <returns>負數表示 a &lt; b，0 表示 a == b，正數表示 a &gt; b</returns>
    private static int CompareNoPairs((int No, List<int> SubNos) a, (int No, List<int> SubNos) b)
    {
        // 先比較主號碼
        if (a.No != b.No)
            return a.No.CompareTo(b.No);

        // 主號碼相同，比較第一層附號
        var aSubNo = a.SubNos.Count > 0 ? a.SubNos[0] : 0;
        var bSubNo = b.SubNos.Count > 0 ? b.SubNos[0] : 0;
        return aSubNo.CompareTo(bSubNo);
    }

    /// <summary>
    /// 判斷給定的地址是否符合此規則
    /// </summary>
    internal bool Match(AddressTokenizer addr)
    {
        // 計算需要精確匹配的 token 位置
        var myLastPos = Tokens.Count - 1;
        if (RuleTokens.Count > 0 && !RuleTokens.Contains("全"))
            myLastPos--;
        if (RuleTokens.Contains("至"))
            myLastPos--;

        // 檢查 token 數量是否足夠
        if (myLastPos >= addr.Tokens.Count)
            return false;

        // 檢查前面的 tokens 是否完全匹配
        for (var i = myLastPos; i >= 0; i--)
        {
            if (!TokensEqual(Tokens[i], addr.Tokens[i]))
                return false;
        }

        // 檢查規則 tokens
        var hisNoPair = addr.Parse(myLastPos + 1);
        if (RuleTokens.Count > 0 && hisNoPair.No == 0 && hisNoPair.SubNos.Count == 0)
            return false;

        var myNoPair = Parse(-1);
        var myAsstNoPair = Parse(-2);

        foreach (var rt in RuleTokens)
        {
            switch (rt)
            {
                case "單" when (hisNoPair.No & 1) != 1:
                case "雙" when (hisNoPair.No & 1) != 0:
                case "以上" when CompareNoPairs(hisNoPair, myNoPair) < 0:
                case "以下" when CompareNoPairs(hisNoPair, myNoPair) > 0:
                    return false;

                case "至":
                    {
                        var inRange = CompareNoPairs(myAsstNoPair, hisNoPair) <= 0 &&
                                      CompareNoPairs(hisNoPair, myNoPair) <= 0;
                        var hasContainSubno = RuleTokens.Contains("含附號全") &&
                                              hisNoPair.No == myNoPair.No;
                        if (!inRange && !hasContainSubno)
                            return false;
                        break;
                    }

                case "含附號" when hisNoPair.No != myNoPair.No:
                case "附號全" when !(hisNoPair.No == myNoPair.No && hisNoPair.SubNos.Count > 0):
                case "及以上附號" when CompareNoPairs(hisNoPair, myNoPair) < 0:
                    return false;

                case "含附號以下":
                    {
                        var isBelow = CompareNoPairs(hisNoPair, myNoPair) <= 0;
                        var isSameNoWithSubno = hisNoPair.No == myNoPair.No;
                        if (!isBelow && !isSameNoWithSubno)
                            return false;
                        break;
                    }
            }
        }

        return true;
    }

    /// <summary>
    /// 比較兩個 token 是否相等
    /// </summary>
    private static bool TokensEqual(string[] t1, string[] t2)
    {
        return t1[NO] == t2[NO] &&
               t1[SUBNO] == t2[SUBNO] &&
               t1[NAME] == t2[NAME] &&
               t1[UNIT] == t2[UNIT];
    }

    public override string ToString()
    {
        return $"DeliveryRuleMatcher('{Flat()}{string.Join("", RuleTokens)}')";
    }
}
