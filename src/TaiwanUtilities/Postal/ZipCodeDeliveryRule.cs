// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// Peak - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

/// <summary>
/// 郵遞區號與投遞規則的組合
/// </summary>
/// <param name="ZipCode">郵遞區號</param>
/// <param name="Rule">投遞規則</param>
public record ZipCodeDeliveryRule(
    string ZipCode,
    DeliveryRule Rule
);
