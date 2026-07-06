// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;

/// <summary>
/// 郵遞區號格式輔助方法
/// </summary>
internal static class ZipCodeUtils
{
    /// <summary>
    /// 取得兩個郵遞區號的共同前綴，並截取至合法的台灣郵遞區號長度。
    /// </summary>
    /// <remarks>
    /// 台灣郵遞區號格式：3碼（區域）、5碼（3+2）、6碼（3+2+1）。
    /// 避免產生 4 碼（不標準）。
    /// </remarks>
    internal static string? GetCommonPart(string? strA, string? strB)
    {
        if (strA == null)
        {
            return strB;
        }

        if (strB == null)
        {
            return strA;
        }

        var minLen = Math.Min(strA.Length, strB.Length);
        var i = 0;

        for (; i < minLen; i++)
        {
            if (strA[i] != strB[i])
            {
                break;
            }
        }

        // 台灣郵遞區號格式：3碼（區域）、5碼（3+2）、6碼（3+2+1）
        // 避免產生 4 碼：如果共同前綴是 4 碼，截取到 3 碼
        if (i >= 6)
        {
            return strA[..6];  // 完整 6 碼
        }

        if (i == 5)
        {
            return strA[..5];  // 5 碼（標準）
        }

        if (i == 4)
        {
            return strA[..3];  // 4 碼 → 截取到 3 碼（避免不標準長度）
        }

        if (i >= 3)
        {
            return strA[..3];  // 3 碼（區域碼）
        }

        // 共同前綴 < 3 碼（1或2碼），保留原樣以支持漸進式查詢（如："1"代表臺北）
        return strA[..i];
    }
}
