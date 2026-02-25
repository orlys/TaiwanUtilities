// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System.Collections.Generic;

/// <summary>
/// 地址驗證結果
/// </summary>
public record PostalValidationResult
{
    /// <summary>
    /// 驗證是否通過
    /// </summary>
    public bool IsValid { get; internal set; }

    /// <summary>
    /// 找到的郵遞區號（如果驗證通過）
    /// </summary>
    public string ZipCode { get; internal set; } = string.Empty;

    /// <summary>
    /// 正規化後的地址
    /// </summary>
    public string NormalizedAddress { get; internal set; } = string.Empty;

    /// <summary>
    /// 驗證訊息
    /// </summary>
    public List<string> Messages { get; internal set; } = new();

    /// <summary>
    /// 驗證失敗的原因
    /// </summary>
    public PostalValidationFailureReason FailureReason { get; internal set; } = PostalValidationFailureReason.None;

    /// <summary>
    /// 建議的正確地址（如果有）
    /// </summary>
    public List<string> Suggestions { get; internal set; } = new();
}

/// <summary>
/// 驗證失敗原因
/// </summary>
public enum PostalValidationFailureReason
{
    /// <summary>
    /// 無（驗證通過）
    /// </summary>
    None = 0,

    /// <summary>
    /// 地址格式無效
    /// </summary>
    InvalidFormat = 1,

    /// <summary>
    /// 找不到地址
    /// </summary>
    AddressNotFound = 2,

    /// <summary>
    /// 門牌號碼超出範圍
    /// </summary>
    NumberOutOfRange = 3,

    /// <summary>
    /// 門牌號碼不符合規則（例如：單雙號）
    /// </summary>
    NumberRuleMismatch = 4,

    /// <summary>
    /// 區域不存在
    /// </summary>
    DistrictNotFound = 5,

    /// <summary>
    /// 街道不存在
    /// </summary>
    StreetNotFound = 6
}
