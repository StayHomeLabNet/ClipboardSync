// UI/TrayAppContext.cs
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

internal sealed class TrayAppContext : ApplicationContext
{
    private const string HELP_URL = "https://stayhomelab.net/ClipboardSync";

    private readonly NotifyIcon _tray;
    private readonly ClipboardWatcherForm _watcher;
    private readonly System.Drawing.Icon _iconOn;
    private readonly System.Drawing.Icon _iconOff;

    private readonly ToolStripMenuItem _enabledMenuItem = new() { CheckOnClick = false };
    private readonly ToolStripMenuItem _cleanupModeInfoItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _cleanupLastResultItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _menuDeleteInbox = new();
    private readonly ToolStripMenuItem _menuSettings = new();
    private readonly ToolStripMenuItem _menuHelp = new();
    private readonly ToolStripMenuItem _menuAbout = new();
    private readonly ToolStripMenuItem _menuExit = new();

    private DateTime? _cleanupLastAt;
    private bool? _cleanupLastOk;

    public TrayAppContext()
    {
        _watcher = new ClipboardWatcherForm();
        _iconOn = EmbeddedIcon.LoadByFileName("tray_on.ico");
        _iconOff = EmbeddedIcon.LoadByFileName("tray_off.ico");

        // ★null警告対策済み：ここでは ContextMenuStrip を new しない
        _tray = new NotifyIcon
        {
            Icon = SettingsStore.Current.Enabled ? _iconOn : _iconOff,
            Text = I18n.T("TrayTitle"),
            Visible = true
        };

        BuildContextMenu();
        SubscribeMenuEvents();
        SubscribeAppEvents();

        _watcher.Show();
        _watcher.Hide();

        CleanupScheduler.ApplyFromSettings();

        RefreshTrayTexts();
        UpdateCleanupModeInfo();
        UpdateCleanupLastResultInfo();
        SyncEnabledMenu();
    }

    // ★重複エラー対策済み：このメソッドは1つだけにしました
    private void BuildContextMenu()
    {
        // ローカル変数でメニューを作ってから、最後にセットする（null警告を回避）
        var menu = new ContextMenuStrip();

        menu.Items.Add(_enabledMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_cleanupModeInfoItem);
        menu.Items.Add(_cleanupLastResultItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_menuDeleteInbox);
        menu.Items.Add(_menuSettings);
        menu.Items.Add(_menuHelp);
        menu.Items.Add(_menuAbout);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_menuExit);

