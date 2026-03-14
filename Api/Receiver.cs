using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

internal static class Receiver
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<(bool ok, string type, string info)> FetchLatestClipTypeAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.ReceiveBaseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseUrl)) return (false, "", "Receive (Read API) URL is empty");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "", "Receive (Read API) URL is invalid");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "", "Token is empty or cannot be decrypted");

            var readUrl = NormalizeToReadApiUrl(baseUrl);
            var url = AppendQuery(readUrl, "token", token);
            url = AppendQuery(url, "action", "latest_clip_type");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode) return (false, "", $"HTTP {(int)res.StatusCode}\n\n{body}");

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var type = root.TryGetProperty("type", out var typeProp)
                    ? (typeProp.GetString() ?? "").Trim().ToLowerInvariant()
                    : "";

                if (type == "text")
                    type = "note";

                return (true, type, "OK");
            }
            catch (Exception ex)
            {
                return (false, "", $"latest_clip_type JSON parse failed\n\n{ex.Message}\n\n{body}");
            }
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

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

            var readUrl = NormalizeToReadApiUrl(baseUrl);
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

    public static async Task<(bool ok, byte[]? imageBytes, string info)> FetchLatestImageAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.ReceiveBaseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseUrl)) return (false, null, "Receive (Read API) URL is empty");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, null, "Receive (Read API) URL is invalid");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, null, "Token is empty or cannot be decrypted");

            var readUrl = NormalizeToReadApiUrl(baseUrl);
            var url = AppendQuery(readUrl, "token", token);
            url = AppendQuery(url, "action", "latest_image");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);

            if (!res.IsSuccessStatusCode)
            {
                var errBody = await res.Content.ReadAsStringAsync();
                return (false, null, $"HTTP {(int)res.StatusCode}\n\n{errBody}");
            }

            var bytes = await res.Content.ReadAsByteArrayAsync();
            if (bytes == null || bytes.Length == 0) return (true, null, "empty response");

            return (true, bytes, "OK");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public static async Task<(bool ok, string? filePath, string info)> FetchLatestFileAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.ReceiveBaseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseUrl)) return (false, null, "Receive (Read API) URL is empty");
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, null, "Receive (Read API) URL is invalid");

            var token = (DpapiHelper.Decrypt(s.TokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, null, "Token is empty or cannot be decrypted");

            var readUrl = NormalizeToReadApiUrl(baseUrl);

            string originalName = "downloaded_file.bin";
            var infoUrl = AppendQuery(readUrl, "token", token);
            infoUrl = AppendQuery(infoUrl, "action", "latest_clip_type");

            using (var infoReq = new HttpRequestMessage(HttpMethod.Get, infoUrl))
            {
                BasicAuth.Apply(infoReq, s);
                using var infoRes = await Client.SendAsync(infoReq);
                if (infoRes.IsSuccessStatusCode)
                {
                    var infoBody = await infoRes.Content.ReadAsStringAsync();
                    try
                    {
                        using var doc = JsonDocument.Parse(infoBody);
                        var root = doc.RootElement;
                        var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "";

                        if (string.Equals(type, "file", StringComparison.OrdinalIgnoreCase) &&
                            root.TryGetProperty("file", out var fileObj) &&
                            fileObj.TryGetProperty("original_name", out var nameProp))
                        {
                            var name = nameProp.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                originalName = name;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            var url = AppendQuery(readUrl, "token", token);
            url = AppendQuery(url, "action", "latest_file");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

            if (!res.IsSuccessStatusCode)
            {
                var errBody = await res.Content.ReadAsStringAsync();
                return (false, null, $"HTTP {(int)res.StatusCode}\n\n{errBody}");
            }

            var tempRoot = Path.Combine(Path.GetTempPath(), "ClipboardSync", "recv");
            Directory.CreateDirectory(tempRoot);

            var sessionDir = Path.Combine(
                tempRoot,
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N")[..8]);

            Directory.CreateDirectory(sessionDir);

            var safeFileName = string.Join("_", originalName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(safeFileName))
                safeFileName = "downloaded_file.bin";

            var filePath = Path.Combine(sessionDir, safeFileName);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await res.Content.CopyToAsync(fs);
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                File.Delete(filePath);
                return (true, null, "empty response");
            }

            return (true, filePath, "OK");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private static string NormalizeToReadApiUrl(string url)
    {
        url = (url ?? "").Trim().TrimEnd('/');

        if (url.EndsWith("/read_api.php", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.EndsWith("/api.php", StringComparison.OrdinalIgnoreCase))
            return url[..^"/api.php".Length] + "/read_api.php";

        if (url.EndsWith("/cleanup_api.php", StringComparison.OrdinalIgnoreCase))
            return url[..^"/cleanup_api.php".Length] + "/read_api.php";

        if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            return url + "/read_api.php";

        return url + "/read_api.php";
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var sep = url.Contains("?") ? "&" : "?";
        return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
    }
}
