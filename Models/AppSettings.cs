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

    // 最新を受信ホットキー（new）
    // 既存ユーザーと競合しないよう、初期値は Disabled
    public uint ReceiveLatestHotkeyModifiers { get; set; } = 0;
    public int ReceiveLatestHotkeyVk { get; set; } = 0;
    public string ReceiveLatestHotkeyDisplay { get; set; } = "Disabled";

    // テキスト受信用（既存）
    public uint ReceiveHotkeyModifiers { get; set; } = 0x0001 | 0x0002; // Alt|Ctrl
    public int ReceiveHotkeyVk { get; set; } = 0x52; // R
    public string ReceiveHotkeyDisplay { get; set; } = "Ctrl + Alt + R";

    // 画像受信用
    public uint ReceiveImageHotkeyModifiers { get; set; } = 0x0001 | 0x0002; // Alt|Ctrl
    public int ReceiveImageHotkeyVk { get; set; } = 0x49; // I
    public string ReceiveImageHotkeyDisplay { get; set; } = "Ctrl + Alt + I";

    // ファイル受信用
    public uint ReceiveFileHotkeyModifiers { get; set; } = 0x0001 | 0x0002; // Alt|Ctrl
    public int ReceiveFileHotkeyVk { get; set; } = 0x46; // F
    public string ReceiveFileHotkeyDisplay { get; set; } = "Ctrl + Alt + F";

    public bool ReceiveAutoPaste { get; set; } = true;
    public int ClipboardStableWaitMs { get; set; } = 60;

    // ======= UI =======
    public string Language { get; set; } = "en";

    // BASIC認証
    public string BasicUser { get; set; } = "";
    public string BasicPassEncrypted { get; set; } = "";
}
