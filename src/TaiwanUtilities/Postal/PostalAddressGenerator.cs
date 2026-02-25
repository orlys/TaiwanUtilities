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
/// 台灣郵政地址生成器
/// 從資料庫中隨機生成符合投遞規則的真實地址
/// </summary>
/// <example>
/// <code>
/// var generator = new PostalAddressGenerator();
/// var addresses = generator.Generate(10000);
///
/// foreach (var addr in addresses)
/// {
///     Console.WriteLine($"{addr.FullAddress} ({addr.ZipCode})");
/// }
/// </code>
/// </example>
public class PostalAddressGenerator
{
    private readonly Random _random = new Random();

    /// <summary>
    /// 生成指定數量的隨機地址
    /// </summary>
    /// <param name="count">要生成的地址數量</param>
    /// <param name="progressCallback">進度回調函數（已生成數量, 總數量）</param>
    /// <returns>生成的地址清單</returns>
    public List<GeneratedPostalAddress> Generate(int count, Action<int, int>? progressCallback = null)
    {
        var addresses = new List<GeneratedPostalAddress>();

        // 嘗試從 postal_rules 生成
        try
        {
            // 查詢所有規則（不限制數量），然後重複使用它們
            var rules = PostalDatabase.ExecuteQuery(repo => repo.QueryRandomPostalRules(100_000));

            if (rules.Count > 0)
            {
                addresses = GenerateFromPostalRules(rules, count, progressCallback);
            }
        }
        catch
        {
            // postal_rules 不存在或查詢失敗，使用後備方案
        }

        // 如果還需要更多地址，從 PostalDatabase 查詢補充
        if (addresses.Count < count)
        {
            var remaining = count - addresses.Count;
            var dbAddresses = GenerateFromDatabase(remaining, addresses.Count, progressCallback);
            addresses.AddRange(dbAddresses);
        }

        return addresses.Take(count).ToList();
    }

    #region 私有方法

    /// <summary>
    /// 從 postal_rules 生成地址
    /// </summary>
    private List<GeneratedPostalAddress> GenerateFromPostalRules(List<PostalRule> rules, int count, Action<int, int>? progressCallback = null)
    {
        var addresses = new List<GeneratedPostalAddress>();

        if (rules.Count == 0)
            return addresses;

        // 計算每個規則需要生成多少個地址（向上取整）
        var addressesPerRule = Math.Max(1, (count + rules.Count - 1) / rules.Count);

        // 隨機打亂規則順序
        var shuffledRules = rules.OrderBy(r => _random.Next()).ToList();

        foreach (var rule in shuffledRules)
        {
            if (addresses.Count >= count)
                break;

            // 從每個規則生成多個地址
            for (int i = 0; i < addressesPerRule && addresses.Count < count; i++)
            {
                try
                {
                    var address = GenerateAddressFromRule(rule);
                    if (address != null)
                    {
                        addresses.Add(address);

                        // 每 10,000 筆輸出一次進度
                        if (progressCallback != null && addresses.Count % 10_000 == 0)
                        {
                            progressCallback(addresses.Count, count);
                        }
                    }
                }
                catch
                {
                    // 忽略生成失敗的地址
                }
            }
        }

        return addresses;
    }

