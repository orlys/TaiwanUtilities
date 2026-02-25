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
    private readonly ConcurrentDictionary<DateTime, RocHoliday?> _overrides = new();
    private readonly ConcurrentDictionary<int, Dictionary<DateTime, RocHoliday>> _cache = new();
    private int _releaseDownloadAttempted;

    private static HttpClient HttpClient => Internals.SharedHttpClient.Instance;

    private static readonly Lazy<(int Min, int Max)> s_embeddedYearRange = new(() =>
    {
        var min = int.MaxValue;
        var max = int.MinValue;
        foreach (var key in s_embedded.Keys)
        {
            if (key.Year < min) min = key.Year;
            if (key.Year > max) max = key.Year;
        }
        return (min, max);
    });

    /// <summary>
    /// 嵌入資料涵蓋的最早西元年份
    /// </summary>
    public int EmbeddedMinYear => s_embeddedYearRange.Value.Min;

    /// <summary>
    /// 嵌入資料涵蓋的最晚西元年份
    /// </summary>
    public int EmbeddedMaxYear => s_embeddedYearRange.Value.Max;

    /// <summary>
    /// 查詢指定日期的假日資訊
    /// </summary>
    internal RocHoliday GetHoliday(RocDateTime date)
    {
        var dt = date.ToDateTime().Date;

        // Layer 1: 使用者手動增刪
        if (_overrides.TryGetValue(dt, out var overrideValue))
        {
            return overrideValue ?? RocHoliday.None;
        }

        // Layer 2: Runtime 下載快取
        if (_cache.TryGetValue(dt.Year, out var yearCache) &&
            yearCache.TryGetValue(dt, out var cachedValue))
        {
            return cachedValue;
        }

        // Layer 3: T4 嵌入資料
        if (s_embedded.TryGetValue(dt, out var embeddedValue))
        {
            return embeddedValue;
        }

        return RocHoliday.None;
    }

    /// <summary>
    /// 查詢指定西元年份是否有資料（嵌入或快取）
    /// </summary>
    public bool ContainsYear(int year)
    {
        if (year >= EmbeddedMinYear && year <= EmbeddedMaxYear)
            return true;

        return _cache.ContainsKey(year);
    }

    /// <summary>
    /// 手動新增或覆寫假日資訊
    /// </summary>
    public void Add(RocDateTime date, RocHoliday holiday)
    {
        var dt = date.ToDateTime().Date;
        _overrides[dt] = holiday;
    }

    /// <summary>
    /// 手動移除假日（標記為非假日）
    /// </summary>
    /// <returns>若該日期已存在於任何資料層，回傳 <c>true</c></returns>
    public bool Remove(RocDateTime date)
    {
        var dt = date.ToDateTime().Date;
        var existed = _overrides.ContainsKey(dt) ||
                      (_cache.TryGetValue(dt.Year, out var yc) && yc.ContainsKey(dt)) ||
                      s_embedded.ContainsKey(dt);

        _overrides[dt] = null; // null = 標記刪除
        return existed;
    }

    /// <summary>
    /// 從遠端下載指定年份的假日資料
    /// </summary>
    /// <remarks>
    /// 下載來源優先順序：
    /// <list type="number">
    /// <item>TaiwanUtilities GitHub Release（合併 CSV，含所有已知年份）</item>
    /// <item>行政院人事行政總處 data.gov.tw（單一年份）</item>
    /// </list>
    /// </remarks>
    public async Task DownloadAsync(int year, CancellationToken ct = default)
    {
        // Primary: GitHub Release（僅首次嘗試，會快取所有年份）
        if (Interlocked.CompareExchange(ref _releaseDownloadAttempted, 1, 0) == 0)
        {
            await TryDownloadFromReleaseAsync(ct).ConfigureAwait(false);
        }

        if (_cache.ContainsKey(year))
            return;

        // Fallback: data.gov.tw
        var holidays = await TryDownloadFromGovAsync(year, ct).ConfigureAwait(false);
        if (holidays.Count > 0)
        {
            _cache[year] = holidays;
        }
    }

    /// <summary>
    /// 重置資料集（清除 Runtime 快取與使用者修改，回到嵌入資料）
    /// </summary>
    public Task ReloadAsync(CancellationToken ct = default)
    {
        _overrides.Clear();
        _cache.Clear();
        Interlocked.Exchange(ref _releaseDownloadAttempted, 0);
        return Task.CompletedTask;
    }

    #region GitHub Release Download

    private const string ReleaseUrl = "https://github.com/Orlys/TaiwanUtilities/releases/latest/download/holidays.csv";

    private async Task TryDownloadFromReleaseAsync(CancellationToken ct)
    {
        try
        {
            var client = HttpClient;

#if NET8_0_OR_GREATER
            var response = await client.GetAsync(ReleaseUrl, ct).ConfigureAwait(false);
#else
            var response = await client.GetAsync(ReleaseUrl, ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode)
                return;

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
                _cache.TryAdd(group.Key, yearDict);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Debug.WriteLine($"Failed to download holidays from GitHub Release: {ex.Message}");
        }
    }

    #endregion

    #region data.gov.tw Download

    private const string GovDatasetApiUrl = "https://data.gov.tw/api/v2/rest/dataset/14718";
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

    private static async Task<Dictionary<DateTime, RocHoliday>> TryDownloadFromGovAsync(int year, CancellationToken ct)
    {
        var result = new Dictionary<DateTime, RocHoliday>();

        try
        {
            var csvUrl = await DiscoverGovCsvUrlAsync(year, ct).ConfigureAwait(false);
            if (csvUrl == null)
                return result;

            var client = HttpClient;

#if NET8_0_OR_GREATER
            var response = await client.GetAsync(csvUrl, ct).ConfigureAwait(false);
#else
            var response = await client.GetAsync(csvUrl, ct).ConfigureAwait(false);
#endif
            if (!response.IsSuccessStatusCode)
                return result;

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
        var response = await client.GetAsync(GovDatasetApiUrl, ct).ConfigureAwait(false);
#else
        var response = await client.GetAsync(GovDatasetApiUrl, ct).ConfigureAwait(false);
#endif
        if (!response.IsSuccessStatusCode)
            return null;

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
                continue;

            var desc = dist.TryGetProperty("resourceDescription", out var d) ? d.GetString() ?? "" : "";
            if (desc.Contains("Google") || desc.Contains("google"))
                continue;

            var match = s_rocYearPattern.Match(desc);
            if (!match.Success)
                continue;

            var csvRocYear = int.Parse(match.Groups[1].Value);
            if (csvRocYear + 1911 != year)
                continue;

            var url = dist.TryGetProperty("resourceDownloadUrl", out var u) ? u.GetString() : null;
            url ??= dist.TryGetProperty("downloadUrl", out var u2) ? u2.GetString() : null;
            if (string.IsNullOrEmpty(url))
                continue;

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
                continue;

            var parts = line.Split(',');
            if (parts.Length < 4)
                continue;

            var dateStr = parts[0].Trim();
            if (dateStr.Length != 8)
                continue;

#if NET8_0_OR_GREATER
            if (!int.TryParse(dateStr.AsSpan(0, 4), out var y) ||
                !int.TryParse(dateStr.AsSpan(4, 2), out var m) ||
                !int.TryParse(dateStr.AsSpan(6, 2), out var d))
                continue;
#else
            if (!int.TryParse(dateStr.Substring(0, 4), out var y) ||
                !int.TryParse(dateStr.Substring(4, 2), out var m) ||
                !int.TryParse(dateStr.Substring(6, 2), out var d))
                continue;
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

            var role = HolidayRole.All;

            // 勞動節
            if (dt.Month == 5 && dt.Day == 1)
            {
                role = HolidayRole.Labor;
                isHoliday = true;
                if (string.IsNullOrEmpty(description)) description = "勞動節";
            }
            // 軍人節
            else if (dt.Month == 9 && dt.Day == 3)
            {
                role = HolidayRole.Soldier;
                isHoliday = true;
                if (string.IsNullOrEmpty(description) || description == "工作日") description = "軍人節";
            }

            if (string.IsNullOrEmpty(description))
            {
                if (isHoliday)
                    s_weekdayNames.TryGetValue((int)dt.DayOfWeek, out description);

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
                continue;

            // Format: "20250101, true , all     , 開國紀念日,"
            var parts = line.Split(',');
            if (parts.Length < 4)
                continue;

            var dateStr = parts[0].Trim();
            if (dateStr.Length != 8)
                continue;

#if NET8_0_OR_GREATER
            if (!int.TryParse(dateStr.AsSpan(0, 4), out var y) ||
                !int.TryParse(dateStr.AsSpan(4, 2), out var m) ||
                !int.TryParse(dateStr.AsSpan(6, 2), out var d))
                continue;
#else
            if (!int.TryParse(dateStr.Substring(0, 4), out var y) ||
                !int.TryParse(dateStr.Substring(4, 2), out var m) ||
                !int.TryParse(dateStr.Substring(6, 2), out var d))
                continue;
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
                _ => HolidayRole.All,
            };

            target[dt] = new RocHoliday(isHoliday, role, description);
        }
    }

    #endregion
}
