using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

internal static class CleanupApi
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<(bool ok, string info)> DeleteInboxAllAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.CleanupBaseUrl ?? "").Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "Cleanup_API URL is not set or invalid");

            var token = (DpapiHelper.Decrypt(s.CleanupTokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "Cleanup_API token is not set / cannot be decrypted");

            var url = baseUrl;
            if (s.CleanupPretty) url = AppendQuery(baseUrl, "pretty", "1");

            var kv = new List<KeyValuePair<string, string>> { new("token", token), new("confirm", "YES"), new("category", "INBOX") };
            using var content = new FormUrlEncodedContent(kv);
            content.Headers.ContentType!.CharSet = "utf-8";

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, string.IsNullOrWhiteSpace(body) ? "OK (empty response)" : body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool ok, int count, string info)> GetBackupCountAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.CleanupBaseUrl ?? "").Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, -1, "Cleanup_API URL is not set or invalid");

            var token = (DpapiHelper.Decrypt(s.CleanupTokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, -1, "Cleanup_API token is not set / cannot be decrypted");

            var url = baseUrl;
            if (s.CleanupPretty) url = AppendQuery(baseUrl, "pretty", "1");

            var kv = new List<KeyValuePair<string, string>> { new("token", token), new("purge_bak", "1"), new("dry_run", "2") };
            using var content = new FormUrlEncodedContent(kv);
            content.Headers.ContentType!.CharSet = "utf-8";

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode) return (false, -1, $"HTTP {(int)res.StatusCode}\n\n{body}");

            var count = TryParseCount(body, out var parsed) ? parsed : -1;
            if (count < 0) return (false, -1, string.IsNullOrWhiteSpace(body) ? "count not found" : body);

            return (true, count, body ?? "");
        }
        catch (Exception ex)
        {
            return (false, -1, ex.Message);
        }
    }

    public static async Task<(bool ok, string info)> PurgeBackupsAsync()
    {
        try
        {
            var s = SettingsStore.Current;
            var baseUrl = (s.CleanupBaseUrl ?? "").Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var _)) return (false, "Cleanup_API URL is not set or invalid");

            var token = (DpapiHelper.Decrypt(s.CleanupTokenEncrypted) ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return (false, "Cleanup_API token is not set / cannot be decrypted");

            var url = baseUrl;
            if (s.CleanupPretty) url = AppendQuery(baseUrl, "pretty", "1");

            var kv = new List<KeyValuePair<string, string>> { new("token", token), new("purge_bak", "1"), new("confirm", "YES") };
            using var content = new FormUrlEncodedContent(kv);
            content.Headers.ContentType!.CharSet = "utf-8";

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            BasicAuth.Apply(req, s);

            using var res = await Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}\n\n{body}");
            return (true, string.IsNullOrWhiteSpace(body) ? "OK (empty response)" : body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static bool TryParseCount(string? body, out int count)
    {
        count = -1;
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number)
            {
                count = c.GetInt32();
                return true;
            }
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object &&
                    prop.Value.TryGetProperty("count", out var cc) && cc.ValueKind == JsonValueKind.Number)
                {
                    count = cc.GetInt32();
                    return true;
                }
            }
        }
        catch { }

        try
        {
            var idx = body.IndexOf("\"count\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var colon = body.IndexOf(':', idx);
            if (colon < 0) return false;

            int i = colon + 1;
            while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
            int start = i;
            while (i < body.Length && char.IsDigit(body[i])) i++;
            if (i <= start) return false;

            var num = body.Substring(start, i - start);
            if (int.TryParse(num, out var n)) { count = n; return true; }
        }
        catch { }
        return false;
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var sep = url.Contains("?") ? "&" : "?";
        return url + sep + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value);
    }
}