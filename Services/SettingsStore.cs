using System;
using System.IO;
using System.Text.Json;

internal static class SettingsStore
{
    private static readonly object _lock = new();

    private static readonly string DirPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardUrlSender");

    private static readonly string FilePath = Path.Combine(DirPath, "settings.json");

    public static AppSettings Current { get; private set; } = Load();

    public static event EventHandler<AppSettings>? Saved;

    public static AppSettings Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new AppSettings();

                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("ReceivePasteEnabled", out var p1) &&
                    (p1.ValueKind == JsonValueKind.True || p1.ValueKind == JsonValueKind.False))
                {
                    s.ReceiveAutoPaste = p1.GetBoolean();
                }

                if (root.TryGetProperty("ReceiveClipboardStabilizeWaitMs", out var p2) &&
                    p2.ValueKind == JsonValueKind.Number)
                {
                    s.ClipboardStableWaitMs = p2.GetInt32();
                }

                return s;
            }
            catch
            {
                return new AppSettings();
            }
        }
    }

    public static void Save(AppSettings settings)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(DirPath);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            Current = settings;
        }
        Saved?.Invoke(null, settings);
    }
}