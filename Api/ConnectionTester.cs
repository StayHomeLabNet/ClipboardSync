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
}