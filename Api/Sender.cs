using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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

            var url = AddUserQuery(baseUrl, s.DirUserName);
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
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
            content.Add(new StringContent(token, Encoding.UTF8), "token");
            content.Add(new StringContent("image", Encoding.UTF8), "type");

            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            content.Add(fileContent, "image", "clipboard.png");

            var url = AddUserQuery(baseUrl, s.DirUserName);
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
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

            var fileName = Path.GetFileName(filePath);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(token, Encoding.UTF8), "token");
            content.Add(new StringContent("file", Encoding.UTF8), "type");

            // 文字化け対策:
            // filename= だけに頼らず、UTF-8 の original_name を別フォーム項目として明示送信する
            // api.php 側では $_POST['original_name'] を優先して file_index.json に保存すること
            content.Add(new StringContent(fileName, Encoding.UTF8), "original_name");

            // Excel や他アプリがファイルを掴んでいる場合でも、可能な限り読み取れるよう共有モードを広げる
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeTypeFromFileName(fileName));
            content.Add(fileContent, "file", fileName);

            var url = AddUserQuery(baseUrl, s.DirUserName);
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
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

    private static string AddUserQuery(string url, string? dirUserName)
    {
        var dirUser = (dirUserName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(dirUser)) return url;
        return AppendQuery(url, "user", dirUser);
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

    private static string GetMimeTypeFromFileName(string fileName)
    {
        var ext = (Path.GetExtension(fileName) ?? "").Trim().ToLowerInvariant();

        return ext switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var sep = url.Contains("?") ? "&" : "?";
        return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
    }
}
