namespace Launcher.Infrastructure;

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
