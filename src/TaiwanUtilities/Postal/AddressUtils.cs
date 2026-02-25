// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;

/// <summary>
/// 地址工具類別
/// </summary>
public static class AddressUtils
{
    /// <summary>
    /// 正規化地址字串（統一格式）
    /// </summary>
    /// <param name="address">原始地址</param>
    /// <returns>正規化後的地址</returns>
    /// <example>
    /// <code>
    /// var normalized = AddressUtils.Normalize("台北市，信義區，市府路１號");
    /// // 返回 "臺北市信義區市府路1號"
    /// </code>
    /// </example>
    public static string Normalize(string address)
    {
        var addr = new AddressTokenizer(address);
        return addr.Flat();
    }
}
