using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed partial class SettingsForm : Form
{
    private const string HelpUrl = "https://stayhomelab.net/ClipboardSync";
    private const string WebsiteUrl = "https://github.com/StayHomeLabNet/ClipboardSync";

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    private bool _syncingToken = false;
    private readonly System.Windows.Forms.Timer _bakDebounceTimer = new();
    private int _bakQuerying = 0;

    private uint _pendingSendMods;
    private int _pendingSendVk;
    private string _pendingSendDisplay = "";

    private uint _pendingRecvMods;
    private int _pendingRecvVk;
    private string _pendingRecvDisplay = "";

    public SettingsForm()
    {
        InitializeUI();
        SubscribeEvents();
        LoadFromSettings();
        ApplyResponsiveLayout();
    }

    private void SubscribeEvents()
    {
        _chkShowTokenSend.CheckedChanged += (_, __) => _tokenSend.UseSystemPasswordChar = !_chkShowTokenSend.Checked;
        _chkShowTokenRecv.CheckedChanged += (_, __) => _tokenRecv.UseSystemPasswordChar = !_chkShowTokenRecv.Checked;
        _chkShowCleanupToken.CheckedChanged += (_, __) => _cleanupToken.UseSystemPasswordChar = !_chkShowCleanupToken.Checked;
        _chkShowBasicPass.CheckedChanged += (_, __) => _basicPass.UseSystemPasswordChar = !_chkShowBasicPass.Checked;

        _tokenSend.TextChanged += (_, __) => { if (_syncingToken) return; _syncingToken = true; _tokenRecv.Text = _tokenSend.Text; _syncingToken = false; };
        _tokenRecv.TextChanged += (_, __) => { if (_syncingToken) return; _syncingToken = true; _tokenSend.Text = _tokenRecv.Text; _syncingToken = false; };

        _btnHelp.Click += (_, __) => OpenUrlOrShowError(HelpUrl, SafeT("HelpLink", "Help"));
        _lnkWebsite.LinkClicked += (_, __) => OpenUrlOrShowError(WebsiteUrl, SafeT("WebsiteLink", "GitHub"));

        _hotkeySendBox.KeyDown += (_, e) => HotkeyBox_KeyDown_Send(e);
        _hotkeySendBox.GotFocus += (_, __) => { _hotkeySendBox.BackColor = System.Drawing.Color.LightYellow; };
        _hotkeySendBox.LostFocus += (_, __) => { _hotkeySendBox.BackColor = System.Drawing.SystemColors.Window; };

        _hotkeyRecvBox.KeyDown += (_, e) => HotkeyBox_KeyDown_Recv(e);
        _hotkeyRecvBox.GotFocus += (_, __) => { _hotkeyRecvBox.BackColor = System.Drawing.Color.LightYellow; };
        _hotkeyRecvBox.LostFocus += (_, __) => { _hotkeyRecvBox.BackColor = System.Drawing.SystemColors.Window; };

        _cleanupDailyEnabled.CheckedChanged += (_, __) => { if (_cleanupDailyEnabled.Checked) _cleanupEveryEnabled.Checked = false; ApplyCleanupUiEnabledState(); };
        _cleanupEveryEnabled.CheckedChanged += (_, __) => { if (_cleanupEveryEnabled.Checked) _cleanupDailyEnabled.Checked = false; ApplyCleanupUiEnabledState(); };

        _btnTest.Click += async (_, __) => await TestConnectionAsync();
        _btnPurgeBak.Click += async (_, __) => await PurgeBackupsAsync();

        _btnSaveSend.Click += (_, __) => SaveAndClose();
        _btnCancelSend.Click += (_, __) => Close();
        _btnSaveRecv.Click += (_, __) => SaveAndClose();
        _btnCancelRecv.Click += (_, __) => Close();

        foreach (var (code, display) in I18n.SupportedLanguages) _lang.Items.Add(new LangItem(code, display));

        _lang.SelectedIndexChanged += (_, __) =>
        {
            if (_lang.SelectedItem is LangItem li)
            {
                I18n.SetLanguage(li.Code);
                ApplyLanguageTexts();
                ApplyResponsiveLayout();
                _ = RefreshBackupCountAsync();
            }
        };

        _bakDebounceTimer.Interval = 650;
        _bakDebounceTimer.Tick += async (_, __) => { _bakDebounceTimer.Stop(); await RefreshBackupCountAsync(); };

        _cleanupUrl.TextChanged += (_, __) => { _bakDebounceTimer.Stop(); _bakDebounceTimer.Start(); };
        _cleanupToken.TextChanged += (_, __) => { _bakDebounceTimer.Stop(); _bakDebounceTimer.Start(); };
        _chkShowCleanupToken.CheckedChanged += (_, __) => { _bakDebounceTimer.Stop(); _bakDebounceTimer.Start(); };

        Shown += async (_, __) => { ApplyResponsiveLayout(); await RefreshBackupCountAsync(); };
        _tabSend.Resize += (_, __) => ApplyResponsiveLayout();
        _tabReceive.Resize += (_, __) => ApplyResponsiveLayout();
        Resize += (_, __) => ApplyResponsiveLayout();
    }

    private void OpenUrlOrShowError(string url, string title)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void LoadFromSettings()
    {
        var s = SettingsStore.Current;

        var code = string.IsNullOrWhiteSpace(s.Language) ? "en" : s.Language;
        SelectLanguage(code);

        _url.Text = s.BaseUrl ?? "";
        var tokenPlain = DpapiHelper.Decrypt(s.TokenEncrypted) ?? "";
        _tokenSend.Text = tokenPlain;
        _enabled.Checked = s.Enabled;
        _showSuccess.Checked = s.ShowMessageOnSuccess;

        _pendingSendMods = s.HotkeyModifiers; _pendingSendVk = s.HotkeyVk; _pendingSendDisplay = s.HotkeyDisplay;
        _hotkeySendBox.Text = string.IsNullOrWhiteSpace(_pendingSendDisplay) ? BuildHotkeyDisplay(_pendingSendMods, (Keys)_pendingSendVk) : _pendingSendDisplay;

        _receiveUrl.Text = s.ReceiveBaseUrl ?? "";
        _tokenRecv.Text = tokenPlain;
        _pendingRecvMods = s.ReceiveHotkeyModifiers; _pendingRecvVk = s.ReceiveHotkeyVk; _pendingRecvDisplay = s.ReceiveHotkeyDisplay;
        _hotkeyRecvBox.Text = string.IsNullOrWhiteSpace(_pendingRecvDisplay) ? BuildHotkeyDisplay(_pendingRecvMods, (Keys)_pendingRecvVk) : _pendingRecvDisplay;

        _receiveAutoPaste.Checked = s.ReceiveAutoPaste;
        var wait = Math.Max((int)_stableWaitMs.Minimum, Math.Min(s.ClipboardStableWaitMs, (int)_stableWaitMs.Maximum));
        _stableWaitMs.Value = wait;

        _basicUser.Text = s.BasicUser ?? "";
        _basicPass.Text = DpapiHelper.Decrypt(s.BasicPassEncrypted) ?? "";

        _cleanupUrl.Text = s.CleanupBaseUrl ?? "";
        _cleanupToken.Text = DpapiHelper.Decrypt(s.CleanupTokenEncrypted) ?? "";
        _cleanupDailyEnabled.Checked = s.CleanupDailyEnabled;
        _cleanupDailyHour.Value = s.CleanupDailyHour;
        _cleanupDailyMinute.Value = s.CleanupDailyMinute;
        _cleanupEveryEnabled.Checked = s.CleanupEveryEnabled;
        _cleanupEveryMinutes.Value = s.CleanupEveryMinutes;

        ApplyCleanupUiEnabledState();
        ApplyLanguageTexts();
        _lblBakCount.Text = I18n.T("BakCountNone");
    }

    private void SelectLanguage(string code)
    {
        for (int i = 0; i < _lang.Items.Count; i++)
            if (_lang.Items[i] is LangItem li && string.Equals(li.Code, code, StringComparison.OrdinalIgnoreCase))
            { _lang.SelectedIndex = i; return; }
        if (_lang.Items.Count > 0) _lang.SelectedIndex = 0;
    }

    private void HotkeyBox_KeyDown_Send(KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingSendMods = mods; _pendingSendVk = vk; _pendingSendDisplay = display;
        _hotkeySendBox.Text = display;
    }

    private void HotkeyBox_KeyDown_Recv(KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingRecvMods = mods; _pendingRecvVk = vk; _pendingRecvDisplay = display;
        _hotkeyRecvBox.Text = display;
    }

    private bool TryCaptureHotkey(KeyEventArgs e, out uint mods, out int vk, out string display)
    {
        e.SuppressKeyPress = true; mods = 0; display = ""; vk = 0;
        if (e.Control) mods |= MOD_CONTROL;
        if (e.Alt) mods |= MOD_ALT;
        if (e.Shift) mods |= MOD_SHIFT;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey) return false;
        if (mods == 0)
        {
            MessageBox.Show(this, I18n.T("NeedModifier"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        vk = (int)e.KeyCode;
        display = BuildHotkeyDisplay(mods, e.KeyCode);
        return true;
    }

    private static string BuildHotkeyDisplay(uint mods, Keys key)
    {
        var parts = new List<string>();
        if ((mods & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((mods & MOD_ALT) != 0) parts.Add("Alt");
        if ((mods & MOD_SHIFT) != 0) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    private async Task TestConnectionAsync()
    {
        var url = (_url.Text ?? "").Trim();
        var token = (_tokenSend.Text ?? "").Trim();
        var basicUser = (_basicUser.Text ?? "").Trim();
        var basicPass = (_basicPass.Text ?? "");

        if (string.IsNullOrWhiteSpace(url) || (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
        {
            MessageBox.Show(this, I18n.T("UrlInvalid"), I18n.T("TestConnection"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show(this, I18n.T("TokenEmpty"), I18n.T("TestConnection"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!string.IsNullOrWhiteSpace(basicUser) && string.IsNullOrEmpty(basicPass))
        {
            MessageBox.Show(this, I18n.T("BasicIncomplete"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var (ok, message) = await ConnectionTester.TestPostAsync(url, token, basicUser, basicPass);
        MessageBox.Show(this, message, I18n.T("TestConnection"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void ApplyCleanupUiEnabledState()
    {
        var daily = _cleanupDailyEnabled.Checked;
        _cleanupDailyHour.Enabled = daily; _cleanupDailyMinute.Enabled = daily;
        var every = _cleanupEveryEnabled.Checked;
        _cleanupEveryMinutes.Enabled = every;
    }

    private async Task RefreshBackupCountAsync()
    {
        if (Interlocked.Exchange(ref _bakQuerying, 1) == 1) return;
        try
        {
            if (string.IsNullOrWhiteSpace(_cleanupUrl.Text) || string.IsNullOrWhiteSpace(_cleanupToken.Text))
            {
                _lblBakCount.Text = I18n.T("BakCountNone"); ApplyResponsiveLayout(); return;
            }

            _lblBakCount.Text = I18n.T("BakCountLoading"); ApplyResponsiveLayout();
            var (ok, count, _) = await CleanupApi.GetBackupCountAsync();
            _lblBakCount.Text = ok && count >= 0 ? string.Format(I18n.T("BakCountFormat"), count) : I18n.T("BakCountFail");
            ApplyResponsiveLayout();
        }
        finally { Interlocked.Exchange(ref _bakQuerying, 0); }
    }

    private async Task PurgeBackupsAsync()
    {
        var r = MessageBox.Show(I18n.T("ConfirmPurgeBakBody"), I18n.T("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;

        _btnPurgeBak.Enabled = false;
        try
        {
            var (ok, info) = await CleanupApi.PurgeBackupsAsync();
            using var top = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
            top.Show();

            MessageBox.Show(top, info, ok ? I18n.T("PurgeBakDoneTitle") : I18n.T("PurgeBakFailTitle"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            await RefreshBackupCountAsync();
        }
        finally { _btnPurgeBak.Enabled = true; }
    }

    private void SaveAndClose()
    {
        var urlSend = (_url.Text ?? "").Trim();
        var urlRecv = (_receiveUrl.Text ?? "").Trim();
        var token = (_tokenSend.Text ?? "").Trim();
        var cleanupUrl = (_cleanupUrl.Text ?? "").Trim();
        var cleanupToken = (_cleanupToken.Text ?? "").Trim();
        var basicUser = (_basicUser.Text ?? "").Trim();
        var basicPass = (_basicPass.Text ?? "");

        bool isInvalidUrl(string u) => !string.IsNullOrWhiteSpace(u) && (!Uri.TryCreate(u, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp));

        if (isInvalidUrl(urlSend) || isInvalidUrl(urlRecv) || isInvalidUrl(cleanupUrl))
        {
            MessageBox.Show(this, I18n.T("UrlInvalid"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if ((!string.IsNullOrWhiteSpace(urlSend) || !string.IsNullOrWhiteSpace(urlRecv)) && string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show(this, I18n.T("TokenEmpty"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(cleanupUrl) && string.IsNullOrWhiteSpace(cleanupToken))
        {
            MessageBox.Show(this, I18n.T("TokenEmpty"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(basicUser) && string.IsNullOrEmpty(basicPass))
        {
            MessageBox.Show(this, I18n.T("BasicIncomplete"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var langCode = (_lang.SelectedItem as LangItem)?.Code ?? "en";

        SettingsStore.Save(new AppSettings
        {
            BaseUrl = urlSend, TokenEncrypted = DpapiHelper.Encrypt(token),
            Enabled = _enabled.Checked, ShowMessageOnSuccess = _showSuccess.Checked,
            HotkeyModifiers = _pendingSendMods, HotkeyVk = _pendingSendVk, HotkeyDisplay = _pendingSendDisplay,

            CleanupBaseUrl = cleanupUrl, CleanupTokenEncrypted = DpapiHelper.Encrypt(cleanupToken),
            CleanupDailyEnabled = _cleanupDailyEnabled.Checked, CleanupDailyHour = (int)_cleanupDailyHour.Value, CleanupDailyMinute = (int)_cleanupDailyMinute.Value,
            CleanupEveryEnabled = _cleanupEveryEnabled.Checked, CleanupEveryMinutes = (int)_cleanupEveryMinutes.Value,

            ReceiveBaseUrl = urlRecv, ReceiveHotkeyModifiers = _pendingRecvMods, ReceiveHotkeyVk = _pendingRecvVk, ReceiveHotkeyDisplay = _pendingRecvDisplay,
            ReceiveAutoPaste = _receiveAutoPaste.Checked, ClipboardStableWaitMs = (int)_stableWaitMs.Value,

            Language = langCode, BasicUser = basicUser, BasicPassEncrypted = DpapiHelper.Encrypt(basicPass),
        });

        MessageBox.Show(this, I18n.T("SavedMsg"), I18n.T("SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private sealed class LangItem
    {
        public string Code { get; }
        public string Display { get; }
        public LangItem(string code, string display) { Code = code; Display = display; }
        public override string ToString() => Display;
    }
}