        _tray.ContextMenuStrip = menu;
    }

    private void SubscribeMenuEvents()
    {
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleEnabledFromTray(); };
        _enabledMenuItem.Click += (_, __) => ToggleEnabledFromTray();
        _menuDeleteInbox.Click += async (_, __) => await OnDeleteInboxClicked();
        _menuSettings.Click += (_, __) => OnSettingsClicked();
        _menuHelp.Click += (_, __) => OnHelpClicked();
        _menuAbout.Click += (_, __) => OnAboutClicked();
        _menuExit.Click += (_, __) => ExitThread();
    }

    private void SubscribeAppEvents()
    {
        I18n.LanguageChanged += (_, __) => RunOnUi(() =>
        {
            RefreshTrayTexts();
            SyncEnabledMenu();
            UpdateCleanupModeInfo();
            UpdateCleanupLastResultInfo();
        });

        _watcher.EnabledToggled += (_, enabled) =>
        {
            CleanupScheduler.ApplyFromSettings();
            SyncEnabledMenu();
            UpdateCleanupModeInfo();
            ShowToggleBalloon(enabled);
        };

        _watcher.ClipboardTextCopied += async (_, text) => await OnClipboardTextCopied(text);
        _watcher.ReceiveHotkeyPressed += async (_, __) => await OnReceiveHotkeyPressed();

        CleanupScheduler.CleanupFinished += (_, result) => OnCleanupFinished(result.ok, result.info);

        SettingsStore.Saved += (_, newSettings) => RunOnUi(() =>
        {
            I18n.SetLanguage(newSettings.Language);
            RefreshTrayTexts();
            SyncEnabledMenu();
            CleanupScheduler.ApplyFromSettings();
            UpdateCleanupModeInfo();
        });
    }

    private async Task OnDeleteInboxClicked()
    {
        if (!SettingsStore.Current.Enabled) return;
        var r = MessageBox.Show(I18n.T("ConfirmDeleteInboxBody"), I18n.T("ConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (r != DialogResult.Yes) return;

        _tray.BalloonTipTitle = I18n.T("CleanupTitle");
        _tray.BalloonTipText = I18n.T("CleanupRunning");
        _tray.ShowBalloonTip(1000);

        var (ok, info) = await CleanupApi.DeleteInboxAllAsync();

        using var top = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
        top.Show();

        MessageBox.Show(top, info, ok ? I18n.T("CleanupDoneTitle") : I18n.T("CleanupFailTitle"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void OnSettingsClicked()
    {
        using var f = new SettingsForm();
        f.ShowDialog();

        SyncEnabledMenu();
        _watcher.ApplyHotkeyFromSettings();
        CleanupScheduler.ApplyFromSettings();
        UpdateCleanupModeInfo();
    }

    private void OnHelpClicked()
    {
        try { Process.Start(new ProcessStartInfo { FileName = HELP_URL, UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, I18n.T("MenuHelp"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void OnAboutClicked()
    {
        using var top = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
        top.Show();

        var appName = AppInfo.GetProductName();
        var ver = AppInfo.GetVersionString();
        var body = string.Format(I18n.T("AboutBodyFormat"), appName, ver, HELP_URL);

        MessageBox.Show(top, body, I18n.T("AboutTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task OnClipboardTextCopied(string text)
    {
        if (!SettingsStore.Current.Enabled) return;

        var (ok, info) = await Sender.SendAsync(text);
        var showSuccess = SettingsStore.Current.ShowMessageOnSuccess;

        if (!ok || showSuccess)
        {
            using var top = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
            top.Show();
            MessageBox.Show(top, info, ok ? I18n.T("SendOkTitle") : I18n.T("SendFailTitle"), MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }

    private async Task OnReceiveHotkeyPressed()
    {
        var (ok, textOrErr) = await Receiver.FetchLatestNoteTextAsync();

        if (!ok)
        {
            using var top = new Form { TopMost = true, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
            top.Show();
            MessageBox.Show(top, textOrErr, I18n.T("ReceiveFailTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var text = textOrErr ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            _tray.BalloonTipTitle = I18n.T("ReceiveTitle");
            _tray.BalloonTipText = I18n.T("ReceiveEmpty");
            _tray.ShowBalloonTip(1000);
            return;
        }

        await ClipboardUtil.TrySetTextAsync(text);

        var waitMs = Math.Max(0, SettingsStore.Current.ClipboardStableWaitMs);
        if (waitMs > 0) await Task.Delay(waitMs);

        if (SettingsStore.Current.ReceiveAutoPaste)
        {
            var okPaste = PasteHelper.CtrlV_FallbackSendKeys();
            if (!okPaste)
            {
                _tray.BalloonTipTitle = I18n.T("ReceiveTitle");
                _tray.BalloonTipText = "Auto paste failed.\n・貼り付け先が管理者権限のアプリだと拒否されます\n・Notepadで試してもダメなら設定/実装側の問題です";
                _tray.ShowBalloonTip(2000);
            }
        }

        _tray.BalloonTipTitle = I18n.T("ReceiveTitle");
        _tray.BalloonTipText = I18n.T("ReceiveOkBalloon");
        _tray.ShowBalloonTip(1000);
    }

    private void OnCleanupFinished(bool ok, string info)
    {
        _cleanupLastAt = DateTime.Now;
        _cleanupLastOk = ok;

        RunOnUi(() =>
        {
            UpdateCleanupLastResultInfo();
            _tray.BalloonTipTitle = I18n.T("AutoCleanupTitle");
            _tray.BalloonTipText = ok ? I18n.T("AutoCleanupOk") : (I18n.T("AutoCleanupFail") + "\n" + Shorten(info, 120));
            _tray.ShowBalloonTip(1000);
        });
    }

    private void RunOnUi(Action a)
    {
        try { if (_watcher.IsHandleCreated && _watcher.InvokeRequired) _watcher.BeginInvoke(a); else a(); } catch { }
    }

    private void RefreshTrayTexts()
    {
        _tray.Text = I18n.T("TrayTitle");
        _menuDeleteInbox.Text = I18n.T("MenuDeleteInbox");
        _menuSettings.Text = I18n.T("MenuSettings");
        _menuHelp.Text = I18n.T("MenuHelp");
        _menuAbout.Text = I18n.T("MenuAbout");
        _menuExit.Text = I18n.T("MenuExit");
        UpdateCleanupModeInfo();
        UpdateCleanupLastResultInfo();
    }

    private void ToggleEnabledFromTray()
    {
        var s = SettingsStore.Current;
        s.Enabled = !s.Enabled;
        SettingsStore.Save(s);
        CleanupScheduler.ApplyFromSettings();
        SyncEnabledMenu();
        UpdateCleanupModeInfo();
        ShowToggleBalloon(s.Enabled);
    }

    private void SyncEnabledMenu()
    {
        _enabledMenuItem.Checked = SettingsStore.Current.Enabled;
        _enabledMenuItem.Text = SettingsStore.Current.Enabled ? I18n.T("MenuEnabled") : I18n.T("MenuDisabled");
        _tray.Icon = SettingsStore.Current.Enabled ? _iconOn : _iconOff;
    }

    private void ShowToggleBalloon(bool enabled)
    {
        _tray.BalloonTipTitle = I18n.T("TrayTitle");
        _tray.BalloonTipText = enabled ? I18n.T("BalloonEnabled") : I18n.T("BalloonDisabled");
        _tray.ShowBalloonTip(1000);
    }

    private void UpdateCleanupModeInfo() => _cleanupModeInfoItem.Text = string.Format(I18n.T("CleanupModeInfoFormat"), GetCleanupModeText());

    private static string GetCleanupModeText()
    {
        var s = SettingsStore.Current;
        if (!s.Enabled) return I18n.T("CleanupModeOff");
        if (s.CleanupDailyEnabled && s.CleanupEveryEnabled) return string.Format(I18n.T("CleanupModeDailyAndEveryFormat"), s.CleanupDailyHour, s.CleanupDailyMinute, Math.Max(1, s.CleanupEveryMinutes));
        if (s.CleanupDailyEnabled) return string.Format(I18n.T("CleanupModeDailyFormat"), s.CleanupDailyHour, s.CleanupDailyMinute);
        if (s.CleanupEveryEnabled) return string.Format(I18n.T("CleanupModeEveryFormat"), Math.Max(1, s.CleanupEveryMinutes));
        return I18n.T("CleanupModeOff");
    }

    private void UpdateCleanupLastResultInfo()
    {
        if (_cleanupLastAt is null || _cleanupLastOk is null) { _cleanupLastResultItem.Text = I18n.T("CleanupLastNotYet"); return; }
        _cleanupLastResultItem.Text = string.Format(I18n.T("CleanupLastFormat"), _cleanupLastAt.Value.ToString("yyyy-MM-dd HH:mm:ss"), _cleanupLastOk.Value ? I18n.T("WordSuccess") : I18n.T("WordFail"));
    }

    private static string Shorten(string s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Replace("\r", "").Replace("\n", " ");
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _watcher.Dispose();
        CleanupScheduler.Stop();
        _iconOn.Dispose();
        _iconOff.Dispose();
        base.ExitThreadCore();
    }
}