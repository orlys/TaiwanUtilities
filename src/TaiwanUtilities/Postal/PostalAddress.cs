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
/// 代表結構化的台灣郵政地址
/// </summary>
public class PostalAddress
{
    /// <summary>
    /// 縣市（如：臺北市）
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// 行政區（如：信義區）
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// 村里
    /// </summary>
    public string? Village { get; set; }

    /// <summary>
    /// 鄰
    /// </summary>
    public string? Neighborhood { get; set; }

    /// <summary>
    /// 路街（如：市府路）
    /// </summary>
    public string? Road { get; set; }

    /// <summary>
    /// 段
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// 巷
    /// </summary>
    public string? Lane { get; set; }

    /// <summary>
    /// 弄
    /// </summary>
    public string? Alley { get; set; }

    /// <summary>
    /// 衖（弄的下級，如：48衖）
    /// </summary>
    public string? SubAlley { get; set; }

    /// <summary>
    /// 門牌號碼
    /// </summary>
    public int? Number { get; set; }

    /// <summary>
    /// 附號（支援多層如：150之1之1之1 → [1, 1, 1]）
    /// </summary>
    public List<int>? SubNumbers { get; set; }

    /// <summary>
    /// 是否為臨時門牌（如：臨11號）
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 是否為地下樓層（如：地下一層）
    /// </summary>
    public bool IsBasement { get; set; }

    /// <summary>
    /// 樓
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// 樓層附號（如：5樓之3 中的 3）
    /// </summary>
    public int? SubFloor { get; set; }

    /// <summary>
    /// 室（如：101室）
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// 原始地址
    /// </summary>
    public string RawAddress { get; set; } = string.Empty;

    /// <summary>
    /// 正規化地址
    /// </summary>
    public string NormalizedAddress { get; set; } = string.Empty;

