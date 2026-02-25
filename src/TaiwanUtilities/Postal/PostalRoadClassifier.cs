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
/// 路名類型
/// </summary>
public enum PostalRoadType
{
    /// <summary>未知</summary>
    Unknown,

    /// <summary>路（如：中山路）</summary>
    Road,

    /// <summary>街（如：中正街）</summary>
    Street,

    /// <summary>道（如：凱達格蘭大道）</summary>
    Boulevard,

    /// <summary>巷</summary>
    Lane,

    /// <summary>弄</summary>
    Alley,

    /// <summary>段（如：八德路一段）</summary>
    Section,

    /// <summary>村（如：仁愛村）</summary>
    Village,

    /// <summary>里</summary>
    Neighborhood,

    /// <summary>歷史聚落/眷村（如：再興、成功新屯、自強新村）</summary>
    Settlement,

    /// <summary>地理特徵（如：福德坑、曲尺坑、公館崙）</summary>
    Geographic,

    /// <summary>傳統建築（如：王軍寮、南邦寮）</summary>
    TraditionalBuilding,

    /// <summary>市場</summary>
    Market,

    /// <summary>商場</summary>
    ShoppingCenter,

    /// <summary>住宅社區（如：新城、山莊、社區、家園）</summary>
    ResidentialComplex,

    /// <summary>工業區</summary>
    IndustrialZone,

    /// <summary>島嶼（如：基隆嶼、彭佳嶼）</summary>
    Island,

    /// <summary>公園</summary>
    Park,

    /// <summary>碼頭</summary>
    Dock,

    /// <summary>建築物樓層（如：二樓、三樓）</summary>
    Floor,

    /// <summary>建築物棟別（如：一棟、二棟）</summary>
    Building,

    /// <summary>地下層/地下商場</summary>
    Basement,

    /// <summary>其他特殊地點</summary>
    Special
}

/// <summary>
/// 路名分類器 - 根據資料集分析結果進行分類
/// </summary>
public static class PostalRoadClassifier
{
    /// <summary>
    /// 標準路名單位字
    /// </summary>
    private static readonly HashSet<string> StandardRoadUnits = new()
    {
        "路", "街", "道", "巷", "弄", "衖", "段"
    };

    /// <summary>
    /// 結尾字元模式對應表（基於 6,815 筆非標準路名統計）
    /// </summary>
    private static readonly Dictionary<char, PostalRoadType> EndingCharPatterns = new()
    {
        // 傳統建築/聚落 (378筆)
        ['寮'] = PostalRoadType.TraditionalBuilding,
        
        // 地理特徵
        ['坑'] = PostalRoadType.Geographic,  // 334筆 - 山谷
        ['山'] = PostalRoadType.Geographic,  // 237筆
        ['埔'] = PostalRoadType.Geographic,  // 180筆 - 平原/台地
        ['湖'] = PostalRoadType.Geographic,  // 158筆
        ['林'] = PostalRoadType.Geographic,  // 138筆
        ['溪'] = PostalRoadType.Geographic,  // 69筆
        ['崙'] = PostalRoadType.Geographic,  // 64筆
        ['潭'] = PostalRoadType.Geographic,  // 48筆
        ['港'] = PostalRoadType.Geographic,  // 43筆
        ['口'] = PostalRoadType.Geographic,  // 57筆
        ['坪'] = PostalRoadType.Geographic,  // 98筆
        
        // 村落
        ['村'] = PostalRoadType.Village,     // 323筆
        ['庄'] = PostalRoadType.Village,     // 119筆
        ['莊'] = PostalRoadType.Village,     // 64筆（山莊等）
        ['里'] = PostalRoadType.Neighborhood, // 13筆
        
        // 歷史聚落（眷村特徵）
        ['興'] = PostalRoadType.Settlement,  // 153筆（新興、中興、再興等）
        
        // 傳統地名
        ['厝'] = PostalRoadType.TraditionalBuilding, // 167筆
        ['子'] = PostalRoadType.Geographic,  // 162筆（地名後綴）
        ['仔'] = PostalRoadType.Geographic,  // 105筆
        ['頭'] = PostalRoadType.Geographic,  // 191筆（位置：前端）
        ['腳'] = PostalRoadType.Geographic,  // 126筆（位置：山腳）
        ['尾'] = PostalRoadType.Geographic,  // 86筆（位置：尾端）
        ['頂'] = PostalRoadType.Geographic,  // 83筆
        
        // 市場/廣場
        ['場'] = PostalRoadType.Market,      // 111筆
        ['園'] = PostalRoadType.Special,     // 73筆（公園、果園等）
        
        // 其他常見地名
        ['南'] = PostalRoadType.Geographic,  // 62筆
        ['安'] = PostalRoadType.Geographic,  // 58筆
        ['內'] = PostalRoadType.Geographic,  // 57筆
        ['下'] = PostalRoadType.Geographic,  // 49筆
        ['和'] = PostalRoadType.Geographic,  // 48筆
    };

