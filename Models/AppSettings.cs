using System;

internal sealed class AppSettings
{
    // ======= 送信 =======
    public string BaseUrl { get; set; } = "";
    public string TokenEncrypted { get; set; } = "";

    public bool Enabled { get; set; } = true;
    public bool ShowMessageOnSuccess { get; set; } = true;

    public uint HotkeyModifiers { get; set; } = 0x0001 | 0x0002; // Alt|Ctrl
    public int HotkeyVk { get; set; } = 0x4E; // N
    public string HotkeyDisplay { get; set; } = "Ctrl + Alt + N";

    // ======= Cleanup =======
    public string CleanupBaseUrl { get; set; } = "";
    public string CleanupTokenEncrypted { get; set; } = "";
    public bool CleanupPretty { get; set; } = true;

    public bool CleanupDailyEnabled { get; set; } = false;
    public int CleanupDailyHour { get; set; } = 2;
    public int CleanupDailyMinute { get; set; } = 0;

    public bool CleanupEveryEnabled { get; set; } = false;
    public int CleanupEveryMinutes { get; set; } = 60;

    // ======= 受信 =======
    public string ReceiveBaseUrl { get; set; } = "";
    public uint ReceiveHotkeyModifiers { get; set; } = 0x0001 | 0x0002; // Alt|Ctrl
    public int ReceiveHotkeyVk { get; set; } = 0x52; // R
    public string ReceiveHotkeyDisplay { get; set; } = "Ctrl + Alt + R";

    public bool ReceiveAutoPaste { get; set; } = true;
    public int ClipboardStableWaitMs { get; set; } = 60;

    // ======= UI =======
    public string Language { get; set; } = "en"; // "en" / "ja" / "tr"

    // BASIC（共通）
    public string BasicUser { get; set; } = "";
    public string BasicPassEncrypted { get; set; } = "";
}