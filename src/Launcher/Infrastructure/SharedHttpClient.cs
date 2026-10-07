namespace Launcher.Infrastructure;

/// <summary>ファビコンと更新処理で接続プールを共有する。ヘッダーは各要求が所有する。</summary>
internal static class SharedHttpClient
{
    internal static HttpClient Instance { get; } = new();

    internal static HttpRequestMessage CreateUpdateRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Launcher-UpdateClient");
        return request;
    }
}