    /// <summary>
    /// 眷村/軍事聚落關鍵字
    /// </summary>
    private static readonly string[] SettlementKeywords = new[]
    {
        "新村", "新屯", "眷村", "自強", "忠義", "仁愛", "介壽", 
        "四維", "復興", "建國", "光復"
    };

    /// <summary>
    /// 建築物相關關鍵字
    /// </summary>
    private static readonly string[] BasementKeywords = new[] { "地下層", "地下商場", "地下街" };
    private static readonly string[] FloorKeywords = new[] { "樓", "層" };
    private static readonly string[] BuildingKeywords = new[] { "棟" };

    /// <summary>
    /// 商業相關關鍵字
    /// </summary>
    private static readonly string[] MarketKeywords = new[] { "市場" };
    private static readonly string[] ShoppingCenterKeywords = new[] { "商場" };
    private static readonly string[] IndustrialZoneKeywords = new[] { "工業區" };

    /// <summary>
    /// 社區相關關鍵字
    /// </summary>
    private static readonly string[] ResidentialComplexKeywords = new[] { "新城", "山莊", "社區", "家園" };

    /// <summary>
    /// 地理相關關鍵字
    /// </summary>
    private static readonly string[] IslandKeywords = new[] { "嶼" };
    private static readonly string[] ParkKeywords = new[] { "公園" };
    private static readonly string[] DockKeywords = new[] { "號碼頭" };