    /// <summary>
    /// 地區名稱（部落、眷村、聚落等）
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// 從地址字串解析組件
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <returns>解析後的地址組件</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1之2號3樓");
    /// Console.WriteLine(comp.City);        // 臺北市
    /// Console.WriteLine(comp.District);    // 信義區
    /// Console.WriteLine(comp.Road);        // 市府路
    /// Console.WriteLine(comp.Number);      // 1
    /// Console.WriteLine(comp.SubNumbers?[0]);   // 2
    /// Console.WriteLine(comp.Floor);       // 3樓
    ///
    /// // 支援樓層附號（室號）格式
    /// var comp2 = PostalAddress.Parse("臺北市信義區市府路1號5樓之3室");
    /// Console.WriteLine(comp2.Floor);      // 5樓
    /// Console.WriteLine(comp2.SubFloor);   // 3
    /// Console.WriteLine(comp2.Room);       // 3室
    ///
    /// // 也支援無「室」字的格式
    /// var comp3 = PostalAddress.Parse("臺北市信義區市府路1號5樓之3");
    /// Console.WriteLine(comp3.Floor);      // 5樓
    /// Console.WriteLine(comp3.SubFloor);   // 3
    ///
    /// // 支援衖（弄的下級）
    /// var comp4 = PostalAddress.Parse("桃園市中壢區龍岡路三段243巷53弄48衖15號");
    /// Console.WriteLine(comp4.Section);    // 3段
    /// Console.WriteLine(comp4.Lane);       // 243巷
    /// Console.WriteLine(comp4.Alley);      // 53弄
    /// Console.WriteLine(comp4.SubAlley);   // 48衖
    /// Console.WriteLine(comp4.Number);     // 15
    ///
    /// // 支援地下樓層
    /// var comp5 = PostalAddress.Parse("臺灣大道一段７０３號地下一層");
    /// Console.WriteLine(comp5.Road);       // 臺灣大道
    /// Console.WriteLine(comp5.Section);    // 1段
    /// Console.WriteLine(comp5.Number);     // 703
    /// Console.WriteLine(comp5.Floor);      // 地下1層
    ///
    /// // 支援臨時門牌
    /// var comp6 = PostalAddress.Parse("彰化縣彰化市民族一街臨11號");
    /// Console.WriteLine(comp6.City);       // 彰化縣
    /// Console.WriteLine(comp6.District);   // 彰化市
    /// Console.WriteLine(comp6.Road);       // 民族1街
    /// Console.WriteLine(comp6.Number);     // 11
    /// Console.WriteLine(comp6.IsTemporary); // True
    ///
    /// // 支援專用道路（XX線：專用公路、林道等）
    /// var comp7 = PostalAddress.Parse("宜蘭縣大同鄉太平村宜專1線中間1號");
    /// Console.WriteLine(comp7.City);       // 宜蘭縣
    /// Console.WriteLine(comp7.District);   // 大同鄉
    /// Console.WriteLine(comp7.Village);    // 太平村
    /// Console.WriteLine(comp7.Road);       // 宜專1線（專用公路）
    /// Console.WriteLine(comp7.Number);     // 1
    /// </code>
    /// </example>
    public static PostalAddress Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new PostalAddress { RawAddress = address ?? string.Empty };
        }

        var addr = new AddressTokenizer(address);
        var components = new PostalAddress
        {
            RawAddress = address,
            NormalizedAddress = addr.Flat()
        };

        // 使用啟發式規則提取各組件
        var tokens = addr.Tokens;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var no = token[AddressTokenizer.NO];
            var subno = token[AddressTokenizer.SUBNO];
            var name = token[AddressTokenizer.NAME];
            var unit = token[AddressTokenizer.UNIT];

            // 解析門牌號碼和附號
            if (!string.IsNullOrEmpty(no) && unit == "號")
            {
                if (int.TryParse(no, out var number))
                    components.Number = number;

                // 檢查前一個 token 是否為「臨」（臨時門牌）
                // Token 結構：[..., ["", "", "臨", ""], ["11", "", "", "號"]]
                if (i >= 1 && tokens[i - 1][AddressTokenizer.NAME] == "臨")
                {
                    components.IsTemporary = true;
                }

                // 解析所有附號（支援多層：之1之1之1）
                if (!string.IsNullOrEmpty(subno))
                {
                    var subNumbers = new List<int>();
                    var parts = subno.Split(new[] { '之' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (int.TryParse(part, out var sn))
                            subNumbers.Add(sn);
                    }

                    if (subNumbers.Count > 0)
                    {
                        components.SubNumbers = subNumbers;
                    }
                }
            }

            // 解析樓層
            if (!string.IsNullOrEmpty(no) && unit == "樓")
            {
                components.Floor = no + "樓";

                // 檢查下一個 token 是否為「之N」格式（如：5樓之3）
                // Token 結構：[..., ["5", "", "", "樓"], ["", "", "之3", ""]]
                if (i + 1 < tokens.Count)
                {
                    var nextToken = tokens[i + 1];
                    var nextName = nextToken[AddressTokenizer.NAME];

                    if (!string.IsNullOrEmpty(nextName) && nextName.StartsWith("之"))
                    {
                        // 提取「之」後面的數字
                        var subFloorStr = nextName.Substring(1);
                        if (int.TryParse(subFloorStr, out var subFloor))
                        {
                            components.SubFloor = subFloor;
                        }
                    }
                }
            }

            // 解析層（地下樓層）
            if (!string.IsNullOrEmpty(no) && unit == "層")
            {
                // 檢查前一個 token 是否為「地下」
                if (i >= 1 && tokens[i - 1][AddressTokenizer.NAME] == "地下")
                {
                    components.Floor = "地下" + no + "層";
                    components.IsBasement = true;  // 設定地下樓層標記
                }
                else
                {
                    components.Floor = no + "層";
                }
            }

            // 解析室號（處理「5樓之3室」格式）
            if (!string.IsNullOrEmpty(no) && unit == "室")
            {
                components.Room = no + "室";

                // 檢查是否為「樓之室」格式（如：5樓之3室）
                // Token 結構：[..., ["5", "", "", "樓"], ["", "", "之", ""], ["3", "", "", "室"]]
                if (i >= 2 &&
                    tokens[i - 1][AddressTokenizer.NAME] == "之" &&
                    tokens[i - 2][AddressTokenizer.UNIT] == "樓")
                {
                    // 這是樓層附號（室號），設定 SubFloor
                    if (int.TryParse(no, out var subFloor))
                    {
                        components.SubFloor = subFloor;
                    }
                }
            }

            // 解析縣市
            if (unit == "縣" || (unit == "市" && string.IsNullOrEmpty(components.City)))
            {
                components.City = name + unit;
            }
            // 解析特殊行政區（unit 為空但 name 是城市名稱）
            else if (string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(name) &&
                     string.IsNullOrEmpty(components.City) && i == 0)
            {
                // 第一個 token 且 unit 為空，代表是特殊行政區
                components.City = name;
            }

            // 解析區鄉鎮市（市可能是縣下的市，如竹北市）
            if (unit is "區" or "鄉" or "鎮" || (unit == "市" && !string.IsNullOrEmpty(components.City) && components.City!.EndsWith("縣")))
            {
                components.District = name + unit;
            }

            // 解析村里
            if (unit is "村" or "里")
            {
                if (string.IsNullOrEmpty(components.Village))
                {
                    components.Village = name + unit;
                }
            }

            // 解析鄰（支援 "30鄰" 和 "第30鄰" 兩種格式）
            if (unit == "鄰")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    // 格式：30鄰（NO 有值）
                    components.Neighborhood = no + "鄰";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // 格式：第30鄰（NAME 為 "第30" 或 "30"）
                    // 提取數字部分
                    var neighborhoodNum = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
                    if (neighborhoodNum.Success)
                    {
                        components.Neighborhood = neighborhoodNum.Value + "鄰";
                    }
                }
            }

            // 解析路街（需要在段之前設置）
            if (unit is "路" or "街")
            {
                components.Road = name + unit;
            }

            // 解析線（專用道路，如：宜專1線、台7-2線）
            // 包含：專用公路、林道、公路支線等
            if (unit == "線")
            {
                // 處理支線編號（如：1-1線、7-2線）
                // Token 中的 SUBNO 是 "-1" 被正規化為 "之1" 後的結果
                // 需要將 "之" 轉回 "-" 以符合專用道路命名慣例
                var branchNumber = "";
                if (!string.IsNullOrEmpty(subno))
                {
                    // 將 "之1" 轉換為 "-1"
                    branchNumber = subno.Replace("之", "-");
                }

                // 檢查前一個 token 是否有專用道路名稱前綴
                // Token 結構：[..., ["", "", "宜專", ""], ["1", "之1", "", "線"]]
                if (i >= 1 && !string.IsNullOrEmpty(tokens[i - 1][AddressTokenizer.NAME]) &&
                    string.IsNullOrEmpty(tokens[i - 1][AddressTokenizer.UNIT]))
                {
                    var roadPrefix = tokens[i - 1][AddressTokenizer.NAME];
                    components.Road = roadPrefix + no + branchNumber + unit;  // 如：宜專1-1線（專用公路）
                }
                else if (!string.IsNullOrEmpty(no))
                {
                    components.Road = no + branchNumber + unit;  // 如：7-2線（公路支線）
                }
            }

            // 解析段
            if (unit == "段")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.Section = no + "段";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // 處理「三段」正規化為「3段」後，被解析為 NAME=3, UNIT=段 的情況
                    // 也處理「臺灣大道二段」→「臺灣大道2段」被解析為 NAME=臺灣大道2, UNIT=段 的情況

                    // 嘗試從 NAME 末尾提取數字（段號）
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"^(.+?)(\d+)$");
                    if (match.Success)
                    {
                        // NAME 包含路名和段號，如「臺灣大道2」
                        var roadName = match.Groups[1].Value;      // 「臺灣大道」
                        var sectionNo = match.Groups[2].Value;     // 「2」

                        // 設定 Road（如果尚未設定）
                        if (string.IsNullOrEmpty(components.Road))
                        {
                            components.Road = roadName;
                        }

                        // 設定 Section
                        components.Section = sectionNo + "段";
                    }
                    else
                    {
                        // NAME 只包含數字，如「3」
                        components.Section = name + "段";
                    }
                }
            }

            // 解析巷
            if (unit == "巷")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    // 一般巷號：如"15巷"
                    components.Lane = no + "巷";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // 路名以巷結尾：如"一村巷"、"光復巷"
                    // 這些是路名，不是巷號
                    if (string.IsNullOrEmpty(components.Road))
                    {
                        components.Road = name + unit;
                    }
                }
            }

            // 解析弄
            if (unit == "弄")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    // 一般弄號：如"3弄"
                    components.Alley = no + "弄";
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // 路名以弄結尾：如"光復弄"
                    // 這些是路名，不是弄號
                    if (string.IsNullOrEmpty(components.Road))
                    {
                        components.Road = name + unit;
                    }
                }
            }

            // 解析衖（弄的下級）
            if (unit == "衖")
            {
                if (!string.IsNullOrEmpty(no))
                {
                    components.SubAlley = no + "衖";
                }
            }

            // ✨ 識別無單位字的地名（可能是眷村、老聚落等）
            if (string.IsNullOrEmpty(unit) && !string.IsNullOrEmpty(name))
            {
                // 排除已知的前綴詞和連接詞
                var knownPrefixes = new[] { "臨", "地下", "之" };

                // 檢查下一個 token 是否為「線」單位（專用道路前綴）
                var isRoadLinePrefix = i + 1 < tokens.Count &&
                                      tokens[i + 1][AddressTokenizer.UNIT] == "線";

                // 如果已經有縣市和區，且這個 token 在號碼之前
                // 且不是已知的前綴詞或專用道路前綴，很可能是歷史地名（眷村、老聚落、部落等）
                if (!string.IsNullOrEmpty(components.City) &&
                    !string.IsNullOrEmpty(components.District) &&
                    i < tokens.Count - 1 && // 不是最後一個 token
                    !knownPrefixes.Contains(name) && // 不是已知的前綴詞
                    !isRoadLinePrefix) // 不是專用道路前綴
                {
                    components.Locality = name;
                }
            }
        }

        return components;
    }

    /// <summary>
    /// 嘗試從地址字串解析組件
    /// </summary>
    /// <param name="address">台灣地址字串</param>
    /// <param name="result">解析後的地址組件，若失敗則為 null</param>
    /// <returns>解析是否成功（地址為空或空白時返回 false）</returns>
    /// <example>
    /// <code>
    /// if (PostalAddress.TryParse("臺北市信義區市府路1號", out var comp))
    /// {
    ///     Console.WriteLine($"解析成功: {comp?.City}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("解析失敗：地址為空");
    /// }
    /// </code>
    /// </example>
    public static bool TryParse(string? address, out PostalAddress? result)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            result = null;
            return false;
        }

        try
        {
            result = Parse(address!);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }


    /// <summary>
    /// 取得完整門牌號碼（含所有附號）
    /// </summary>
    /// <returns>完整門牌號碼字串，如「1之2號」或「150之1之1之1號」</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1之2號");
    /// Console.WriteLine(comp.GetFullNumber()); // "1之2號"
    ///
    /// var comp2 = PostalAddress.Parse("台中市中區平等街150之1之1之1號");
    /// Console.WriteLine(comp2.GetFullNumber()); // "150之1之1之1號"
    /// </code>
    /// </example>
    public string GetFullNumber()
    {
        if (!Number.HasValue)
            return string.Empty;

        if (SubNumbers != null && SubNumbers.Count > 0)
        {
            var subNoStr = string.Join("之", SubNumbers);
            return $"{Number}之{subNoStr}號";
        }

        return $"{Number}號";
    }

    /// <summary>
    /// 取得基本地址（縣市+區+路/村里/歷史地名）
    /// </summary>
    /// <returns>基本地址字串</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1號");
    /// Console.WriteLine(comp.GetBaseAddress()); // "臺北市信義區市府路"
    ///
    /// var comp2 = PostalAddress.Parse("高雄市阿蓮區再興23號");
    /// Console.WriteLine(comp2.GetBaseAddress()); // "高雄市阿蓮區再興"
    /// </code>
    /// </example>
    public string GetBaseAddress()
    {
        var parts = new List<string?>
        {
            City,
            District,
            Village ?? Road ?? Locality
        };
        return string.Concat(parts.Where(p => !string.IsNullOrEmpty(p)));
    }

    /// <summary>
    /// 驗證組件有效性
    /// </summary>
    /// <param name="address">PostalAddress 物件</param>
    /// <returns>驗證結果</returns>
    /// <example>
    /// <code>
    /// var comp = PostalAddress.Parse("臺北市信義區市府路1號");
    /// var validation = PostalAddress.Validate(comp);
    /// Console.WriteLine(validation.IsValidCity);      // true
    /// Console.WriteLine(validation.IsValidDistrict);  // true
    /// </code>
    /// </example>
    public static PostalAddressValidation Validate(PostalAddress address)
    {
        var validation = new PostalAddressValidation();

        if (address == null)
        {
            validation.Messages.Add("地址物件為空");
            return validation;
        }

        // 如果沒有縣市或行政區，無法驗證
        if (string.IsNullOrEmpty(address.City) && string.IsNullOrEmpty(address.District))
        {
            validation.Messages.Add("地址缺少縣市或行政區資訊");
            return validation;
        }

        try
        {
            // 驗證縣市
            if (!string.IsNullOrEmpty(address.City))
            {
                var cityResult = ZipCode.Find(address.City!).ZipCode;
                validation.IsValidCity = !string.IsNullOrEmpty(cityResult);
                if (!validation.IsValidCity)
                {
                    validation.Messages.Add($"縣市「{address.City}」不存在");
                }
            }

            // 驗證縣市+行政區
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrEmpty(address.District))
            {
                var districtResult = ZipCode.Find(address.City! + address.District!).ZipCode;
                validation.IsValidDistrict = !string.IsNullOrEmpty(districtResult);
                if (!validation.IsValidDistrict)
                {
                    validation.Messages.Add($"行政區「{address.District}」在「{address.City}」中不存在");
                }
            }

            // 驗證路街
            if (!string.IsNullOrEmpty(address.City) && !string.IsNullOrEmpty(address.District) && !string.IsNullOrEmpty(address.Road))
            {
                var roadResult = ZipCode.Find(address.City! + address.District! + address.Road!).ZipCode;
                validation.IsValidRoad = !string.IsNullOrEmpty(roadResult);
                if (!validation.IsValidRoad)
                {
                    validation.Messages.Add($"路街「{address.Road}」在「{address.City}{address.District}」中不存在");
                }
                else if (address.Number.HasValue)
                {
                    // 如果有門牌號碼，使用結構化驗證檢查範圍
                    var structuredValidation = ValidateWithStructuredFields(address);
                    if (structuredValidation != null)
                    {
                        // 合併結構化驗證結果
                        validation.IsValidNumber = structuredValidation.IsValidNumber;
                        validation.IsValidLane = structuredValidation.IsValidLane;
                        validation.IsValidAlley = structuredValidation.IsValidAlley;
                        validation.MatchedRule = structuredValidation.MatchedRule;

                        if (!structuredValidation.IsValidNumber ||
                            !structuredValidation.IsValidLane ||
                            !structuredValidation.IsValidAlley)
                        {
                            validation.Messages.AddRange(structuredValidation.Messages);
                        }
                        else
                        {
                            validation.Messages.Add($"門牌號碼驗證通過（郵遞區號：{structuredValidation.MatchedRule?.ZipCode}）");
                        }
                    }
                }
            }

            // ✨ 驗證地區名稱（部落、眷村、聚落等）
            if (!string.IsNullOrEmpty(address.Locality))
            {
                var locationResult = ZipCode.Find(address.City! + address.District! + address.Locality!).ZipCode;
                validation.IsValidLocality = !string.IsNullOrEmpty(locationResult);

                if (validation.IsValidLocality)
                {
                    validation.Messages.Add($"「{address.Locality}」在資料庫中存在（可能是眷村、老聚落等歷史地名）");
                }
                else
                {
                    validation.Messages.Add($"「{address.Locality}」在資料庫中不存在");
                }
            }
        }
        catch (Exception ex)
        {
            validation.Messages.Add($"驗證時發生錯誤：{ex.Message}");
        }

        return validation;
    }

    /// <summary>
    /// 使用結構化欄位驗證地址（私有方法）
    /// </summary>
    private static PostalAddressValidation? ValidateWithStructuredFields(PostalAddress address)
    {
        if (string.IsNullOrEmpty(address.City) ||
            string.IsNullOrEmpty(address.District) ||
            string.IsNullOrEmpty(address.Road))
        {
            return null;
        }

        var validation = new PostalAddressValidation
        {
            IsValidCity = true,
            IsValidDistrict = true,
            IsValidRoad = true
        };

        try
        {
            // 查詢匹配的 postal_rules
            var rules = Database.QueryPostalRules(address.City!, address.District!, address.Road!);

            if (rules == null || rules.Count == 0)
            {
                // 沒有結構化規則，無法進行精確驗證
                return null;
            }

            // 解析巷號（從 Lane 字串解析出數字）
            int? laneNumber = null;
            if (!string.IsNullOrEmpty(address.Lane) && int.TryParse(address.Lane, out var ln))
            {
                laneNumber = ln;
            }

            // 解析弄號（從 Alley 字串解析出數字）
            int? alleyNumber = null;
            if (!string.IsNullOrEmpty(address.Alley) && int.TryParse(address.Alley, out var an))
            {
                alleyNumber = an;
            }

            // 解析附號（取第一個附號）
            int? subNumber = null;
            if (address.SubNumbers != null && address.SubNumbers.Count > 0)
            {
                subNumber = address.SubNumbers[0];
            }

            // 嘗試匹配所有規則
            foreach (var rule in rules)
            {
                bool matched = true;

                // 驗證巷號範圍
                if (laneNumber.HasValue)
                {
                    if (!rule.IsLaneInRange(laneNumber.Value))
                    {
                        matched = false;
                        continue;
                    }
                }
                else if (rule.LaneStart.HasValue)
                {
                    // 規則要求巷號，但地址沒有
                    matched = false;
                    continue;
                }

                // 驗證弄號範圍
                if (alleyNumber.HasValue)
                {
                    if (!rule.IsAlleyInRange(alleyNumber.Value))
                    {
                        matched = false;
                        continue;
                    }
                }
                else if (rule.AlleyStart.HasValue)
                {
                    // 規則要求弄號，但地址沒有
                    matched = false;
                    continue;
                }

                // 驗證門牌號碼範圍
                if (address.Number.HasValue)
                {
                    if (!rule.IsNumberInRange(address.Number.Value, subNumber))
                    {
                        matched = false;
                        continue;
                    }
                }

                // 通過所有驗證
                if (matched)
                {
                    validation.IsValidLane = true;
                    validation.IsValidAlley = true;
                    validation.IsValidNumber = true;
                    validation.MatchedRule = rule;
                    return validation;
                }
            }

            // 沒有規則匹配
            validation.IsValidLane = laneNumber.HasValue ? false : true;
            validation.IsValidAlley = alleyNumber.HasValue ? false : true;
            validation.IsValidNumber = false;

            if (laneNumber.HasValue && !validation.IsValidLane)
            {
                validation.Messages.Add($"巷號「{laneNumber}」不在投遞範圍內");
            }

            if (alleyNumber.HasValue && !validation.IsValidAlley)
            {
                validation.Messages.Add($"弄號「{alleyNumber}」不在投遞範圍內");
            }

            if (!validation.IsValidNumber && address.Number.HasValue)
            {
                var numberStr = subNumber.HasValue
                    ? $"{address.Number}之{subNumber}"
                    : address.Number.ToString();
                validation.Messages.Add($"門牌號碼「{numberStr}號」不在投遞範圍內");
            }

            return validation;
        }
        catch
        {
            // 結構化驗證失敗，返回 null（後退到傳統驗證）
            return null;
        }
    }

    public override string ToString()
    {
        var parts = new List<string>
        {
            $"City={City}",
            $"District={District}",
            $"Road={Road}"
        };

        if (!string.IsNullOrEmpty(Section))
            parts.Add($"Section={Section}");

        if (!string.IsNullOrEmpty(Lane))
            parts.Add($"Lane={Lane}");

        if (!string.IsNullOrEmpty(Alley))
            parts.Add($"Alley={Alley}");

        if (!string.IsNullOrEmpty(SubAlley))
            parts.Add($"SubAlley={SubAlley}");

        if (!string.IsNullOrEmpty(Locality))
            parts.Add($"Locality={Locality}");

        if (Number.HasValue)
            parts.Add($"Number={Number}");

        if (SubNumbers != null && SubNumbers.Count > 0)
            parts.Add($"SubNumbers=[{string.Join(", ", SubNumbers)}]");

        if (!string.IsNullOrEmpty(Floor))
            parts.Add($"Floor={Floor}");

        if (SubFloor.HasValue)
            parts.Add($"SubFloor={SubFloor}");

        if (!string.IsNullOrEmpty(Room))
            parts.Add($"Room={Room}");

        if (IsTemporary)
            parts.Add($"IsTemporary=True");

        if (IsBasement)
            parts.Add($"IsBasement=True");

        return $"PostalAddress({string.Join(", ", parts)})";
    }
}

