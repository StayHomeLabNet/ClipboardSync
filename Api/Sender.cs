using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal static class Sender
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static string? _lastSent;
    private static DateTime _lastSentAtUtc = DateTime.MinValue;
    private static int _sending = 0;

    public static async Task<(bool ok, string info)> SendAsync(string text)
    {
        if (Interlocked.Exchange(ref _sending, 1) == 1) return (false, "busy");

        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.BaseUrl ?? "").Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "settings: invalid url");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "token is empty or cannot be decrypted");

            text = (text ?? "").Replace("\r\n", "\n");
            if (string.IsNullOrWhiteSpace(text)) return (false, "empty text");

            if (text == _lastSent && DateTime.UtcNow - _lastSentAtUtc < TimeSpan.FromSeconds(2))
                return (false, "duplicate");

            var kv = new List<KeyValuePair<string, string>> { new("token", token), new("text", text) };
            using var content = new FormUrlEncodedContent(kv);
            content.Headers.ContentType!.CharSet = "utf-8";

            using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            _lastSent = text;
            _lastSentAtUtc = DateTime.UtcNow;

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, string.IsNullOrWhiteSpace(body) ? "sent" : body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _sending, 0);
        }
    }
}