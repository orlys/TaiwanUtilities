//namespace TaiwanUtilities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;

//public static partial class TaiwanIdGenerator
//{
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static Random GetRandom()
//    {
//#if NET6_0_OR_GREATER
//        return Random.Shared;
//#else
//        return new Random(Guid.NewGuid().GetHashCode());
//#endif
//    }

//    private static readonly int[] TAIWAN_ID_LOCALE_CODE_LIST = [
//        1,  // A -> 10 -> 1 * 1 + 9 * 0 = 1
//        10, // B -> 11 -> 1 * 1 + 9 * 1 = 10
//        19, // C -> 12 -> 1 * 1 + 9 * 2 = 19
//        28, // D
//        37, // E
//        46, // F
//        55, // G
//        64, // H
//        39, // I -> 34 -> 1 * 3 + 9 * 4 = 39
//        73, // J
//        82, // K
//        2,  // L
//        11, // M
//        20, // N
//        48, // O -> 35 -> 1 * 3 + 9 * 5 = 48
//        29, // P
//        38, // Q
//        47, // R
//        56, // S
//        65, // T
//        74, // U
//        83, // V
//        21, // W -> 32 -> 1 * 3 + 9 * 2 = 21
//        3,  // X
//        12, // Y
//        30  // Z -> 33 -> 1 * 3 + 9 * 3 = 30
//    ];

//    private static readonly int[] ID_COEFFICIENTS = [1, 8, 7, 6, 5, 4, 3, 2, 1, 1];

//    /// <summary>
//    /// 產生營利事業統一編號
//    /// </summary>
//    /// <returns>有效的營利事業統一編號</returns>
//    public static string GenerateBusinessAdministrationNumber()
//    {
//        while (true)
//        {
//            var digits = new int[8];

//            // 產生前7位數字
//            for (int i = 0; i < 7; i++)
//            {
//                digits[i] = GetRandom().Next(0, 10);
//            }

//            // 計算檢查碼
//            int sum = 0;
//            int[] weights = { 1, 2, 1, 2, 1, 2, 4, 1 };

//            for (int i = 0; i < 7; i++)
//            {
//                int product = digits[i] * weights[i];
//                sum += product / 10 + product % 10;
//            }

//            // 第8位數字
//            for (int checkDigit = 0; checkDigit < 10; checkDigit++)
//            {
//                digits[7] = checkDigit;
//                int total = sum + checkDigit * weights[7];

//                // 總和是10的倍數，則為有效的統一編號
//                if (total % 10 == 0)
//                {
//                    return string.Concat(digits);
//                }

//                // 特殊規則：如果第7位是7，而且總和除以10餘數為1，則也是有效的
//                if (digits[6] == 7 && total % 10 == 1)
//                {
//                    return string.Concat(digits);
//                }
//            }
//        }
//    }

//    /// <summary>
//    /// 產生自然人憑證號碼
//    /// </summary>
//    /// <returns>有效的自然人憑證號碼</returns>
//    public static string GenerateCitizenDigitalCertificateNumber()
//    {
//        // 自然人憑證格式為2碼英文+14碼數字
//        char[] chars = new char[16];

//        // 前兩碼是英文字母 (A-Z)
//        chars[0] = (char)('A' + GetRandom().Next(0, 26));
//        chars[1] = (char)('A' + GetRandom().Next(0, 26));

//        // 後14碼是數字
//        for (int i = 2; i < 16; i++)
//        {
//            chars[i] = (char)('0' + GetRandom().Next(0, 10));
//        }

//        return new string(chars);
//    }

//    /// <summary>
//    /// 產生電子發票捐贈碼
//    /// </summary>
//    /// <returns>有效的電子發票捐贈碼</returns>
//    public static string GenerateElectronicInvoiceDonateCode()
//    {
//        // 電子發票捐贈碼格式為3~7碼數字
//        int length = GetRandom().Next(3, 8); // 3到7碼

//        StringBuilder sb = new StringBuilder();

//        // 第一個數字不可為0
//        sb.Append(GetRandom().Next(1, 10).ToString());

//        // 產生剩餘的數字
//        for (int i = 1; i < length; i++)
//        {
//            sb.Append(GetRandom().Next(0, 10).ToString());
//        }

//        return sb.ToString();
//    }

//    /// <summary>
//    /// 產生電子發票手機條碼
//    /// </summary>
//    /// <returns>有效的電子發票手機條碼</returns>
//    public static string GenerateElectronicInvoiceMobileBarcode()
//    {
//        // 電子發票手機條碼格式為 / 開頭 + 7碼英數字
//        StringBuilder sb = new StringBuilder("/");

//        // 產生後7碼英數字
//        string validChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
//        for (int i = 0; i < 7; i++)
//        {
//            sb.Append(validChars[GetRandom().Next(0, validChars.Length)]);
//        }

//        return sb.ToString();
//    }

//    /// <summary>
//    /// 產生身分證識別碼
//    /// </summary>
//    /// <returns>有效的身分證識別碼</returns>
//    public static string GenerateIdentityCardNumber()
//    {
//        // 隨機選擇一個英文字母（A-Z）作為首字母
//        char firstChar = (char)('A' + GetRandom().Next(0, 26));

//        // 決定性別碼 (1:男, 2:女)
//        int genderCode = GetRandom().Next(1, 3);

//        var idInDigits = new List<int>();

//        // 計算首字母對應的數值
//        int firstDigit = TAIWAN_ID_LOCALE_CODE_LIST[firstChar - 'A'];
//        idInDigits.Add(firstDigit);

//        // 加入性別碼
//        idInDigits.Add(genderCode);

//        // 產生後面7位隨機數字
//        for (int i = 0; i < 7; i++)
//        {
//            idInDigits.Add(GetRandom().Next(0, 10));
//        }

//        // 計算檢查碼
//        int sum = 0;
//        for (int i = 0; i < 9; i++)
//        {
//            sum += idInDigits[i] * ID_COEFFICIENTS[i];
//        }

//        // 尋找合適的檢查碼
//        for (int checkDigit = 0; checkDigit <= 9; checkDigit++)
//        {
//            if ((sum + checkDigit) % 10 == 0)
//            {
//                // 組合完整身分證號
//                return $"{firstChar}{genderCode}{string.Join("", idInDigits.Skip(2))}{checkDigit}";
//            }
//        }

//        // 理論上不會到這裡，因為一定能找到一個檢查碼使總和被10整除
//        return string.Empty;
//    }
//}