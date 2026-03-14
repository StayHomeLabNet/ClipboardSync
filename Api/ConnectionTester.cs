using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

internal static class ConnectionTester
{
    public static async Task<(bool ok, string message)> TestPostAsync(string url, string token, string basicUser, string basicPass)
    {
        var testText = "TEST\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            url = NormalizeToApiUrl(url);

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var kv = new List<KeyValuePair<string, string>>
            {
                new("token", token),
                new("text", testText),
            };

            using var content = new FormUrlEncodedContent(kv);
            content.Headers.ContentType!.CharSet = "utf-8";

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            if (!string.IsNullOrWhiteSpace(basicUser))
            {
                var raw = $"{basicUser}:{basicPass}";
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", b64);
            }

            using var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");

            return (true, string.IsNullOrWhiteSpace(body) ? "OK" : body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string NormalizeToApiUrl(string url)
    {
        url = (url ?? "").Trim().TrimEnd('/');

        if (url.EndsWith("/api.php", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.EndsWith("/read_api.php", StringComparison.OrdinalIgnoreCase))
            return url[..^"/read_api.php".Length] + "/api.php";

        if (url.EndsWith("/cleanup_api.php", StringComparison.OrdinalIgnoreCase))
            return url[..^"/cleanup_api.php".Length] + "/api.php";

        if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            return url + "/api.php";

        return url + "/api.php";
    }
}
