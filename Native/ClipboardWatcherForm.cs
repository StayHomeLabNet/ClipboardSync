using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class ClipboardWatcherForm : Form
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_TOGGLE = 1;
    private const int HOTKEY_ID_RECEIVE = 2;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public event EventHandler<string>? ClipboardTextCopied;
    public event EventHandler<bool>? EnabledToggled;
    public event EventHandler? ReceiveHotkeyPressed;

    private string? _lastClipboardText;
    private DateTime _lastEventAtUtc = DateTime.MinValue;
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(350);

    private bool _registeredToggle = false;
    private bool _registeredReceive = false;

    public void ApplyHotkeyFromSettings()
    {
        if (_registeredToggle) { UnregisterHotKey(this.Handle, HOTKEY_ID_TOGGLE); _registeredToggle = false; }
        if (_registeredReceive) { UnregisterHotKey(this.Handle, HOTKEY_ID_RECEIVE); _registeredReceive = false; }

        var s = SettingsStore.Current;
        _registeredToggle = RegisterHotKey(this.Handle, HOTKEY_ID_TOGGLE, s.HotkeyModifiers, s.HotkeyVk);
        _registeredReceive = RegisterHotKey(this.Handle, HOTKEY_ID_RECEIVE, s.ReceiveHotkeyModifiers, s.ReceiveHotkeyVk);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
        Hide();
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_CLIPBOARDUPDATE = 0x031D;
        if (m.Msg == WM_CLIPBOARDUPDATE) _ = HandleClipboardUpdateAsync();
        else if (m.Msg == WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            if (id == HOTKEY_ID_TOGGLE) ToggleEnabled();
            else if (id == HOTKEY_ID_RECEIVE) ReceiveHotkeyPressed?.Invoke(this, EventArgs.Empty);
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
            var now = DateTime.UtcNow;
            if (now - _lastEventAtUtc < Debounce) return;
            if (!Clipboard.ContainsText(TextDataFormat.UnicodeText)) return;

            string text = await TryGetClipboardTextAsync();
            text = (text ?? "").Replace("\r\n", "\n");

            if (string.IsNullOrWhiteSpace(text)) return;
            if (text == _lastClipboardText) return;

            _lastClipboardText = text;
            _lastEventAtUtc = now;
            ClipboardTextCopied?.Invoke(this, text);
        }
        catch { }
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        AddClipboardFormatListener(this.Handle);
        ApplyHotkeyFromSettings();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        UnregisterHotKey(this.Handle, HOTKEY_ID_TOGGLE);
        UnregisterHotKey(this.Handle, HOTKEY_ID_RECEIVE);
        RemoveClipboardFormatListener(this.Handle);
        base.OnHandleDestroyed(e);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}