/// <summary>
/// 地址組件驗證結果
/// </summary>
public record PostalAddressValidation
{
    /// <summary>
    /// 縣市是否有效
    /// </summary>
    public bool IsValidCity { get; set; }

    /// <summary>
    /// 行政區是否有效
    /// </summary>
    public bool IsValidDistrict { get; set; }

    /// <summary>
    /// 路街是否有效
    /// </summary>
    public bool IsValidRoad { get; set; }

    /// <summary>
    /// 地區名稱是否在資料庫中存在
    /// </summary>
    public bool IsValidLocality { get; set; }

    /// <summary>
    /// 巷號是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidLane { get; set; } = true;

    /// <summary>
    /// 弄號是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidAlley { get; set; } = true;

    /// <summary>
    /// 門牌號碼是否有效（使用結構化驗證）
    /// </summary>
    public bool IsValidNumber { get; set; } = true;

    /// <summary>
    /// 匹配的投遞規則（如果有）
    /// </summary>
    public PostalRule? MatchedRule { get; set; }

    /// <summary>
    /// 驗證訊息
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// 是否全部有效
    /// </summary>
    public bool IsValid => IsValidCity && IsValidDistrict && (IsValidRoad || IsValidLocality) &&
                          IsValidLane && IsValidAlley && IsValidNumber;
}

