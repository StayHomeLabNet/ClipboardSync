using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class ClipboardWatcherForm : Form
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_TOGGLE = 1;
    private const int HOTKEY_ID_RECEIVE_LATEST = 2;
    private const int HOTKEY_ID_RECEIVE_TEXT = 3;
    private const int HOTKEY_ID_RECEIVE_IMAGE = 4;
    private const int HOTKEY_ID_RECEIVE_FILE = 5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    public event EventHandler<string>? ClipboardTextCopied;
    public event EventHandler<byte[]>? ClipboardImageCopied;
    public event EventHandler<string>? ClipboardFileCopied;
    public event EventHandler<bool>? EnabledToggled;
    public event EventHandler? ReceiveLatestHotkeyPressed;
    public event EventHandler? ReceiveHotkeyPressed;
    public event EventHandler? ReceiveImageHotkeyPressed;
    public event EventHandler? ReceiveFileHotkeyPressed;

    private string? _lastClipboardText;
    private string? _lastImageHash;
    private string? _lastFilePath;
    private DateTime _lastEventAtUtc = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(350);

    private bool _registeredToggle;
    private bool _registeredReceiveLatest;
    private bool _registeredReceive;
    private bool _registeredReceiveImage;
    private bool _registeredReceiveFile;

    public void ApplyHotkeyFromSettings()
    {
        if (_registeredToggle) { UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE); _registeredToggle = false; }
        if (_registeredReceiveLatest) { UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_LATEST); _registeredReceiveLatest = false; }
        if (_registeredReceive) { UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_TEXT); _registeredReceive = false; }
        if (_registeredReceiveImage) { UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_IMAGE); _registeredReceiveImage = false; }
        if (_registeredReceiveFile) { UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_FILE); _registeredReceiveFile = false; }

        var s = SettingsStore.Current;

        _registeredToggle = TryRegisterHotKey(HOTKEY_ID_TOGGLE, s.HotkeyModifiers, s.HotkeyVk);
        _registeredReceiveLatest = TryRegisterHotKey(HOTKEY_ID_RECEIVE_LATEST, s.ReceiveLatestHotkeyModifiers, s.ReceiveLatestHotkeyVk);
        _registeredReceive = TryRegisterHotKey(HOTKEY_ID_RECEIVE_TEXT, s.ReceiveHotkeyModifiers, s.ReceiveHotkeyVk);
        _registeredReceiveImage = TryRegisterHotKey(HOTKEY_ID_RECEIVE_IMAGE, s.ReceiveImageHotkeyModifiers, s.ReceiveImageHotkeyVk);
        _registeredReceiveFile = TryRegisterHotKey(HOTKEY_ID_RECEIVE_FILE, s.ReceiveFileHotkeyModifiers, s.ReceiveFileHotkeyVk);
    }

    private bool TryRegisterHotKey(int id, uint mods, int vk)
    {
        if (mods == 0 || vk == 0)
            return false;

        return RegisterHotKey(Handle, id, mods, vk);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
        Hide();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        AddClipboardFormatListener(Handle);
        ApplyHotkeyFromSettings();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID_TOGGLE);
        UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_LATEST);
        UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_TEXT);
        UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_IMAGE);
        UnregisterHotKey(Handle, HOTKEY_ID_RECEIVE_FILE);
        RemoveClipboardFormatListener(Handle);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_CLIPBOARDUPDATE = 0x031D;

        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            _ = HandleClipboardUpdateAsync();
        }
        else if (m.Msg == WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();

            if (id == HOTKEY_ID_TOGGLE) ToggleEnabled();
            else if (id == HOTKEY_ID_RECEIVE_LATEST) ReceiveLatestHotkeyPressed?.Invoke(this, EventArgs.Empty);
            else if (id == HOTKEY_ID_RECEIVE_TEXT) ReceiveHotkeyPressed?.Invoke(this, EventArgs.Empty);
            else if (id == HOTKEY_ID_RECEIVE_IMAGE) ReceiveImageHotkeyPressed?.Invoke(this, EventArgs.Empty);
            else if (id == HOTKEY_ID_RECEIVE_FILE) ReceiveFileHotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref m);
    }

    private void ToggleEnabled()
    {
        var s = SettingsStore.Current;
        s.Enabled = !s.Enabled;
        SettingsStore.Save(s);
        EnabledToggled?.Invoke(this, s.Enabled);
    }

    private async Task HandleClipboardUpdateAsync()
    {
        try
        {
            if (ClipboardUtil.IsSuppressedNow())
                return;

            var now = DateTime.UtcNow;
            if (now - _lastEventAtUtc < Debounce)
                return;

            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files != null && files.Count > 0)
                {
                    var firstFile = files[0];
                    if (!string.IsNullOrEmpty(firstFile) && File.Exists(firstFile))
                    {
                        if (firstFile == _lastFilePath) return;

                        _lastFilePath = firstFile;
                        _lastEventAtUtc = now;
                        ClipboardFileCopied?.Invoke(this, firstFile);
                        return;
                    }
                }
            }

            if (Clipboard.ContainsImage())
            {
                var imgBytes = await TryGetClipboardImageAsPngAsync();
                if (imgBytes != null && imgBytes.Length > 0)
                {
                    var hash = Convert.ToBase64String(SHA256.HashData(imgBytes));
                    if (hash == _lastImageHash) return;

                    _lastImageHash = hash;
                    _lastEventAtUtc = now;
                    ClipboardImageCopied?.Invoke(this, imgBytes);
                    return;
                }
            }

            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                var text = await TryGetClipboardTextAsync();
                text = (text ?? "").Replace("\r\n", "\n");

                if (string.IsNullOrWhiteSpace(text)) return;
                if (text == _lastClipboardText) return;

                _lastClipboardText = text;
                _lastEventAtUtc = now;
                ClipboardTextCopied?.Invoke(this, text);
            }
        }
        catch
        {
        }
    }

    private static async Task<string> TryGetClipboardTextAsync()
    {
        for (int i = 0; i < 3; i++)
        {
            try { return Clipboard.GetText(TextDataFormat.UnicodeText); }
            catch { await Task.Delay(40); }
        }

        return "";
    }

    private static async Task<byte[]?> TryGetClipboardImageAsPngAsync()
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var img = Clipboard.GetImage();
                if (img == null) return null;

                using (img)
                using (var ms = new MemoryStream())
                {
                    img.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
            catch
            {
                await Task.Delay(40);
            }
        }

        return null;
    }
}
