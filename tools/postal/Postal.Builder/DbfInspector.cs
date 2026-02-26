namespace TaiwanUtilities.Builder;

using System;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// 檢查 .dbf 檔案結構
/// </summary>
public class DbfInspector
{
    public static void Inspect(string dbfPath)
    {
        Console.WriteLine($"檢查 DBF 檔案: {dbfPath}\n");

        try
        {
            // 註冊 BIG5 編碼
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.OpenRead(dbfPath);
            using var reader = new DbfDataReader.DbfDataReader(stream, new DbfDataReader.DbfDataReaderOptions
            {
                Encoding = Encoding.GetEncoding("big5")
            });

            Console.WriteLine("=== DBF 檔案資訊 ===");
            Console.WriteLine($"欄位數量: {reader.DbfTable.Columns.Count}");

            Console.WriteLine("\n=== 欄位結構 ===");
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var column = reader.DbfTable.Columns[i];
                Console.WriteLine($"  {reader.GetName(i),-15} | 型別: {column.ColumnType,-10} | 長度: {column.Length}");
            }

            Console.WriteLine("\n=== 前 5 筆記錄範例 ===");
            int count = 0;
            while (reader.Read() && count < 5)
            {
                Console.WriteLine($"\n記錄 #{count + 1}:");
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.GetValue(i);
                    var displayValue = value?.ToString()?.Trim() ?? "(null)";
                    if (displayValue.Length > 50)
                    {
                        displayValue = displayValue[..50] + "...";
                    }

                    Console.WriteLine($"  {columnName,-15}: {displayValue}");
                }
                count++;
            }

            Console.WriteLine($"\n✅ 檢查完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 錯誤: {ex.Message}");
            Console.WriteLine($"\n詳細資訊:\n{ex}");
        }
    }
}