    /// <summary>
    /// 分類路名
    /// </summary>
    /// <param name="road">路名字串</param>
    /// <returns>路名類型</returns>
    public static PostalRoadType Classify(string road)
    {
        if (string.IsNullOrWhiteSpace(road))
            return PostalRoadType.Unknown;

        road = road.Trim();

        // === 第一優先：建築物相關（最高優先級，因為會包含其他關鍵字） ===

        // 1. 地下層/地下商場
        if (BasementKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Basement;

        // 2. 碼頭（包含「號碼頭」）
        if (DockKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Dock;

        // 3. 工業區
        if (IndustrialZoneKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.IndustrialZone;

        // === 第二優先：商業設施 ===

        // 4. 商場（在市場之前，因為「商場」更具體）
        if (ShoppingCenterKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.ShoppingCenter;

        // 5. 市場
        if (MarketKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Market;

        // === 第三優先：社區/聚落特徵 ===

        // 6. 眷村特徵（新村、新屯等，優先於一般村落）
        if (road.Contains("新村") || road.Contains("新屯") || road.Contains("眷村"))
            return PostalRoadType.Settlement;

        // 7. 住宅社區（新城、山莊、社區、家園）
        if (ResidentialComplexKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.ResidentialComplex;

        // === 第四優先：地理特徵 ===

        // 8. 島嶼
        if (IslandKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Island;

        // 9. 公園
        if (ParkKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Park;

        // === 第五優先：建築物棟別/樓層（在村里之前檢查） ===

        // 10. 棟別（但排除地名如「李棟山」）
        if (BuildingKeywords.Any(k => road.Contains(k)))
        {
            // 如果包含「山」，可能是地名而非棟別
            if (road.Contains("山") || road.Contains("嶺"))
                return PostalRoadType.Geographic;
            return PostalRoadType.Building;
        }

        // 11. 樓層（但要排除「樓仔」等地名）
        if (FloorKeywords.Any(k => road.Contains(k)))
        {
            // 如果是「層」結尾但沒有數字，可能是地名
            if (road.EndsWith("層") && !road.Any(char.IsDigit))
                return PostalRoadType.Geographic;
            return PostalRoadType.Floor;
        }

        // === 第六優先：行政區劃 ===

        // 12. 明確的村里結尾
        if (road.EndsWith("村") || road.EndsWith("里"))
        {
            return road.EndsWith("村") ? PostalRoadType.Village : PostalRoadType.Neighborhood;
        }

        // 3. 檢查標準路名單位（注意優先順序）
        // 弄 > 巷 > 段 > 路/街/道
        if (road.Contains("弄"))
            return PostalRoadType.Alley;

        if (road.Contains("巷"))
            return PostalRoadType.Lane;

        if (road.Contains("段"))
            return PostalRoadType.Section;

        if (road.Contains("路"))
            return PostalRoadType.Road;

        if (road.Contains("街"))
            return PostalRoadType.Street;

        if (road.Contains("道"))
            return PostalRoadType.Boulevard;

        // 4. 檢查其他眷村/聚落關鍵字（無單位字時，如「復興」、「光復」等）
        if (SettlementKeywords.Any(k => road.Contains(k)))
            return PostalRoadType.Settlement;

        // 5. 根據結尾字元模式判斷
        if (road.Length > 0)
        {
            var lastChar = road[road.Length - 1];
            if (EndingCharPatterns.TryGetValue(lastChar, out var type))
            {
                return type;
            }
        }

        // 6. 預設為特殊地點
        return PostalRoadType.Special;
    }

    /// <summary>
    /// 取得路名類型的描述
    /// </summary>
    public static string GetDescription(PostalRoadType type)
    {
        return type switch
        {
            PostalRoadType.Road => "路",
            PostalRoadType.Street => "街",
            PostalRoadType.Boulevard => "道",
            PostalRoadType.Lane => "巷",
            PostalRoadType.Alley => "弄",
            PostalRoadType.Section => "段",
            PostalRoadType.Village => "村",
            PostalRoadType.Neighborhood => "里",
            PostalRoadType.Settlement => "歷史聚落/眷村",
            PostalRoadType.Geographic => "地理特徵",
            PostalRoadType.TraditionalBuilding => "傳統建築",
            PostalRoadType.Market => "市場",
            PostalRoadType.ShoppingCenter => "商場",
            PostalRoadType.ResidentialComplex => "住宅社區",
            PostalRoadType.IndustrialZone => "工業區",
            PostalRoadType.Island => "島嶼",
            PostalRoadType.Park => "公園",
            PostalRoadType.Dock => "碼頭",
            PostalRoadType.Floor => "建築物樓層",
            PostalRoadType.Building => "建築物棟別",
            PostalRoadType.Basement => "地下層",
            PostalRoadType.Special => "特殊地點",
            _ => "未知"
        };
    }

    /// <summary>
    /// 判斷是否為標準路街名稱
    /// </summary>
    public static bool IsStandardRoad(string road)
    {
        var type = Classify(road);
        return type is PostalRoadType.Road or PostalRoadType.Street or PostalRoadType.Boulevard 
                    or PostalRoadType.Lane or PostalRoadType.Alley or PostalRoadType.Section;
    }

    /// <summary>
    /// 判斷是否為非標準地名（需特殊處理）
    /// </summary>
    public static bool IsNonStandardLocation(string road)
    {
        return !IsStandardRoad(road);
    }
}
