using System;
using System.Net.Http;
using System.Threading.Tasks;

internal static class Receiver
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<(bool ok, string infoOrText)> FetchLatestNoteTextAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.ReceiveBaseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseUrl)) return (false, "Receive (Read API) URL is empty");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "Receive (Read API) URL is invalid");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "Token is empty or cannot be decrypted");

            var readUrl = NormalizeReadApiUrl(baseUrl);
            var url = AppendQuery(readUrl, "token", token);
            url = AppendQuery(url, "action", "latest_note");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, body ?? "");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string NormalizeReadApiUrl(string url)
    {
        if (url.EndsWith("/api.php", StringComparison.OrdinalIgnoreCase))
            return url.Substring(0, url.Length - "/api.php".Length) + "/read_api.php";
        if (url.EndsWith("/api/", StringComparison.OrdinalIgnoreCase)) return url + "read_api.php";
        if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase)) return url + "/read_api.php";
        return url;
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var sep = url.Contains("?") ? "&" : "?";
        return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
    }
}