    /// <summary>
    /// 從單一規則生成地址（使用資料庫欄位，不進行字串解析）
    /// </summary>
    private GeneratedPostalAddress? GenerateAddressFromRule(PostalRule rule)
    {
        try
        {
            var addressParts = new List<string>
            {
                rule.City,
                rule.Area,
                rule.Road
            };

            // 生成巷號
            var lane = GenerateNumberInRange(rule.LaneStart, rule.LaneEnd);
            if (lane.HasValue)
            {
                addressParts.Add($"{lane}巷");
            }

            // 生成弄號
            var alley = GenerateNumberInRange(rule.AlleyStart, rule.AlleyEnd);
            if (alley.HasValue)
            {
                addressParts.Add($"{alley}弄");
            }

            // 生成門牌號碼（考慮單雙號規則）
            var number = GenerateNumberInRange(rule.NumberStart, rule.NumberEnd, rule.EvenOdd);
            if (number.HasValue)
            {
                addressParts.Add($"{number}號");

                // 生成附號
                var subNumber = GenerateNumberInRange(rule.NumberStartSub, rule.NumberEndSub);
                if (subNumber.HasValue && subNumber.Value > 0)
                {
                    addressParts[addressParts.Count - 1] = $"{number}之{subNumber}號";
                }
            }
            else
            {
                addressParts.Add("1號"); // 預設門牌
            }

            var fullAddress = string.Join("", addressParts);

            // 直接建立 PostalAddress 物件，不使用字串解析
            var postalAddress = new PostalAddress
            {
                City = rule.City,
                District = rule.Area,
                Road = rule.Road,
                Lane = lane.HasValue ? lane.ToString() : null,
                Alley = alley.HasValue ? alley.ToString() : null,
                Number = number,
                SubNumbers = rule.NumberStartSub.HasValue && rule.NumberStartSub.Value > 0
                    ? new List<int> { GenerateNumberInRange(rule.NumberStartSub, rule.NumberEndSub) ?? 0 }
                    : null,
                RawAddress = fullAddress,
                NormalizedAddress = fullAddress
            };

            // 驗證生成的地址是否有效
            var result = ZipCode.Find(fullAddress);
            if (result.ZipCode != rule.ZipCode)
            {
                // 生成的地址郵遞區號不符，返回 null
                return null;
            }

            return new GeneratedPostalAddress
            {
                Address = postalAddress,
                FullAddress = fullAddress,
                ZipCode = rule.ZipCode,
                Rule = rule,
                Source = PostalGenerationSource.PostalRules
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 從資料庫查詢生成地址（後備方案）
    /// </summary>
    private List<GeneratedPostalAddress> GenerateFromDatabase(int count, int offset = 0, Action<int, int>? progressCallback = null)
    {
        var addresses = new List<GeneratedPostalAddress>();

        try
        {
            // 計算需要多少個查詢（每個查詢預計生成若干地址）
            var addressesPerQuery = 100; // 每個查詢生成 100 個地址
            var queriesNeeded = Math.Max((count + addressesPerQuery - 1) / addressesPerQuery, 1);

            var queries = GenerateSearchQueries(queriesNeeded);

            foreach (var query in queries)
            {
                if (addresses.Count >= count)
                    break;

                var result = ZipCode.Find(query);
                if (!string.IsNullOrEmpty(result.ZipCode))
                {
                    // 取得該地址的所有投遞規則
                    var rules = ZipCode.GetDeliveryRules(query);

                    if (rules.Count > 0)
                    {
                        // 使用投遞規則生成地址
                        var numbersToGenerate = Math.Min(addressesPerQuery, count - addresses.Count);

                        for (int i = 0; i < numbersToGenerate; i++)
                        {
                            // 從規則中隨機選一條
                            var deliveryRule = rules[_random.Next(rules.Count)];

                            // 根據規則範圍生成門牌號碼
                            int? number = null;
                            if (deliveryRule.Rule.StartNumber.HasValue && deliveryRule.Rule.EndNumber.HasValue)
                            {
                                // 如果有範圍，從範圍中隨機選擇
                                var evenOdd = deliveryRule.Rule.Type == PostalRuleType.Odd ? 1 :
                                            deliveryRule.Rule.Type == PostalRuleType.Even ? 2 : 0;
                                number = GenerateNumberInRange(deliveryRule.Rule.StartNumber,
                                                             deliveryRule.Rule.EndNumber, evenOdd);
                            }
                            else if (deliveryRule.Rule.SpecificNumber.HasValue)
                            {
                                // 使用指定號碼
                                number = deliveryRule.Rule.SpecificNumber;
                            }

                            if (!number.HasValue)
                            {
                                number = 1; // 如果無法從規則生成，使用預設值
                            }

                            // 從原始規則字串中提取地址前綴（移除規則部分）
                            var addressPrefix = ExtractAddressPrefix(deliveryRule.Rule.RawRule);
                            var testAddress = addressPrefix + number + "號";
                            var postalAddress = PostalAddress.Parse(testAddress);
                            var fullAddress = BuildFullAddress(postalAddress);

                            addresses.Add(new GeneratedPostalAddress
                            {
                                Address = postalAddress,
                                FullAddress = fullAddress,
                                ZipCode = deliveryRule.ZipCode,
                                Rule = null,
                                Source = PostalGenerationSource.Database
                            });

                            // 每 10,000 筆輸出一次進度
                            var currentTotal = offset + addresses.Count;
                            if (progressCallback != null && currentTotal % 10_000 == 0)
                            {
                                progressCallback(currentTotal, offset + count);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略錯誤
        }

        return addresses;
    }

    /// <summary>
    /// 從完整規則字串中提取地址前綴（移除規則部分）
    /// </summary>
    /// <param name="ruleString">完整規則字串（如：「臺北市中正區三元街單147號以下」）</param>
    /// <returns>地址前綴（如：「臺北市中正區三元街」）</returns>
    private string ExtractAddressPrefix(string ruleString)
    {
        if (string.IsNullOrEmpty(ruleString))
            return string.Empty;

        // 移除常見的規則關鍵字
        var ruleKeywords = new[]
        {
            "全", "單", "雙",
            "號以下", "號以上", "號",
            "巷以下", "巷以上", "巷",
            "弄以下", "弄以上", "弄",
            "含附號全", "附號全", "含附號以下", "含附號",
            "及以上附號", "以下", "以上", "至"
        };

        var result = ruleString;

        // 先移除所有數字相關的規則（如：「147號以下」）
        // 匹配模式：數字 + 規則關鍵字
        var digitPattern = new System.Text.RegularExpressions.Regex(@"\d+[-之\d]*(?:號|巷|弄)?(?:以下|以上|至)?");
        result = digitPattern.Replace(result, "");

        // 再移除剩餘的規則關鍵字
        foreach (var keyword in ruleKeywords)
        {
            result = result.Replace(keyword, "");
        }

        // 移除尾部的「至」字（如：「100至」）
        result = result.TrimEnd('至', '-', '之');

        return result.Trim();
    }

    /// <summary>
    /// 生成搜尋查詢字串
    /// </summary>
    private List<string> GenerateSearchQueries(int count)
    {
        var queries = new List<string>();

        // 從常見縣市中隨機選擇
        var commonCities = new[]
        {
            "臺北市", "新北市", "桃園市", "臺中市", "臺南市", "高雄市",
            "基隆市", "新竹市", "嘉義市"
        };

        // 隨機選擇足夠數量的縣市（可重複）
        while (queries.Count < count)
        {
            var city = commonCities[_random.Next(commonCities.Length)];
            queries.Add(city);
        }

        return queries;
    }

    /// <summary>
    /// 在範圍內生成隨機數字（考慮單雙號規則）
    /// </summary>
    private int? GenerateNumberInRange(int? start, int? end, int? evenOdd = null)
    {
        if (!start.HasValue)
            return null;

        var max = end ?? start;
        if (max < start) max = start.Value;

        // 限制範圍避免生成過大的數字
        if (max - start.Value > 1000)
            max = start.Value + 1000;

        // 如果有單雙號規則
        if (evenOdd.HasValue && evenOdd.Value != 0)
        {
            var isEven = evenOdd.Value == 2;
            var candidates = new List<int>();

            for (int i = start.Value; i <= max && candidates.Count < 100; i++)
            {
                if (isEven && i % 2 == 0)
                    candidates.Add(i);
                else if (!isEven && i % 2 == 1)
                    candidates.Add(i);
            }

            if (candidates.Count > 0)
                return candidates[_random.Next(candidates.Count)];

            return null;
        }

        return _random.Next(start.Value, max.Value + 1);
    }

    /// <summary>
    /// 建立完整地址字串
    /// </summary>
    private string BuildFullAddress(PostalAddress address)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(address.City))
            parts.Add(address.City!);

        if (!string.IsNullOrEmpty(address.District))
            parts.Add(address.District!);

        if (!string.IsNullOrEmpty(address.Road))
            parts.Add(address.Road!);

        if (!string.IsNullOrEmpty(address.Lane))
            parts.Add($"{address.Lane}巷");

        if (!string.IsNullOrEmpty(address.Alley))
            parts.Add($"{address.Alley}弄");

        if (address.Number.HasValue)
        {
            if (address.SubNumbers != null && address.SubNumbers.Count > 0)
                parts.Add($"{address.Number}之{address.SubNumbers[0]}號");
            else
                parts.Add($"{address.Number}號");
        }

        if (!string.IsNullOrEmpty(address.Floor))
            parts.Add($"{address.Floor}樓");

        return string.Join("", parts);
    }

    #endregion
}

/// <summary>
/// 地址生成來源
/// </summary>
public enum PostalGenerationSource
{
    /// <summary>
    /// 從 postal_rules 資料表生成（結構化規則）
    /// </summary>
    PostalRules,

    /// <summary>
    /// 從資料庫查詢生成（後備方案）
    /// </summary>
    Database
}

/// <summary>
/// 生成的郵政地址
/// </summary>
public class GeneratedPostalAddress
{
    /// <summary>
    /// 解析後的地址物件
    /// </summary>
    public PostalAddress Address { get; set; } = null!;

    /// <summary>
    /// 完整地址字串
    /// </summary>
    public string FullAddress { get; set; } = null!;

    /// <summary>
    /// 郵遞區號
    /// </summary>
    public string ZipCode { get; set; } = null!;

    /// <summary>
    /// 匹配的投遞規則（如果有）
    /// </summary>
    public PostalRule? Rule { get; set; }

    /// <summary>
    /// 生成來源
    /// </summary>
    public PostalGenerationSource Source { get; set; }

    /// <summary>
    /// 驗證地址是否正確（使用 ZipCode.Find 查詢並比對）
    /// </summary>
    public bool Validate()
    {
        var result = TaiwanUtilities.ZipCode.Find(FullAddress);
        return result.ZipCode == this.ZipCode;
    }

    /// <summary>
    /// 取得詳細的驗證結果
    /// </summary>
    public PostalAddressValidation GetValidation()
    {
        return PostalAddress.Validate(Address);
    }

    /// <summary>
    /// 返回地址的字串表示
    /// </summary>
    public override string ToString()
    {
        return $"{FullAddress} ({ZipCode})";
    }
}
