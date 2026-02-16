using System;
using System.Reflection;
using System.Windows.Forms;

internal sealed partial class SettingsForm : Form
{
    // ======= Tabs =======
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage _tabSend = new();
    private readonly TabPage _tabReceive = new();

    // ======= 共通 =======
    private readonly TextBox _tokenSend = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
    private readonly CheckBox _chkShowTokenSend = new() { AutoSize = true, Checked = false };
    private readonly TextBox _tokenRecv = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
    private readonly CheckBox _chkShowTokenRecv = new() { AutoSize = true, Checked = false };

    // ======= 送信タブ =======
    private readonly Label _lblUrl = new() { AutoSize = true, Left = 12, Top = 16 };
    private readonly TextBox _url = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _lblTokenSend = new() { AutoSize = true, Left = 12 };
    private readonly CheckBox _enabled = new() { AutoSize = true };
    private readonly CheckBox _showSuccess = new() { AutoSize = true };
    private readonly Button _btnTest = new() { Width = 120, Height = 30 };
    private readonly Label _lblHotkeySend = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _hotkeySendBox = new() { ReadOnly = true, TabStop = true };

    // BASIC
    private readonly Label _lblBasic = new() { AutoSize = true, Left = 12 };
    private readonly Label _lblBasicUser = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _basicUser = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _lblBasicPass = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _basicPass = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
    private readonly CheckBox _chkShowBasicPass = new() { AutoSize = true, Checked = false };

    // Cleanup
    private readonly Label _lblCleanup = new() { AutoSize = true, Left = 12 };
    private readonly Label _lblCleanupUrl = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _cleanupUrl = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _lblCleanupToken = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _cleanupToken = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
    private readonly CheckBox _chkShowCleanupToken = new() { AutoSize = true, Checked = false };

    // 自動削除
    private readonly CheckBox _cleanupDailyEnabled = new() { AutoSize = true };
    private readonly NumericUpDown _cleanupDailyHour = new() { Minimum = 0, Maximum = 23, Width = 60 };
    private readonly NumericUpDown _cleanupDailyMinute = new() { Minimum = 0, Maximum = 59, Width = 60 };
    private readonly Label _lblDailyTime = new() { AutoSize = true, Left = 30 };
    private readonly Label _lblH = new() { AutoSize = true };
    private readonly Label _lblM = new() { AutoSize = true };
    private readonly CheckBox _cleanupEveryEnabled = new() { AutoSize = true };
    private readonly NumericUpDown _cleanupEveryMinutes = new() { Minimum = 1, Maximum = 1440, Width = 80 };
    private readonly Label _lblEvery = new() { AutoSize = true, Left = 30 };

    // バックアップ
    private readonly Label _lblBakCount = new() { AutoSize = true };
    private readonly Button _btnPurgeBak = new() { Width = 200, Height = 30 };

