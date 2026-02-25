// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// Peak - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

/// <summary>
/// 郵政地址候選項目（用於自動完成）
/// </summary>
/// <param name="AddressText">候選地址字串</param>
/// <param name="ZipCode">郵遞區號</param>
/// <param name="Address">結構化郵政地址</param>
public record PostalAddressSuggestion(
    string AddressText,
    string ZipCode,
    PostalAddress? Address
);
