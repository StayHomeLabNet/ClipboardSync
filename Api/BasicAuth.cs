using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

internal static class BasicAuth
{
    public static void Apply(HttpRequestMessage req, AppSettings s)
    {
        var user = (s.BasicUser ?? "").Trim();
        if (string.IsNullOrEmpty(user)) return;

        var pass = DpapiHelper.Decrypt(s.BasicPassEncrypted) ?? "";
        var raw = $"{user}:{pass}";
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", b64);
    }
}