    // 言語
    private readonly Label _lblLang = new() { AutoSize = true, Left = 12 };
    private readonly ComboBox _lang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };

    // ボタン類
    private readonly Button _btnSaveSend = new() { Width = 120, Height = 30 };
    private readonly Button _btnCancelSend = new() { Width = 120, Height = 30 };

    // ======= 受信タブ =======
    private readonly Label _lblReceiveUrl = new() { AutoSize = true, Left = 12, Top = 16 };
    private readonly TextBox _receiveUrl = new() { Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
    private readonly Label _lblTokenRecv = new() { AutoSize = true, Left = 12 };
    private readonly Label _lblHotkeyRecv = new() { AutoSize = true, Left = 12 };
    private readonly TextBox _hotkeyRecvBox = new() { ReadOnly = true, TabStop = true };
    private readonly CheckBox _receiveAutoPaste = new() { AutoSize = true, Left = 12 };
    private readonly Label _lblStableWait = new() { AutoSize = true, Left = 12 };
    private readonly NumericUpDown _stableWaitMs = new() { Minimum = 0, Maximum = 2000, Width = 90, Left = 12 };
    private readonly Button _btnSaveRecv = new() { Width = 120, Height = 30 };
    private readonly Button _btnCancelRecv = new() { Width = 120, Height = 30 };

    // ======= 最下段 =======
    private readonly FlowLayoutPanel _bottomRow = new()
    {
        Dock = DockStyle.Bottom, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(12, 8, 12, 10)
    };
    private readonly Button _btnHelp = new() { AutoSize = true, Height = 26 };
    private readonly Label _lblAppName = new() { AutoSize = true };
    private readonly Label _lblVersion = new() { AutoSize = true };
    private readonly LinkLabel _lnkWebsite = new() { AutoSize = true };

    private void InitializeUI()
    {
        Width = 720; Height = 960; MinimumSize = new System.Drawing.Size(680, 880);
        StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true; MinimizeBox = false;

        Controls.Add(_tabs);
        _tabs.TabPages.Add(_tabSend);
        _tabs.TabPages.Add(_tabReceive);
        _tabSend.Padding = new Padding(8);
        _tabReceive.Padding = new Padding(8);

        _url.Left = 12; _url.Top = 38; _url.Width = 660;
        _lblTokenSend.Left = 12; _lblTokenSend.Top = 78;
        _tokenSend.Left = 12; _tokenSend.Top = 98; _tokenSend.Width = 660;
        _chkShowTokenSend.Left = 12; _chkShowTokenSend.Top = _tokenSend.Bottom + 6;
        _btnTest.Left = 552; _btnTest.Top = _tokenSend.Bottom + 2;
        _showSuccess.Left = 12; _showSuccess.Top = _chkShowTokenSend.Bottom + 10;
        _enabled.Left = 12; _enabled.Top = _showSuccess.Bottom + 10;
        _lblHotkeySend.Top = _enabled.Bottom + 16;
        _hotkeySendBox.Left = 12; _hotkeySendBox.Top = _lblHotkeySend.Bottom + 6; _hotkeySendBox.Width = 300;

        _lblBasic.Left = 360; _lblBasic.Top = _showSuccess.Top;
        _lblBasicUser.Left = 360; _lblBasicUser.Top = _lblBasic.Bottom + 8;
        _basicUser.Left = 360; _basicUser.Top = _lblBasicUser.Bottom + 6; _basicUser.Width = 312;
        _lblBasicPass.Left = 360; _lblBasicPass.Top = _basicUser.Bottom + 10;
        _basicPass.Left = 360; _basicPass.Top = _lblBasicPass.Bottom + 6; _basicPass.Width = 312;
        _chkShowBasicPass.Left = 360; _chkShowBasicPass.Top = _basicPass.Bottom + 6;

        _lblCleanup.Left = 12; _lblCleanup.Top = _hotkeySendBox.Bottom + 18;
        _lblCleanupUrl.Left = 12; _lblCleanupUrl.Top = _lblCleanup.Bottom + 8;
        _cleanupUrl.Left = 12; _cleanupUrl.Top = _lblCleanupUrl.Bottom + 6; _cleanupUrl.Width = 660;
        _lblCleanupToken.Left = 12; _lblCleanupToken.Top = _cleanupUrl.Bottom + 10;
        _cleanupToken.Left = 12; _cleanupToken.Top = _lblCleanupToken.Bottom + 6; _cleanupToken.Width = 660;
        _chkShowCleanupToken.Left = 12; _chkShowCleanupToken.Top = _cleanupToken.Bottom + 6;

        _cleanupDailyEnabled.Left = 12; _cleanupDailyEnabled.Top = _chkShowCleanupToken.Bottom + 12;
        _lblBakCount.Left = _lblBasic.Left; _lblBakCount.Top = _cleanupDailyEnabled.Top + 2;
        _btnPurgeBak.Left = _lblBasic.Left; _btnPurgeBak.Top = _cleanupDailyEnabled.Bottom + 6;

        _lblDailyTime.Left = 30; _lblDailyTime.Top = _btnPurgeBak.Bottom + 8;
        _cleanupDailyHour.Left = 30; _cleanupDailyHour.Top = _lblDailyTime.Bottom + 4;
        _lblH.Left = _cleanupDailyHour.Right + 6; _lblH.Top = _cleanupDailyHour.Top + 4;
        _cleanupDailyMinute.Left = _lblH.Right + 10; _cleanupDailyMinute.Top = _cleanupDailyHour.Top;
        _lblM.Left = _cleanupDailyMinute.Right + 6; _lblM.Top = _cleanupDailyMinute.Top + 4;

        _cleanupEveryEnabled.Left = 12; _cleanupEveryEnabled.Top = _cleanupDailyHour.Bottom + 14;
        _lblEvery.Left = 30; _lblEvery.Top = _cleanupEveryEnabled.Bottom + 8;
        _cleanupEveryMinutes.Left = 30; _cleanupEveryMinutes.Top = _lblEvery.Bottom + 4;

        _lblLang.Left = 12; _lblLang.Top = _cleanupEveryMinutes.Bottom + 18;
        _lang.Left = 12; _lang.Top = _lblLang.Bottom + 6;

        _btnSaveSend.Left = 552; _btnSaveSend.Top = _lang.Top;
        _btnCancelSend.Left = 424; _btnCancelSend.Top = _lang.Top;

        _receiveUrl.Left = 12; _receiveUrl.Top = 38; _receiveUrl.Width = 660;
        _lblTokenRecv.Left = 12; _lblTokenRecv.Top = 78;
        _tokenRecv.Left = 12; _tokenRecv.Top = 98; _tokenRecv.Width = 660;
        _chkShowTokenRecv.Left = 12; _chkShowTokenRecv.Top = _tokenRecv.Bottom + 6;
        _lblHotkeyRecv.Left = 12; _lblHotkeyRecv.Top = _chkShowTokenRecv.Bottom + 14;
        _hotkeyRecvBox.Left = 12; _hotkeyRecvBox.Top = _lblHotkeyRecv.Bottom + 6; _hotkeyRecvBox.Width = 300;
        _receiveAutoPaste.Left = 12; _receiveAutoPaste.Top = _hotkeyRecvBox.Bottom + 16;
        _lblStableWait.Left = 12; _lblStableWait.Top = _receiveAutoPaste.Bottom + 12;
        _stableWaitMs.Left = 12; _stableWaitMs.Top = _lblStableWait.Bottom + 6;

        _btnSaveRecv.Left = 552; _btnCancelRecv.Left = 424;
        _btnSaveRecv.Top = _stableWaitMs.Bottom + 18; _btnCancelRecv.Top = _btnSaveRecv.Top;

        _bottomRow.Controls.Add(_btnHelp);
        _lblAppName.Margin = new Padding(14, 6, 0, 0); _lblVersion.Margin = new Padding(10, 6, 0, 0); _lnkWebsite.Margin = new Padding(10, 6, 0, 0);
        _bottomRow.Controls.Add(_lblAppName); _bottomRow.Controls.Add(_lblVersion); _bottomRow.Controls.Add(_lnkWebsite);
        Controls.Add(_bottomRow);

        _tabSend.Controls.AddRange(new Control[] { _lblUrl, _url, _lblTokenSend, _tokenSend, _chkShowTokenSend, _btnTest, _showSuccess, _enabled, _lblHotkeySend, _hotkeySendBox, _lblBasic, _lblBasicUser, _basicUser, _lblBasicPass, _basicPass, _chkShowBasicPass, _lblCleanup, _lblCleanupUrl, _cleanupUrl, _lblCleanupToken, _cleanupToken, _chkShowCleanupToken, _cleanupDailyEnabled, _lblBakCount, _btnPurgeBak, _lblDailyTime, _cleanupDailyHour, _lblH, _cleanupDailyMinute, _lblM, _cleanupEveryEnabled, _lblEvery, _cleanupEveryMinutes, _lblLang, _lang, _btnSaveSend, _btnCancelSend });
        _tabReceive.Controls.AddRange(new Control[] { _lblReceiveUrl, _receiveUrl, _lblTokenRecv, _tokenRecv, _chkShowTokenRecv, _lblHotkeyRecv, _hotkeyRecvBox, _receiveAutoPaste, _lblStableWait, _stableWaitMs, _btnSaveRecv, _btnCancelRecv });
    }

    private void ApplyResponsiveLayout()
    {
        FitWidthToTab(_tabSend, _url, 12, 12); FitWidthToTab(_tabSend, _tokenSend, 12, 12);
        FitWidthToTab(_tabSend, _cleanupUrl, 12, 12); FitWidthToTab(_tabSend, _cleanupToken, 12, 12);
        FitWidthToTab(_tabReceive, _receiveUrl, 12, 12); FitWidthToTab(_tabReceive, _tokenRecv, 12, 12);

        var rightW = Math.Max(120, _tabSend.ClientSize.Width - 360 - 12);
        _basicUser.Width = rightW; _basicPass.Width = rightW;
        _btnTest.Left = Math.Max(12, _tabSend.ClientSize.Width - 12 - _btnTest.Width);

        var xRight = _tabSend.ClientSize.Width - 12 - _lblBakCount.Width;
        _lblBakCount.Left = Math.Max(_cleanupDailyEnabled.Right + 12, xRight);
        _lblBakCount.Top = _cleanupDailyEnabled.Top + 2;
        _btnPurgeBak.Width = Math.Min(260, Math.Max(160, _tabSend.ClientSize.Width - 24));

        _lblBakCount.Left = _lblBasic.Left; _lblDailyTime.Top = _btnPurgeBak.Top;
        _cleanupDailyHour.Top = _btnPurgeBak.Bottom + 4; _lblH.Top = _cleanupDailyHour.Top + 4;
        _cleanupDailyMinute.Top = _cleanupDailyHour.Top; _lblM.Top = _cleanupDailyMinute.Top + 4;
        _cleanupEveryEnabled.Top = _cleanupDailyHour.Bottom + 14; _lblEvery.Top = _cleanupEveryEnabled.Bottom + 8;
        _cleanupEveryMinutes.Top = _lblEvery.Bottom + 4; _lblLang.Top = _cleanupEveryMinutes.Bottom + 18;
        _lang.Top = _lblLang.Bottom + 6; _btnSaveSend.Top = _lang.Top; _btnCancelSend.Top = _lang.Top;
    }

    private static void FitWidthToTab(TabPage tab, Control c, int marginLeft, int marginRight)
    {
        var w = tab.ClientSize.Width - marginLeft - marginRight;
        c.Left = marginLeft; c.Width = w < 50 ? 50 : w;
    }

    private void ApplyLanguageTexts()
    {
        Text = I18n.T("SettingsTitle");
        _tabSend.Text = I18n.T("TabSendDelete"); _tabReceive.Text = I18n.T("TabReceive");

        _lblUrl.Text = I18n.T("PostUrlLabel"); _lblTokenSend.Text = I18n.T("TokenLabel");
        _chkShowTokenSend.Text = I18n.T("ShowToken"); _btnTest.Text = I18n.T("TestConnection");
        _enabled.Text = I18n.T("EnabledCheckbox"); _showSuccess.Text = I18n.T("ShowSuccessCheckbox");
        _lblHotkeySend.Text = I18n.T("HotkeyLabel"); _lblBasic.Text = I18n.T("BasicAuthSection");
        _lblBasicUser.Text = I18n.T("BasicUserLabel"); _lblBasicPass.Text = I18n.T("BasicPassLabel");
        _chkShowBasicPass.Text = I18n.T("ShowBasicPass"); _lblCleanup.Text = I18n.T("CleanupSection");
        _lblCleanupUrl.Text = I18n.T("CleanupUrlLabel"); _lblCleanupToken.Text = I18n.T("CleanupTokenLabel");
        _chkShowCleanupToken.Text = I18n.T("ShowCleanupToken"); _cleanupDailyEnabled.Text = I18n.T("DailyCheckbox");
        _lblDailyTime.Text = I18n.T("TimeLabel"); _lblH.Text = I18n.T("Hour"); _lblM.Text = I18n.T("Minute");
        _cleanupEveryEnabled.Text = I18n.T("EveryCheckbox"); _lblEvery.Text = I18n.T("EveryMinutesLabel");
        _btnPurgeBak.Text = I18n.T("PurgeBakButton"); _lblLang.Text = I18n.T("LangLabel");
        _btnSaveSend.Text = I18n.T("Save"); _btnCancelSend.Text = I18n.T("Cancel");

        _lblReceiveUrl.Text = I18n.T("ReceiveUrlLabel"); _lblTokenRecv.Text = I18n.T("TokenLabel");
        _chkShowTokenRecv.Text = I18n.T("ShowToken"); _lblHotkeyRecv.Text = I18n.T("ReceiveHotkeyLabel");
        _receiveAutoPaste.Text = SafeT("ReceiveAutoPaste", "Auto paste (Ctrl+V) after receive");
        _lblStableWait.Text = SafeT("ClipboardStableWaitLabel", "Clipboard stable wait (ms)");
        _btnSaveRecv.Text = I18n.T("Save"); _btnCancelRecv.Text = I18n.T("Cancel");

        _btnHelp.Text = SafeT("HelpLink", "Help");
        _lblAppName.Text = GetProductName();
        _lblVersion.Text = $"{SafeT("VersionLabel", "Version")}: {GetAppVersion()}";
        _lnkWebsite.Text = SafeT("WebsiteLink", "GitHub");
    }

    private static string SafeT(string key, string fallback)
    {
        var v = I18n.T(key);
        return string.Equals(v, key, StringComparison.OrdinalIgnoreCase) ? fallback : v;
    }

    private static string GetProductName()
    {
        try { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product?.Trim() ?? "App"; }
        catch { return "App"; }
    }

    private static string GetAppVersion()
    {
        try { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Trim() ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"; }
        catch { return "unknown"; }
    }
}