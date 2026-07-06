// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;

/// <summary>
/// 結構化投遞規則
/// </summary>
public class PostalRule
{
    /// <summary>規則 ID</summary>
    public long Id { get; init; }

    /// <summary>郵遞區號</summary>
    public string ZipCode { get; init; } = string.Empty;

    /// <summary>縣市</summary>
    public string City { get; init; } = string.Empty;

    /// <summary>行政區</summary>
    public string Area { get; init; } = string.Empty;

    /// <summary>路街名</summary>
    public string Road { get; init; } = string.Empty;

    /// <summary>巷號範圍起始</summary>
    public int? LaneStart { get; init; }

    /// <summary>巷號範圍結束（NULL 表示單一巷號）</summary>
    public int? LaneEnd { get; init; }

    /// <summary>弄號範圍起始</summary>
    public int? AlleyStart { get; init; }

    /// <summary>弄號範圍結束（NULL 表示單一弄號）</summary>
    public int? AlleyEnd { get; init; }

    /// <summary>門牌主號範圍起始</summary>
    public int? NumberStart { get; init; }

    /// <summary>門牌附號範圍起始（0=無附號）</summary>
    public int? NumberStartSub { get; init; }

    /// <summary>門牌主號範圍結束（NULL 表示單一門牌）</summary>
    public int? NumberEnd { get; init; }

    /// <summary>門牌附號範圍結束（0=無附號）</summary>
    public int? NumberEndSub { get; init; }

    /// <summary>單雙號規則（0=全部, 1=單號, 2=雙號）</summary>
    public int? EvenOdd { get; init; }

    /// <summary>樓層範圍起始（僅存儲不驗證）</summary>
    public int? FloorStart { get; init; }

    /// <summary>樓層範圍結束（僅存儲不驗證）</summary>
    public int? FloorEnd { get; init; }

    /// <summary>範圍說明</summary>
    public string? Scope { get; init; }

    /// <summary>投遞單位</summary>
    public string? Department { get; init; }

    /// <summary>郵局</summary>
    public string? Office { get; init; }

    /// <summary>備註</summary>
    public string? Remark { get; init; }

    /// <summary>路號（用途未明）</summary>
    public string? RoadNo { get; init; }

    /// <summary>路1（用途未明）</summary>
    public string? Road1 { get; init; }

    /// <summary>巷11（用途未明，保留供未來研究）</summary>
    public int? Lane11 { get; init; }

    /// <summary>巷22（用途未明，保留供未來研究）</summary>
    public int? Lane22 { get; init; }

    /// <summary>弄11（用途未明，保留供未來研究）</summary>
    public int? Alley11 { get; init; }

    /// <summary>弄22（用途未明，保留供未來研究）</summary>
    public int? Alley22 { get; init; }

    /// <summary>
    /// 檢查門牌號碼是否在規則範圍內
    /// </summary>
    public bool IsNumberInRange(int number, int? subNumber = null)
    {
        // 檢查主號範圍
        if (NumberStart.HasValue && number < NumberStart.Value)
        {
            return false;
        }

        if (NumberEnd.HasValue && number > NumberEnd.Value)
        {
            return false;
        }

        // 檢查附號範圍（如果地址有附號）
        if (subNumber.HasValue)
        {
            if (NumberStartSub.HasValue && subNumber.Value < NumberStartSub.Value)
            {
                return false;
            }

            if (NumberEndSub.HasValue && subNumber.Value > NumberEndSub.Value)
            {
                return false;
            }
        }

        // 檢查單雙號規則
        if (EvenOdd.HasValue)
        {
            if (EvenOdd.Value == 1 && number % 2 == 0)
            {
                return false; // 規則要求單號，但地址是雙號
            }

            if (EvenOdd.Value == 2 && number % 2 == 1)
            {
                return false; // 規則要求雙號，但地址是單號
            }
        }

        return true;
    }

    /// <summary>
    /// 檢查巷號是否在規則範圍內
    /// </summary>
    public bool IsLaneInRange(int lane)
    {
        if (LaneStart.HasValue && lane < LaneStart.Value)
        {
            return false;
        }

        if (LaneEnd.HasValue && lane > LaneEnd.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 檢查弄號是否在規則範圍內
    /// </summary>
    public bool IsAlleyInRange(int alley)
    {
        if (AlleyStart.HasValue && alley < AlleyStart.Value)
        {
            return false;
        }

        if (AlleyEnd.HasValue && alley > AlleyEnd.Value)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// 結構化投遞規則資料（用於插入資料庫）
/// </summary>
internal class PostalRuleData
{
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Road { get; set; } = string.Empty;

    public int? LaneStart { get; set; }
    public int? LaneEnd { get; set; }
    public int? AlleyStart { get; set; }
    public int? AlleyEnd { get; set; }

    public int? NumberStart { get; set; }
    public int? NumberStartSub { get; set; }
    public int? NumberEnd { get; set; }
    public int? NumberEndSub { get; set; }

    public int? EvenOdd { get; set; }
    public int? FloorStart { get; set; }
    public int? FloorEnd { get; set; }

    public string? Scope { get; set; }
    public string? Department { get; set; }
    public string? Office { get; set; }
    public string? Remark { get; set; }

    public string? RoadNo { get; set; }
    public string? Road1 { get; set; }
    public int? Lane11 { get; set; }
    public int? Lane22 { get; set; }
    public int? Alley11 { get; set; }
    public int? Alley22 { get; set; }
}