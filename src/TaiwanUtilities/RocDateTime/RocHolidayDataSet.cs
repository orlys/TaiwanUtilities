namespace TaiwanUtilities;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 台灣國定假日資料集，支援嵌入資料、Runtime 下載與使用者手動增刪。
/// </summary>
/// <remarks>
/// 資料查詢優先順序：使用者手動增刪 → Runtime 下載快取 → T4 嵌入資料。
/// 此類別的所有公開方法皆為執行緒安全。
/// </remarks>
public sealed partial class RocHolidayDataSet
{
    private static readonly ConcurrentDictionary<DateTime, RocHoliday?> s_overrides = new();
    private static readonly ConcurrentDictionary<int, Dictionary<DateTime, RocHoliday>> s_cache = new();

    private static HttpClient HttpClient => Internals.SharedHttpClient.Instance;

    /// <summary>
    /// 嵌入資料涵蓋的最早西元年份
    /// </summary>
    public static int EmbeddedMinYear => s_embeddedMinYear;

    /// <summary>
    /// 嵌入資料涵蓋的最晚西元年份
    /// </summary>
    public static int EmbeddedMaxYear => s_embeddedMaxYear;

    /// <summary>
    /// 查詢指定日期的假日資訊
    /// </summary>
    internal static RocHoliday GetHoliday(RocDateTime date)
    {
        var dt = date.ToDateTime().Date;

        // Layer 1: 使用者手動增刪
        if (s_overrides.TryGetValue(dt, out var overrideValue))
        {
            return overrideValue ?? RocHoliday.None;
        }

        // Layer 2: Runtime 下載快取
        if (s_cache.TryGetValue(dt.Year, out var yearCache) &&
            yearCache.TryGetValue(dt, out var cachedValue))
        {
            return cachedValue;
        }

        // Layer 3: T4 嵌入資料
        if (s_embedded.TryGetValue(dt, out var embeddedValue))
        {
            return embeddedValue;
        }

        if (dt.Year >= EmbeddedMinYear && dt.Year <= EmbeddedMaxYear)
        {
            return DeriveHoliday(dt);
        }

        return RocHoliday.None;
    }

    private static RocHoliday DeriveHoliday(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Saturday => new RocHoliday(true, HolidayRole.All, "週六"),
            DayOfWeek.Sunday => new RocHoliday(true, HolidayRole.All, "週日"),
            _ => new RocHoliday(false, HolidayRole.None, "工作日"),
        };
    }

    /// <summary>
    /// 查詢指定西元年份是否有資料（嵌入或快取）
    /// </summary>
    public static bool ContainsYear(int year)
    {
        if (year >= EmbeddedMinYear && year <= EmbeddedMaxYear)
        {
            return true;
        }

        return s_cache.ContainsKey(year);
    }

    /// <summary>
    /// 手動新增或覆寫假日資訊
    /// </summary>
    public static void Add(RocDateTime date, RocHoliday holiday)
    {
        var dt = date.ToDateTime().Date;
        s_overrides[dt] = holiday;
    }

    /// <summary>
    /// 手動移除假日（標記為非假日）
    /// </summary>
    /// <returns>若該日期已存在於任何資料層，回傳 <c>true</c></returns>
    public static bool Remove(RocDateTime date)
    {
        var dt = date.ToDateTime().Date;
        var existed = s_overrides.ContainsKey(dt) ||
                      (s_cache.TryGetValue(dt.Year, out var yc) && yc.ContainsKey(dt)) ||
                      s_embedded.ContainsKey(dt) ||
                      (dt.Year >= EmbeddedMinYear && dt.Year <= EmbeddedMaxYear);

        s_overrides[dt] = null; // null = 標記刪除
        return existed;
    }

    /// <summary>
    /// 從遠端更新假日資料
    /// </summary>
    /// <remarks>
    /// 每次呼叫皆會嘗試下載最新資料，下載來源優先順序：
    /// <list type="number">
    /// <item>TaiwanUtilities GitHub Release（合併 CSV，含所有已知年份）</item>
    /// <item>行政院人事行政總處 data.gov.tw（補充當前年與下一年）</item>
    /// </list>
    /// </remarks>
    public static async Task UpdateAsync(CancellationToken ct = default)
    {
        // Primary: GitHub Release（包含所有已知年份）
        await TryDownloadFromReleaseAsync(ct).ConfigureAwait(false);

        // Fallback: 若當前年或下一年仍缺少資料，從 data.gov.tw 補充
        var currentYear = DateTime.Today.Year;
        var neededYears = new[] { currentYear, currentYear + 1 };

        foreach (var year in neededYears)
        {
            if (s_cache.ContainsKey(year) || (year >= EmbeddedMinYear && year <= EmbeddedMaxYear))
            {
                continue;
            }

            var holidays = await TryDownloadFromGovAsync(year, ct).ConfigureAwait(false);
            if (holidays.Count > 0)
            {
                s_cache[year] = holidays;
            }
        }
    }

    /// <summary>
    /// 從本地 CSV 檔案更新假日資料（使用本專案的 holidays.csv 格式）
    /// </summary>
    /// <param name="csvPath">CSV 檔案路徑</param>
    /// <param name="ct">取消權杖</param>
    /// <exception cref="FileNotFoundException">找不到指定的檔案</exception>
    public static async Task UpdateFromAsync(string csvPath, CancellationToken ct = default)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("找不到假日資料檔案", csvPath);
        }

