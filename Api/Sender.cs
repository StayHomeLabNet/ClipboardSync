using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

internal static class Sender
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) }; // ファイル用にタイムアウト少し長め
    private static string? _lastSent;
    private static DateTime _lastSentAtUtc = DateTime.MinValue;
    private static int _sending = 0;

    public static async Task<(bool ok, string info)> SendAsync(string text)
    {
        if (Interlocked.Exchange(ref _sending, 1) == 1) return (false, "busy");

        try
        {
            var s = SettingsStore.Current;
            var baseUrl = NormalizeToApiUrl((s.BaseUrl ?? "").Trim());
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

    public static async Task<(bool ok, string info)> SendImageAsync(byte[] imageBytes)
    {
        if (Interlocked.Exchange(ref _sending, 1) == 1) return (false, "busy");

        try
        {
            var s = SettingsStore.Current;
            var baseUrl = NormalizeToApiUrl((s.BaseUrl ?? "").Trim());
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "settings: invalid url");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "token is empty or cannot be decrypted");

            if (imageBytes == null || imageBytes.Length == 0) return (false, "empty image");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(token), "token");
            content.Add(new StringContent("image"), "type");

            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            content.Add(fileContent, "image", "clipboard.png");

            using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            _lastSentAtUtc = DateTime.UtcNow;

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, string.IsNullOrWhiteSpace(body) ? "image sent" : body);
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

    public static async Task<(bool ok, string info)> SendFileAsync(string filePath)
    {
        if (Interlocked.Exchange(ref _sending, 1) == 1) return (false, "busy");

        try
        {
            var s = SettingsStore.Current;
            var baseUrl = NormalizeToApiUrl((s.BaseUrl ?? "").Trim());
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "settings: invalid url");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "token is empty or cannot be decrypted");

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return (false, "file not found");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(token), "token");
            content.Add(new StringContent("file"), "type");

            var fileContent = new StreamContent(File.OpenRead(filePath));
            var fileName = Path.GetFileName(filePath);
            content.Add(fileContent, "file", fileName);

            using var req = new HttpRequestMessage(HttpMethod.Post, baseUrl) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            _lastSentAtUtc = DateTime.UtcNow;

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, string.IsNullOrWhiteSpace(body) ? "file sent" : body);
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
