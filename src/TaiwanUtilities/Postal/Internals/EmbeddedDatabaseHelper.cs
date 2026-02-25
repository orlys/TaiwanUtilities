// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// Peak - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.IO;
using System.Reflection;
using System.Threading;

/// <summary>
/// 內嵌資料庫資源輔助類別
/// 負責解壓縮和管理內嵌的 SQLite 資料庫檔案
/// </summary>
internal static class EmbeddedDatabaseHelper
{
    private static readonly object _extractLock = new object();

    /// <summary>
    /// 從內嵌資源複製資料庫到臨時檔案
    /// </summary>
    /// <param name="resourceName">資源名稱（例如：zipcode.db）</param>
    /// <returns>臨時檔案路徑</returns>
    internal static string ExtractEmbeddedDatabase(string resourceName = "zipcode.db")
    {
        // 使用此類別所在的組件，而非執行中的組件（避免在測試專案中呼叫時找錯組件）
        var assembly = typeof(EmbeddedDatabaseHelper).Assembly;
        var fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

        // 嘗試找到資源
        var resource = assembly.GetManifestResourceStream(fullResourceName);
        if (resource == null)
        {
            // 嘗試列出所有資源以找到正確的名稱
            var resourceNames = assembly.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    resource = assembly.GetManifestResourceStream(name);
                    break;
                }
            }
        }

        if (resource == null)
        {
            throw new FileNotFoundException(
                $"找不到內嵌資源 '{resourceName}'。請確保資料庫已設為內嵌資源。");
        }

        // 建立臨時檔案
        var tempPath = Path.Combine(Path.GetTempPath(), "TaiwanUtilities", resourceName);
        var tempDir = Path.GetDirectoryName(tempPath);

        if (!System.IO.Directory.Exists(tempDir))
            System.IO.Directory.CreateDirectory(tempDir!);

        // 使用鎖定確保只有一個執行緒/處理程序執行解壓縮
        lock (_extractLock)
        {
            // 如果臨時檔案已存在且大小相同，直接使用
            if (File.Exists(tempPath))
            {
                var fileInfo = new FileInfo(tempPath);
                if (fileInfo.Length == resource.Length)
                {
                    resource.Dispose();
                    return tempPath;
                }
            }

            // 複製資源到臨時檔案
            // 使用 FileShare.None 防止其他處理程序在寫入時存取
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                resource.CopyTo(fileStream);
            }
        }

        resource.Dispose();
        return tempPath;
    }

    /// <summary>
    /// 檢查內嵌資源是否存在
    /// </summary>
    public static bool HasEmbeddedDatabase(string resourceName = "zipcode.db")
    {
        var assembly = typeof(EmbeddedDatabaseHelper).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var name in resourceNames)
        {
            if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 列出所有內嵌資源
    /// </summary>
    public static string[] GetEmbeddedResourceNames()
    {
        var assembly = typeof(EmbeddedDatabaseHelper).Assembly;
        return assembly.GetManifestResourceNames();
    }
}