#if NET8_0_OR_GREATER
        var content = await File.ReadAllTextAsync(csvPath, ct).ConfigureAwait(false);
#else
        var content = await Task.Run(() => File.ReadAllText(csvPath), ct).ConfigureAwait(false);
#endif
        MergeCsvToCache(content);
    }

    /// <summary>
    /// 從串流更新假日資料（使用本專案的 holidays.csv 格式）
    /// </summary>
    /// <param name="stream">包含 CSV 內容的串流</param>
    /// <param name="ct">取消權杖</param>
    public static async Task UpdateFromStreamAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
#if NET8_0_OR_GREATER
        var content = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
#else
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
#endif
        MergeCsvToCache(content);
    }

    /// <summary>
    /// 將 CSV 內容解析後合併到快取
    /// </summary>
    private static void MergeCsvToCache(string content)
    {
        var allData = new Dictionary<DateTime, RocHoliday>();
        ParseCsv(content, allData);

        foreach (var group in allData.GroupBy(kv => kv.Key.Year))
        {
            var yearDict = new Dictionary<DateTime, RocHoliday>();
            foreach (var kv in group)
            {
                yearDict[kv.Key] = kv.Value;
            }
            s_cache[group.Key] = yearDict;
        }
    }

    /// <summary>
    /// 重置資料集（清除 Runtime 快取與使用者修改，回到嵌入資料）
    /// </summary>
    public static void Reload()
    {
        s_overrides.Clear();
        s_cache.Clear();
        s_lastPublishedAt = null;
    }

    #region GitHub Release Download

    private const string RELEASE_TAG = "holidays-latest";
    private const string RELEASE_ASSET_NAME = "holidays.csv";
    private static string? s_lastPublishedAt;

    private static async Task TryDownloadFromReleaseAsync(CancellationToken ct)
    {
        try
        {
            var releaseInfo = await Internals.GitHubReleaseClient
                .GetReleaseInfoAsync(RELEASE_TAG, RELEASE_ASSET_NAME, ct)
                .ConfigureAwait(false);

            if (releaseInfo == null)
            {
                return;
            }

            var (publishedAt, downloadUrl) = releaseInfo.Value;

            // 版本相同則跳過下載
            if (s_lastPublishedAt != null && s_lastPublishedAt == publishedAt)
            {
                return;
            }

            var client = HttpClient;

#if NET8_0_OR_GREATER
            var response = await client.GetAsync(downloadUrl, ct).ConfigureAwait(false);
#else
            var response = await client.GetAsync(downloadUrl, ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

#if NET8_0_OR_GREATER
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            var allData = new Dictionary<DateTime, RocHoliday>();
            ParseCsv(content, allData);

            // 依年份分組寫入快取
            foreach (var group in allData.GroupBy(kv => kv.Key.Year))
            {
                var yearDict = new Dictionary<DateTime, RocHoliday>();
                foreach (var kv in group)
                {
                    yearDict[kv.Key] = kv.Value;
                }
                s_cache.TryAdd(group.Key, yearDict);
            }

            // 更新版本標記
            s_lastPublishedAt = publishedAt;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Debug.WriteLine($"Failed to download holidays from GitHub Release: {ex.Message}");
        }
    }

    #endregion

    #region data.gov.tw Download

    private const string GOV_DATASET_API_URL = "https://data.gov.tw/api/v2/rest/dataset/14718";
    private static readonly Regex s_rocYearPattern = new(@"(\d{2,3})年", RegexOptions.Compiled);
    private static readonly Dictionary<int, string> s_weekdayNames = new()
    {
        [0] = "週日",
        [1] = "",
        [2] = "",
        [3] = "",
        [4] = "",
        [5] = "",
        [6] = "週六",
    };

    private readonly struct RoleHolidayRule
    {
        public RoleHolidayRule(int month, int day, HolidayRole role, int startYear, string defaultDescription)
        {
            Month = month;
            Day = day;
            Role = role;
            StartYear = startYear;
            DefaultDescription = defaultDescription;
        }

        public int Month { get; }

        public int Day { get; }

        public HolidayRole Role { get; }

        public int StartYear { get; }

        public string DefaultDescription { get; }

        public bool AppliesTo(DateTime date) =>
            date.Month == Month && date.Day == Day && date.Year >= StartYear;
    }

    // data.gov.tw 的行事曆由人事行政總處發布，以公務員行事曆為本位；
    // 勞工、軍人、教師等角色專屬假日可能被來源標為非假日，需在解析時補上角色語意。
    private static readonly RoleHolidayRule[] s_roleHolidayRules =
    [
        new RoleHolidayRule(5, 1, HolidayRole.Labor, 1, "勞動節"),
        new RoleHolidayRule(9, 3, HolidayRole.Soldier, 1, "軍人節"),
        new RoleHolidayRule(9, 28, HolidayRole.Teacher, 2025, "教師節"),
    ];

    private static async Task<Dictionary<DateTime, RocHoliday>> TryDownloadFromGovAsync(int year, CancellationToken ct)
    {
        var result = new Dictionary<DateTime, RocHoliday>();

        try
        {
            var csvUrl = await DiscoverGovCsvUrlAsync(year, ct).ConfigureAwait(false);
            if (csvUrl == null)
            {
                return result;
            }

            var client = HttpClient;

#if NET8_0_OR_GREATER
            var response = await client.GetAsync(csvUrl, ct).ConfigureAwait(false);
#else
            var response = await client.GetAsync(csvUrl, ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

#if NET8_0_OR_GREATER
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            ParseGovCsv(content, result);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or JsonException)
        {
            Debug.WriteLine($"Failed to download holidays from data.gov.tw for {year}: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 從 data.gov.tw 的 dataset metadata API 找到指定年份的 CSV 下載網址
    /// </summary>
    private static async Task<string> DiscoverGovCsvUrlAsync(int year, CancellationToken ct)
    {
        var client = HttpClient;
        var rocYear = year - 1911;

#if NET8_0_OR_GREATER
        var response = await client.GetAsync(GOV_DATASET_API_URL, ct).ConfigureAwait(false);
#else
        var response = await client.GetAsync(GOV_DATASET_API_URL, ct).ConfigureAwait(false);
#endif
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

#if NET8_0_OR_GREATER
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

        using var doc = JsonDocument.Parse(json);
        var distributions = doc.RootElement
            .GetProperty("result")
            .GetProperty("distribution");

        string bestUrl = null;
        var isCorrected = false;

        foreach (var dist in distributions.EnumerateArray())
        {
            var format = dist.TryGetProperty("resourceFormat", out var fmt) ? fmt.GetString() : "";
            if (!string.Equals(format, "CSV", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var desc = dist.TryGetProperty("resourceDescription", out var d) ? d.GetString() ?? "" : "";
            if (desc.Contains("Google") || desc.Contains("google"))
            {
                continue;
            }

            var match = s_rocYearPattern.Match(desc);
            if (!match.Success)
            {
                continue;
            }

            var csvRocYear = int.Parse(match.Groups[1].Value);
            if (csvRocYear + 1911 != year)
            {
                continue;
            }

            var url = dist.TryGetProperty("resourceDownloadUrl", out var u) ? u.GetString() : null;
            url ??= dist.TryGetProperty("downloadUrl", out var u2) ? u2.GetString() : null;
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            var corrected = desc.Contains("修正");
            if (bestUrl == null || (corrected && !isCorrected))
            {
                bestUrl = url;
                isCorrected = corrected;
            }
        }

        return bestUrl;
    }

    /// <summary>
    /// 解析行政院 data.gov.tw 原始 CSV 格式
    /// </summary>
    /// <remarks>
    /// 欄位: [0]=日期(YYYYMMDD), [1]=星期, [2]=是否放假(2=放假), [3]=備註
    /// </remarks>
    internal static void ParseGovCsv(string content, Dictionary<DateTime, RocHoliday> target)
    {
        using var reader = new StringReader(content);
        string line;
        var isFirstLine = true;

        while ((line = reader.ReadLine()) != null)
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                continue; // skip header
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                continue;
            }

            var dateStr = parts[0].Trim();
            if (dateStr.Length != 8)
            {
                continue;
            }

#if NET8_0_OR_GREATER
            if (!int.TryParse(dateStr.AsSpan(0, 4), out var y) ||
                !int.TryParse(dateStr.AsSpan(4, 2), out var m) ||
                !int.TryParse(dateStr.AsSpan(6, 2), out var d))
            {
                continue;
            }
#else
            if (!int.TryParse(dateStr[..4], out var y) ||
                !int.TryParse(dateStr.Substring(4, 2), out var m) ||
                !int.TryParse(dateStr.Substring(6, 2), out var d))
            {
                continue;
            }
#endif

            DateTime dt;
            try
            {
                dt = new DateTime(y, m, d);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            var isHoliday = parts[2].Trim() == "2";
            var description = parts[3].Trim();

            var role = HolidayRole.None;

            var matchedRoleRule = false;
            foreach (var rule in s_roleHolidayRules)
            {
                if (!rule.AppliesTo(dt))
                {
                    continue;
                }

                role = rule.Role;
                isHoliday = true;
                if (string.IsNullOrEmpty(description) || description == "工作日")
                {
                    description = rule.DefaultDescription;
                }

                matchedRoleRule = true;
                break;
            }

            if (!matchedRoleRule && isHoliday)
            {
                role = HolidayRole.All;
            }

            if (string.IsNullOrEmpty(description))
            {
                if (isHoliday)
                {
                    s_weekdayNames.TryGetValue((int)dt.DayOfWeek, out description);
                }

                description ??= "工作日";
            }

            target[dt] = new RocHoliday(isHoliday, role, description);
        }
    }

    #endregion

    #region CSV Parsing (holidays.csv format)

    /// <summary>
    /// 解析本專案的 holidays.csv 格式
    /// </summary>
    /// <remarks>
    /// 欄位: date, is_holiday, role, description
    /// </remarks>
    internal static void ParseCsv(string content, Dictionary<DateTime, RocHoliday> target)
    {
        using var reader = new StringReader(content);
        string line;
        var isFirstLine = true;

        while ((line = reader.ReadLine()) != null)
        {
            if (isFirstLine)
            {
                isFirstLine = false;
                continue; // skip header
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Format: "20250101, true , all     , 開國紀念日,"
            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                continue;
            }

            var dateStr = parts[0].Trim();
            if (dateStr.Length != 8)
            {
                continue;
            }

#if NET8_0_OR_GREATER
            if (!int.TryParse(dateStr.AsSpan(0, 4), out var y) ||
                !int.TryParse(dateStr.AsSpan(4, 2), out var m) ||
                !int.TryParse(dateStr.AsSpan(6, 2), out var d))
            {
                continue;
            }
#else
            if (!int.TryParse(dateStr[..4], out var y) ||
                !int.TryParse(dateStr.Substring(4, 2), out var m) ||
                !int.TryParse(dateStr.Substring(6, 2), out var d))
            {
                continue;
            }
#endif

            DateTime dt;
            try
            {
                dt = new DateTime(y, m, d);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            var isHoliday = parts[1].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            var roleStr = parts[2].Trim();
            var description = parts[3].Trim();

            var role = roleStr switch
            {
                "labor" => HolidayRole.Labor,
                "soldier" => HolidayRole.Soldier,
                "teacher" => HolidayRole.Teacher,
                _ => isHoliday ? HolidayRole.All : HolidayRole.None,
            };

            target[dt] = new RocHoliday(isHoliday, role, description);
        }
    }

    #endregion
}
