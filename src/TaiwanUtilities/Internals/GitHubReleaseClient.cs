namespace TaiwanUtilities.Internals;

using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 共用的 GitHub Release API 查詢工具
/// </summary>
internal static class GitHubReleaseClient
{
    private const string GITHUB_REPOSITORY = "orlys/TaiwanUtilities";

    /// <summary>
    /// 查詢指定 tag 的 release，回傳發布時間和 asset 下載 URL
    /// </summary>
    /// <param name="releaseTag">Release tag 名稱（如 "database-latest"）</param>
    /// <param name="assetFileName">要尋找的 asset 檔案名稱（如 "zipcode.db"）</param>
    /// <param name="ct">取消權杖</param>
    /// <returns>發布時間和下載 URL，若查詢失敗則回傳 null</returns>
    internal static async Task<(string publishedAt, string downloadUrl)?> GetReleaseInfoAsync(
        string releaseTag, string assetFileName, CancellationToken ct)
    {
        try
        {
            var httpClient = SharedHttpClient.Instance;

            var releaseUrl = $"https://api.github.com/repos/{GITHUB_REPOSITORY}/releases/tags/{releaseTag}";
            var response = await httpClient.GetAsync(releaseUrl, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

#if NET8_0_OR_GREATER
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var publishedAt = root.TryGetProperty("published_at", out var pat) ? pat.GetString() : null;

            // 從 assets 陣列中找到符合檔案名稱的下載 URL
            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    if (name == assetFileName &&
                        asset.TryGetProperty("browser_download_url", out var urlProp))
                    {
                        downloadUrl = urlProp.GetString();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(publishedAt) || string.IsNullOrEmpty(downloadUrl))
            {
                return null;
            }

            return (publishedAt, downloadUrl);
        }
        catch (System.Exception ex) when (ex is not System.OutOfMemoryException and not System.StackOverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"GitHubReleaseClient.GetReleaseInfoAsync failed: {ex.Message}");
            return null;
        }
    }
}
