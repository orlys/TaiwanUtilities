namespace TaiwanUtilities.Internals;

using System;
using System.Net.Http;

/// <summary>
/// 專案共用的 HttpClient 單例，避免每次操作建立新的 HttpClient 實例
/// </summary>
internal static class SharedHttpClient
{
    private static readonly Lazy<HttpClient> s_instance = new(() =>
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TaiwanUtilities/1.0");
        return client;
    });

    /// <summary>
    /// 取得共用的 HttpClient 實例
    /// </summary>
    internal static HttpClient Instance => s_instance.Value;
}