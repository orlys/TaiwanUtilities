// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities;

using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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

        // 建立臨時檔案（使用組件 hash 避免路徑可預測）
        var assemblyHash = assembly.GetHashCode().ToString("x8");
        var tempPath = Path.Combine(Path.GetTempPath(), "TaiwanUtilities", $"{assemblyHash}_{resourceName}");
        var tempDir = Path.GetDirectoryName(tempPath);

        if (!System.IO.Directory.Exists(tempDir))
            System.IO.Directory.CreateDirectory(tempDir!);

        // 使用鎖定確保只有一個執行緒/處理程序執行解壓縮
        lock (_extractLock)
        {
            // 如果臨時檔案已存在且大小和內容雜湊相同，直接使用
            if (File.Exists(tempPath))
            {
                var fileInfo = new FileInfo(tempPath);
                if (fileInfo.Length == resource.Length)
                {
                    // 比較前 8KB 的雜湊，避免被替換的同大小檔案
                    if (VerifyPartialHash(resource, tempPath))
                    {
                        resource.Dispose();
                        return tempPath;
                    }
                    resource.Position = 0; // 重置 stream 位置
                }
            }

            // 複製資源到臨時檔案
            // 使用 FileShare.None 防止其他處理程序在寫入時存取
            try
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(fileStream);
                }
            }
            catch (IOException) when (File.Exists(tempPath))
            {
                // 檔案被其他程序佔用（如 SQLite），但已存在則直接使用
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

    /// <summary>
    /// 比較嵌入資源和磁碟檔案的前 8KB 雜湊，快速驗證內容一致性
    /// </summary>
    private static bool VerifyPartialHash(Stream resource, string filePath)
    {
        const int hashSize = 8192;
        try
        {
            var buffer1 = new byte[hashSize];
            var buffer2 = new byte[hashSize];

            resource.Position = 0;
            var read1 = ReadFully(resource, buffer1);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var read2 = ReadFully(fileStream, buffer2);

            if (read1 != read2) return false;

            for (int i = 0; i < read1; i++)
            {
                if (buffer1[i] != buffer2[i]) return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ReadFully(Stream stream, byte[] buffer)
    {
        int totalRead = 0;
        int read;
        while (totalRead < buffer.Length &&
               (read = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
        {
            totalRead += read;
        }
        return totalRead;
    }
}
