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

    private const string DisabledHotkeyText = "Disabled";

    private bool _syncingToken = false;
    private readonly System.Windows.Forms.Timer _bakDebounceTimer = new();
    private int _bakQuerying = 0;

    private uint _pendingSendMods;
    private int _pendingSendVk;
    private string _pendingSendDisplay = "";

    private uint _pendingRecvLatestMods;
    private int _pendingRecvLatestVk;
    private string _pendingRecvLatestDisplay = "";

    private uint _pendingRecvMods;
    private int _pendingRecvVk;
    private string _pendingRecvDisplay = "";

    private uint _pendingRecvImgMods;
    private int _pendingRecvImgVk;
    private string _pendingRecvImgDisplay = "";

    private uint _pendingRecvFileMods;
    private int _pendingRecvFileVk;
    private string _pendingRecvFileDisplay = "";

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

        _tokenSend.TextChanged += (_, __) =>
        {
            if (_syncingToken) return;
            _syncingToken = true;
            _tokenRecv.Text = _tokenSend.Text;
            _syncingToken = false;
        };

        _tokenRecv.TextChanged += (_, __) =>
        {
            if (_syncingToken) return;
            _syncingToken = true;
            _tokenSend.Text = _tokenRecv.Text;
            _syncingToken = false;
        };

        _btnHelp.Click += (_, __) => OpenUrlOrShowError(HelpUrl, SafeT("HelpLink", "Help"));
        _lnkWebsite.LinkClicked += (_, __) => OpenUrlOrShowError(WebsiteUrl, SafeT("WebsiteLink", "GitHub"));

        BindHotkeyBox(_hotkeySendBox, HotkeyBox_KeyDown_Send);
        BindHotkeyBox(_hotkeyRecvLatestBox, HotkeyBox_KeyDown_RecvLatest);
        BindHotkeyBox(_hotkeyRecvBox, HotkeyBox_KeyDown_Recv);
        BindHotkeyBox(_hotkeyRecvImageBox, HotkeyBox_KeyDown_RecvImage);
        BindHotkeyBox(_hotkeyRecvFileBox, HotkeyBox_KeyDown_RecvFile);

        _cleanupDailyEnabled.CheckedChanged += (_, __) =>
        {
            if (_cleanupDailyEnabled.Checked) _cleanupEveryEnabled.Checked = false;
            ApplyCleanupUiEnabledState();
        };

        _cleanupEveryEnabled.CheckedChanged += (_, __) =>
        {
            if (_cleanupEveryEnabled.Checked) _cleanupDailyEnabled.Checked = false;
            ApplyCleanupUiEnabledState();
        };

        _btnCopyApiDir.Click += (_, __) => CopyApiDirectoryToOtherUrls();
        _btnTest.Click += async (_, __) => await TestConnectionAsync();
        _btnPurgeBak.Click += async (_, __) => await PurgeBackupsAsync();
        _btnPurgeMedia.Click += async (_, __) => await PurgeMediaAsync();

        _btnSaveSend.Click += (_, __) => SaveAndClose();
        _btnCancelSend.Click += (_, __) => Close();
        _btnSaveRecv.Click += (_, __) => SaveAndClose();
        _btnCancelRecv.Click += (_, __) => Close();

        foreach (var (code, display) in I18n.SupportedLanguages)
            _lang.Items.Add(new LangItem(code, display));

        _lang.SelectedIndexChanged += (_, __) =>
        {
            if (_lang.SelectedItem is LangItem li)
            {
                I18n.SetLanguage(li.Code);
                ApplyLanguageTexts();
                ApplyResponsiveLayout();
                _ = RefreshCountsAsync();
            }
        };

        _bakDebounceTimer.Interval = 650;
        _bakDebounceTimer.Tick += async (_, __) =>
        {
            _bakDebounceTimer.Stop();
            await RefreshCountsAsync();
        };

        _cleanupUrl.TextChanged += (_, __) => RestartCountDebounce();
        _cleanupToken.TextChanged += (_, __) => RestartCountDebounce();
        _chkShowCleanupToken.CheckedChanged += (_, __) => RestartCountDebounce();

        Shown += async (_, __) =>
        {
            ApplyResponsiveLayout();
            await RefreshCountsAsync();
        };

        _tabSend.Resize += (_, __) => ApplyResponsiveLayout();
        _tabReceive.Resize += (_, __) => ApplyResponsiveLayout();
        Resize += (_, __) => ApplyResponsiveLayout();
    }

    private void BindHotkeyBox(TextBox box, KeyEventHandler handler)
    {
        box.KeyDown += handler;
        box.GotFocus += (_, __) => { box.BackColor = System.Drawing.Color.LightYellow; };
        box.LostFocus += (_, __) => { box.BackColor = System.Drawing.SystemColors.Window; };
    }

    private void RestartCountDebounce()
    {
        _bakDebounceTimer.Stop();
        _bakDebounceTimer.Start();
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

        _pendingSendMods = s.HotkeyModifiers;
        _pendingSendVk = s.HotkeyVk;
        _pendingSendDisplay = NormalizeHotkeyDisplay(s.HotkeyModifiers, s.HotkeyVk, s.HotkeyDisplay);
        _hotkeySendBox.Text = _pendingSendDisplay;

        _receiveUrl.Text = s.ReceiveBaseUrl ?? "";
        _tokenRecv.Text = tokenPlain;

        _pendingRecvLatestMods = s.ReceiveLatestHotkeyModifiers;
        _pendingRecvLatestVk = s.ReceiveLatestHotkeyVk;
        _pendingRecvLatestDisplay = NormalizeHotkeyDisplay(s.ReceiveLatestHotkeyModifiers, s.ReceiveLatestHotkeyVk, s.ReceiveLatestHotkeyDisplay);
        _hotkeyRecvLatestBox.Text = _pendingRecvLatestDisplay;

        _pendingRecvMods = s.ReceiveHotkeyModifiers;
        _pendingRecvVk = s.ReceiveHotkeyVk;
        _pendingRecvDisplay = NormalizeHotkeyDisplay(s.ReceiveHotkeyModifiers, s.ReceiveHotkeyVk, s.ReceiveHotkeyDisplay);
        _hotkeyRecvBox.Text = _pendingRecvDisplay;

        _pendingRecvImgMods = s.ReceiveImageHotkeyModifiers;
        _pendingRecvImgVk = s.ReceiveImageHotkeyVk;
        _pendingRecvImgDisplay = NormalizeHotkeyDisplay(s.ReceiveImageHotkeyModifiers, s.ReceiveImageHotkeyVk, s.ReceiveImageHotkeyDisplay);
        _hotkeyRecvImageBox.Text = _pendingRecvImgDisplay;

        _pendingRecvFileMods = s.ReceiveFileHotkeyModifiers;
        _pendingRecvFileVk = s.ReceiveFileHotkeyVk;
        _pendingRecvFileDisplay = NormalizeHotkeyDisplay(s.ReceiveFileHotkeyModifiers, s.ReceiveFileHotkeyVk, s.ReceiveFileHotkeyDisplay);
        _hotkeyRecvFileBox.Text = _pendingRecvFileDisplay;

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
        _lblImgCount.Text = I18n.T("ImgCountNone");
        _lblFileCount.Text = I18n.T("FileCountNone");
    }

    private void SelectLanguage(string code)
    {
        for (int i = 0; i < _lang.Items.Count; i++)
        {
            if (_lang.Items[i] is LangItem li && string.Equals(li.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                _lang.SelectedIndex = i;
                return;
            }
        }

        if (_lang.Items.Count > 0)
            _lang.SelectedIndex = 0;
    }

    private void HotkeyBox_KeyDown_Send(object? sender, KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingSendMods = mods;
        _pendingSendVk = vk;
        _pendingSendDisplay = display;
        _hotkeySendBox.Text = display;
    }

    private void HotkeyBox_KeyDown_RecvLatest(object? sender, KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingRecvLatestMods = mods;
        _pendingRecvLatestVk = vk;
        _pendingRecvLatestDisplay = display;
        _hotkeyRecvLatestBox.Text = display;
    }

    private void HotkeyBox_KeyDown_Recv(object? sender, KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingRecvMods = mods;
        _pendingRecvVk = vk;
        _pendingRecvDisplay = display;
        _hotkeyRecvBox.Text = display;
    }

    private void HotkeyBox_KeyDown_RecvImage(object? sender, KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingRecvImgMods = mods;
        _pendingRecvImgVk = vk;
        _pendingRecvImgDisplay = display;
        _hotkeyRecvImageBox.Text = display;
    }

    private void HotkeyBox_KeyDown_RecvFile(object? sender, KeyEventArgs e)
    {
        if (!TryCaptureHotkey(e, out var mods, out var vk, out var display)) return;
        _pendingRecvFileMods = mods;
        _pendingRecvFileVk = vk;
        _pendingRecvFileDisplay = display;
        _hotkeyRecvFileBox.Text = display;
    }

    private bool TryCaptureHotkey(KeyEventArgs e, out uint mods, out int vk, out string display)
    {
        e.SuppressKeyPress = true;
        mods = 0;
        vk = 0;
        display = "";

        if (!e.Control && !e.Alt && !e.Shift &&
            (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back))
        {
            display = DisabledHotkeyText;
            return true;
        }

        if (e.Control) mods |= MOD_CONTROL;
        if (e.Alt) mods |= MOD_ALT;
        if (e.Shift) mods |= MOD_SHIFT;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey)
            return false;

        if (mods == 0)
        {
            MessageBox.Show(this, I18n.T("NeedModifier"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        vk = (int)e.KeyCode;
        display = BuildHotkeyDisplay(mods, e.KeyCode);
        return true;
    }

    private static string NormalizeHotkeyDisplay(uint mods, int vk, string? display)
    {
        if (mods == 0 || vk == 0)
            return DisabledHotkeyText;

        if (!string.IsNullOrWhiteSpace(display))
            return display;

        return BuildHotkeyDisplay(mods, (Keys)vk);
    }

    private static string BuildHotkeyDisplay(uint mods, Keys key)
    {
        if (mods == 0 || key == Keys.None)
            return DisabledHotkeyText;

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
        _cleanupDailyHour.Enabled = daily;
        _cleanupDailyMinute.Enabled = daily;

        var every = _cleanupEveryEnabled.Checked;
        _cleanupEveryMinutes.Enabled = every;
    }

    private async Task RefreshCountsAsync()
    {
        if (Interlocked.Exchange(ref _bakQuerying, 1) == 1) return;

        try
        {
            if (string.IsNullOrWhiteSpace(_cleanupUrl.Text) || string.IsNullOrWhiteSpace(_cleanupToken.Text))
            {
                _lblBakCount.Text = I18n.T("BakCountNone");
                _lblImgCount.Text = I18n.T("ImgCountNone");
                _lblFileCount.Text = I18n.T("FileCountNone");
                ApplyResponsiveLayout();
                return;
            }

            _lblBakCount.Text = I18n.T("BakCountLoading");
            _lblImgCount.Text = I18n.T("ImgCountLoading");
            _lblFileCount.Text = I18n.T("FileCountLoading");
            ApplyResponsiveLayout();

            var tBak = CleanupApi.GetBackupCountAsync();
            var tImg = CleanupApi.GetImageCountAsync();
            var tFile = CleanupApi.GetFileCountAsync();

            await Task.WhenAll(tBak, tImg, tFile);

            var rBak = tBak.Result;
            var rImg = tImg.Result;
            var rFile = tFile.Result;

            _lblBakCount.Text = rBak.ok && rBak.count >= 0 ? string.Format(I18n.T("BakCountFormat"), rBak.count) : I18n.T("BakCountFail");
            _lblImgCount.Text = rImg.ok && rImg.count >= 0 ? string.Format(I18n.T("ImgCountFormat"), rImg.count) : I18n.T("ImgCountFail");
            _lblFileCount.Text = rFile.ok && rFile.count >= 0 ? string.Format(I18n.T("FileCountFormat"), rFile.count) : I18n.T("FileCountFail");

            ApplyResponsiveLayout();
        }
        finally
        {
            Interlocked.Exchange(ref _bakQuerying, 0);
        }
    }

    private async Task PurgeBackupsAsync()
    {
        var r = MessageBox.Show(I18n.T("ConfirmPurgeBakBody"), I18n.T("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;

        _btnPurgeBak.Enabled = false;
        try
        {
            var (ok, info) = await CleanupApi.PurgeBackupsAsync();
            using var top = NewTopMostHelperForm();
            MessageBox.Show(top, info, ok ? I18n.T("PurgeBakDoneTitle") : I18n.T("PurgeBakFailTitle"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            await RefreshCountsAsync();
        }
        finally
        {
            _btnPurgeBak.Enabled = true;
        }
    }

    private async Task PurgeMediaAsync()
    {
        var r = MessageBox.Show(I18n.T("ConfirmPurgeMediaBody"), I18n.T("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;

        _btnPurgeMedia.Enabled = false;
        try
        {
            var (ok, info) = await CleanupApi.PurgeMediaAsync();
            using var top = NewTopMostHelperForm();
            MessageBox.Show(top, info, ok ? I18n.T("PurgeMediaDoneTitle") : I18n.T("PurgeMediaFailTitle"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            await RefreshCountsAsync();
        }
        finally
        {
            _btnPurgeMedia.Enabled = true;
        }
    }

    private void CopyApiDirectoryToOtherUrls()
    {
        var src = (_url.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(src))
        {
            MessageBox.Show(this, I18n.T("UrlInvalid"), I18n.T("InputErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var apiDir = ExtractApiDirectory(src);
        if (string.IsNullOrWhiteSpace(apiDir))
        {
            MessageBox.Show(this,
                SafeT("CopyApiDirectoryFailed", "Could not detect the /api/ directory from the POST URL."),
                I18n.T("InputErrorTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _receiveUrl.Text = apiDir;
        _cleanupUrl.Text = apiDir;

        MessageBox.Show(this,
            SafeT("CopyApiDirectoryDone", "Copied the API directory to Read API URL and Cleanup API URL."),
            I18n.T("SavedTitle"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static string ExtractApiDirectory(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        url = url.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "";

        var abs = uri.GetLeftPart(UriPartial.Path);

        if (abs.EndsWith("/api.php", StringComparison.OrdinalIgnoreCase))
            return abs[..^"api.php".Length];

        if (abs.EndsWith("/read_api.php", StringComparison.OrdinalIgnoreCase))
            return abs[..^"read_api.php".Length];

        if (abs.EndsWith("/cleanup_api.php", StringComparison.OrdinalIgnoreCase))
            return abs[..^"cleanup_api.php".Length];

        if (abs.EndsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return abs;

        if (abs.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            return abs + "/";

        return abs.EndsWith("/") ? abs : abs + "/";
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

        bool IsInvalidUrl(string u) => !string.IsNullOrWhiteSpace(u) &&
            (!Uri.TryCreate(u, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp));

        if (IsInvalidUrl(urlSend) || IsInvalidUrl(urlRecv) || IsInvalidUrl(cleanupUrl))
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
            BaseUrl = urlSend,
            TokenEncrypted = DpapiHelper.Encrypt(token),
            Enabled = _enabled.Checked,
            ShowMessageOnSuccess = _showSuccess.Checked,
            HotkeyModifiers = _pendingSendMods,
            HotkeyVk = _pendingSendVk,
            HotkeyDisplay = NormalizeHotkeyDisplay(_pendingSendMods, _pendingSendVk, _pendingSendDisplay),

            CleanupBaseUrl = cleanupUrl,
            CleanupTokenEncrypted = DpapiHelper.Encrypt(cleanupToken),
            CleanupDailyEnabled = _cleanupDailyEnabled.Checked,
            CleanupDailyHour = (int)_cleanupDailyHour.Value,
            CleanupDailyMinute = (int)_cleanupDailyMinute.Value,
            CleanupEveryEnabled = _cleanupEveryEnabled.Checked,
            CleanupEveryMinutes = (int)_cleanupEveryMinutes.Value,

            ReceiveBaseUrl = urlRecv,
            ReceiveLatestHotkeyModifiers = _pendingRecvLatestMods,
            ReceiveLatestHotkeyVk = _pendingRecvLatestVk,
            ReceiveLatestHotkeyDisplay = NormalizeHotkeyDisplay(_pendingRecvLatestMods, _pendingRecvLatestVk, _pendingRecvLatestDisplay),

            ReceiveHotkeyModifiers = _pendingRecvMods,
            ReceiveHotkeyVk = _pendingRecvVk,
            ReceiveHotkeyDisplay = NormalizeHotkeyDisplay(_pendingRecvMods, _pendingRecvVk, _pendingRecvDisplay),

            ReceiveImageHotkeyModifiers = _pendingRecvImgMods,
            ReceiveImageHotkeyVk = _pendingRecvImgVk,
            ReceiveImageHotkeyDisplay = NormalizeHotkeyDisplay(_pendingRecvImgMods, _pendingRecvImgVk, _pendingRecvImgDisplay),

            ReceiveFileHotkeyModifiers = _pendingRecvFileMods,
            ReceiveFileHotkeyVk = _pendingRecvFileVk,
            ReceiveFileHotkeyDisplay = NormalizeHotkeyDisplay(_pendingRecvFileMods, _pendingRecvFileVk, _pendingRecvFileDisplay),

            ReceiveAutoPaste = _receiveAutoPaste.Checked,
            ClipboardStableWaitMs = (int)_stableWaitMs.Value,

            Language = langCode,
            BasicUser = basicUser,
            BasicPassEncrypted = DpapiHelper.Encrypt(basicPass),
        });

        MessageBox.Show(this, I18n.T("SavedMsg"), I18n.T("SavedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }

    private static Form NewTopMostHelperForm()
    {
        var top = new Form
        {
            TopMost = true,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(-2000, -2000)
        };
        top.Show();
        return top;
    }

    private sealed class LangItem
    {
        public string Code { get; }
        public string Display { get; }

        public LangItem(string code, string display)
        {
            Code = code;
            Display = display;
        }

        public override string ToString() => Display;
    }
}
