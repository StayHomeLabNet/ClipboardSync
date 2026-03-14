using System;
using System.Collections.Generic;

internal static class I18n
{
    private static string _currentLanguage = "en";

    public static event EventHandler? LanguageChanged;

    public static IReadOnlyList<(string Code, string Display)> SupportedLanguages { get; } =
        new List<(string Code, string Display)>
        {
            ("en", "English"),
            ("ja", "日本語"),
            ("tr", "Türkçe")
        };

    private static readonly Dictionary<string, Dictionary<string, string>> _resources =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SettingsTitle"] = "Clipboard Sync Settings",
                ["TabSendDelete"] = "Send / Delete",
                ["TabReceive"] = "Receive",

                ["PostUrlLabel"] = "URL or directory of the POST API (api.php)",
                ["ReceiveUrlLabel"] = "URL or directory of the Read API (read_api.php)",
                ["TokenLabel"] = "Token (EXPECTED_TOKEN)",
                ["ShowToken"] = "Show token",
                ["EnabledCheckbox"] = "Enable sending",
                ["ShowSuccessCheckbox"] = "Show success message",
                ["HotkeyLabel"] = "Send hotkey",
                ["NeedModifier"] = "Please press a hotkey with Ctrl, Alt, or Shift.",
                ["InputErrorTitle"] = "Input Error",
                ["CopyApiDirectoryButton"] = "Copy directory to all API URLs",
                ["CopyApiDirectoryDone"] = "Copied the API directory to Read API URL and Cleanup API URL.",
                ["CopyApiDirectoryFailed"] = "Could not detect the /api/ directory from the POST URL.",

                ["BasicAuthSection"] = "Basic Authentication",
                ["BasicUserLabel"] = "Username",
                ["BasicPassLabel"] = "Password",
                ["ShowBasicPass"] = "Show password",
                ["BasicIncomplete"] = "If Basic Auth username is set, password is also required.",

                ["CleanupSection"] = "Cleanup",
                ["CleanupUrlLabel"] = "URL or directory of the Cleanup API (cleanup_api.php)",
                ["CleanupTokenLabel"] = "Token (ADMIN_TOKEN)",
                ["ShowCleanupToken"] = "Show cleanup token",
                ["DailyCheckbox"] = "Run once daily",
                ["TimeLabel"] = "Time",
                ["Hour"] = "Hour",
                ["Minute"] = "Minute",
                ["EveryCheckbox"] = "Run every X minutes",
                ["EveryMinutesLabel"] = "Minutes",
                ["PurgeBakButton"] = "Delete all backups",
                ["PurgeMediaButton"] = "Delete all images/files",

                ["BakCountNone"] = "Backup count: not available",
                ["BakCountLoading"] = "Backup count: loading...",
                ["BakCountFormat"] = "Backup count: {0}",
                ["BakCountFail"] = "Backup count: failed",

                ["ImgCountNone"] = "Image count: not available",
                ["ImgCountLoading"] = "Image count: loading...",
                ["ImgCountFormat"] = "Image count: {0}",
                ["ImgCountFail"] = "Image count: failed",

                ["FileCountNone"] = "File count: not available",
                ["FileCountLoading"] = "File count: loading...",
                ["FileCountFormat"] = "File count: {0}",
                ["FileCountFail"] = "File count: failed",

                ["LangLabel"] = "Language",
                ["Save"] = "Save",
                ["Cancel"] = "Cancel",
                ["SavedTitle"] = "Saved",
                ["SavedMsg"] = "Settings have been saved.",

                ["ReceiveHotkeyLabel"] = "Receive Text hotkey",
                ["ReceiveImageHotkeyLabel"] = "Receive Image hotkey",
                ["ReceiveFileHotkeyLabel"] = "Receive File hotkey",
                ["ReceiveAutoPaste"] = "Auto paste (Ctrl+V) after receive",
                ["ClipboardStableWaitLabel"] = "Clipboard stable wait (ms)",

                ["HotkeyClearHint"] = "Hotkeys can be cleared with the Delete or Backspace key.",
                ["ReceiveLatestHotkeyLabel"] = "Receive Latest hotkey",
                ["ReceiveLatestTitle"] = "Receive Latest",
                ["ReceiveLatestEmpty"] = "Latest clipboard type was not found",
                ["ReceiveLatestUnsupportedFormat"] = "Latest clipboard type is not supported: {0}",

                ["TestConnection"] = "Test Connection",
                ["UrlInvalid"] = "The URL is invalid.",
                ["TokenEmpty"] = "Token is empty.",
                ["ConfirmTitle"] = "Confirmation",
                ["ConfirmPurgeBakBody"] = "Delete all backup files?",
                ["ConfirmPurgeMediaBody"] = "Delete all images and files?",
                ["PurgeBakDoneTitle"] = "Backup Deletion Completed",
                ["PurgeBakFailTitle"] = "Backup Deletion Failed",
                ["PurgeMediaDoneTitle"] = "Media Deletion Completed",
                ["PurgeMediaFailTitle"] = "Media Deletion Failed",

                ["TrayTitle"] = "Clipboard Sync",
                ["MenuEnabled"] = "Sending: Enabled",
                ["MenuDisabled"] = "Sending: Disabled",
                ["MenuDeleteInbox"] = "Delete INBOX",
                ["MenuSettings"] = "Settings",
                ["MenuHelp"] = "Help",
                ["MenuAbout"] = "About",
                ["MenuExit"] = "Exit",

                ["BalloonEnabled"] = "Sending has been enabled.",
                ["BalloonDisabled"] = "Sending has been disabled.",

                ["CleanupTitle"] = "Cleanup",
                ["CleanupRunning"] = "Cleanup is running...",
                ["CleanupDoneTitle"] = "Cleanup Completed",
                ["CleanupFailTitle"] = "Cleanup Failed",
                ["ConfirmDeleteInboxBody"] = "Delete all INBOX items?",

                ["SendOkTitle"] = "Send Completed",
                ["SendFailTitle"] = "Send Failed",

                ["ReceiveTitle"] = "Receive Text",
                ["ReceiveEmpty"] = "No text was received.",
                ["ReceiveOkBalloon"] = "Text received successfully.",

                ["ReceiveImageTitle"] = "Receive Image",
                ["ReceiveImageEmpty"] = "No image was received.",
                ["ReceiveImageOkBalloon"] = "Image received successfully.",

                ["ReceiveFileTitle"] = "Receive File",
                ["ReceiveFileEmpty"] = "No file was received.",
                ["ReceiveFileOkBalloon"] = "File received successfully.",

                ["ReceiveFailTitle"] = "Receive Failed",

                ["AutoCleanupTitle"] = "Automatic Cleanup",
                ["AutoCleanupOk"] = "Automatic cleanup completed.",
                ["AutoCleanupFail"] = "Automatic cleanup failed.",

                ["CleanupModeInfoFormat"] = "Cleanup mode: {0}",
                ["CleanupModeDailyAndEveryFormat"] = "Daily {0:D2}:{1:D2} / Every {2} min",
                ["CleanupModeDailyFormat"] = "Daily {0:D2}:{1:D2}",
                ["CleanupModeEveryFormat"] = "Every {0} min",
                ["CleanupModeOff"] = "Off",

                ["CleanupLastNotYet"] = "Last cleanup: not yet",
                ["CleanupLastFormat"] = "Last cleanup: {0} ({1})",
                ["WordSuccess"] = "Success",
                ["WordFail"] = "Failed",

                ["AboutTitle"] = "About",
                ["AboutBodyFormat"] = "{0}\nVersion: {1}\n{2}",
                ["HelpLink"] = "Help",
                ["WebsiteLink"] = "GitHub",
                ["VersionLabel"] = "Version",
            },

            ["ja"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SettingsTitle"] = "Clipboard Sync 設定",
                ["TabSendDelete"] = "送信 / 削除",
                ["TabReceive"] = "受信",

                ["PostUrlLabel"] = "POST API (api.php) の URL または、ディレクトリ",
                ["ReceiveUrlLabel"] = "Read API (read_api.php) の URL または、ディレクトリ",
                ["TokenLabel"] = "トークン (EXPECTED_TOKEN)",
                ["ShowToken"] = "トークンを表示",
                ["EnabledCheckbox"] = "送信を有効にする",
                ["ShowSuccessCheckbox"] = "送信成功時にメッセージを表示",
                ["HotkeyLabel"] = "送信ホットキー",
                ["NeedModifier"] = "Ctrl、Alt、Shift のいずれかを含むホットキーを入力してください。",
                ["InputErrorTitle"] = "入力エラー",
                ["CopyApiDirectoryButton"] = "ディレクトリをすべてのAPI URLにコピー",
                ["CopyApiDirectoryDone"] = "api.php の欄のディレクトリを Read API URL と Cleanup API URL にコピーしました。",
                ["CopyApiDirectoryFailed"] = "POST URL から /api/ ディレクトリを判定できませんでした。",

                ["BasicAuthSection"] = "Basic認証",
                ["BasicUserLabel"] = "ユーザー名",
                ["BasicPassLabel"] = "パスワード",
                ["ShowBasicPass"] = "パスワードを表示",
                ["BasicIncomplete"] = "Basic認証のユーザー名を設定する場合は、パスワードも必要です。",

                ["CleanupSection"] = "クリーンアップ",
                ["CleanupUrlLabel"] = "Cleanup API (cleanup_api.php) の URL または、ディレクトリ",
                ["CleanupTokenLabel"] = "トークン (ADMIN_TOKEN)",
                ["ShowCleanupToken"] = "Cleanup トークンを表示",
                ["DailyCheckbox"] = "1日1回実行",
                ["TimeLabel"] = "時刻",
                ["Hour"] = "時",
                ["Minute"] = "分",
                ["EveryCheckbox"] = "X分ごとに実行",
                ["EveryMinutesLabel"] = "分",
                ["PurgeBakButton"] = "バックアップを全削除",
                ["PurgeMediaButton"] = "画像・ファイルを全削除",

                ["BakCountNone"] = "バックアップ件数: 未取得",
                ["BakCountLoading"] = "バックアップ件数: 読み込み中...",
                ["BakCountFormat"] = "バックアップ件数: {0}",
                ["BakCountFail"] = "バックアップ件数: 取得失敗",

                ["ImgCountNone"] = "画像件数: 未取得",
                ["ImgCountLoading"] = "画像件数: 読み込み中...",
                ["ImgCountFormat"] = "画像件数: {0}",
                ["ImgCountFail"] = "画像件数: 取得失敗",

                ["FileCountNone"] = "ファイル件数: 未取得",
                ["FileCountLoading"] = "ファイル件数: 読み込み中...",
                ["FileCountFormat"] = "ファイル件数: {0}",
                ["FileCountFail"] = "ファイル件数: 取得失敗",

                ["LangLabel"] = "言語",
                ["Save"] = "保存",
                ["Cancel"] = "キャンセル",
                ["SavedTitle"] = "保存完了",
                ["SavedMsg"] = "設定を保存しました。",

                ["ReceiveHotkeyLabel"] = "テキストを受信ホットキー",
                ["ReceiveImageHotkeyLabel"] = "画像を受信ホットキー",
                ["ReceiveFileHotkeyLabel"] = "ファイルを受信ホットキー",
                ["ReceiveAutoPaste"] = "受信後に自動で貼り付け (Ctrl+V)",
                ["ClipboardStableWaitLabel"] = "クリップボード安定待ち (ms)",

                ["HotkeyClearHint"] = "ホットキーは、DeleteまたはBackspaceキーで解除できます。",
                ["ReceiveLatestHotkeyLabel"] = "最新を受信ホットキー",
                ["ReceiveLatestTitle"] = "最新を受信",
                ["ReceiveLatestEmpty"] = "最新クリップの種類が取得できませんでした",
                ["ReceiveLatestUnsupportedFormat"] = "未対応の最新クリップ種別です: {0}",

                ["TestConnection"] = "接続テスト",
                ["UrlInvalid"] = "URL が正しくありません。",
                ["TokenEmpty"] = "トークンが空です。",
                ["ConfirmTitle"] = "確認",
                ["ConfirmPurgeBakBody"] = "バックアップファイルをすべて削除しますか？",
                ["ConfirmPurgeMediaBody"] = "画像とファイルをすべて削除しますか？",
                ["PurgeBakDoneTitle"] = "バックアップ削除完了",
                ["PurgeBakFailTitle"] = "バックアップ削除失敗",
                ["PurgeMediaDoneTitle"] = "メディア削除完了",
                ["PurgeMediaFailTitle"] = "メディア削除失敗",

                ["TrayTitle"] = "Clipboard Sync",
                ["MenuEnabled"] = "送信: 有効",
                ["MenuDisabled"] = "送信: 無効",
                ["MenuDeleteInbox"] = "INBOX を削除",
                ["MenuSettings"] = "設定",
                ["MenuHelp"] = "ヘルプ",
                ["MenuAbout"] = "このアプリについて",
                ["MenuExit"] = "終了",

                ["BalloonEnabled"] = "送信を有効にしました。",
                ["BalloonDisabled"] = "送信を無効にしました。",

                ["CleanupTitle"] = "クリーンアップ",
                ["CleanupRunning"] = "クリーンアップ実行中...",
                ["CleanupDoneTitle"] = "クリーンアップ完了",
                ["CleanupFailTitle"] = "クリーンアップ失敗",
                ["ConfirmDeleteInboxBody"] = "INBOX をすべて削除しますか？",

                ["SendOkTitle"] = "送信完了",
                ["SendFailTitle"] = "送信失敗",

                ["ReceiveTitle"] = "テキストを受信",
                ["ReceiveEmpty"] = "受信できるテキストがありませんでした。",
                ["ReceiveOkBalloon"] = "テキストを受信しました。",

                ["ReceiveImageTitle"] = "画像を受信",
                ["ReceiveImageEmpty"] = "受信できる画像がありませんでした。",
                ["ReceiveImageOkBalloon"] = "画像を受信しました。",

                ["ReceiveFileTitle"] = "ファイルを受信",
                ["ReceiveFileEmpty"] = "受信できるファイルがありませんでした。",
                ["ReceiveFileOkBalloon"] = "ファイルを受信しました。",

                ["ReceiveFailTitle"] = "受信失敗",

                ["AutoCleanupTitle"] = "自動クリーンアップ",
                ["AutoCleanupOk"] = "自動クリーンアップが完了しました。",
                ["AutoCleanupFail"] = "自動クリーンアップに失敗しました。",

                ["CleanupModeInfoFormat"] = "クリーンアップ設定: {0}",
                ["CleanupModeDailyAndEveryFormat"] = "毎日 {0:D2}:{1:D2} / {2}分ごと",
                ["CleanupModeDailyFormat"] = "毎日 {0:D2}:{1:D2}",
                ["CleanupModeEveryFormat"] = "{0}分ごと",
                ["CleanupModeOff"] = "オフ",

                ["CleanupLastNotYet"] = "前回のクリーンアップ: まだありません",
                ["CleanupLastFormat"] = "前回のクリーンアップ: {0} ({1})",
                ["WordSuccess"] = "成功",
                ["WordFail"] = "失敗",

                ["AboutTitle"] = "このアプリについて",
                ["AboutBodyFormat"] = "{0}\nバージョン: {1}\n{2}",
                ["HelpLink"] = "ヘルプ",
                ["WebsiteLink"] = "GitHub",
                ["VersionLabel"] = "バージョン",
            },

            ["tr"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["SettingsTitle"] = "Clipboard Sync Ayarları",
                ["TabSendDelete"] = "Gönder / Sil",
                ["TabReceive"] = "Al",

                ["PostUrlLabel"] = "POST API'nin URL'si veya dizini (api.php)",
                ["ReceiveUrlLabel"] = "Read API'nin URL'si veya dizini (read_api.php)",
                ["TokenLabel"] = "Token (EXPECTED_TOKEN)",
                ["ShowToken"] = "Tokenı göster",
                ["EnabledCheckbox"] = "Göndermeyi etkinleştir",
                ["ShowSuccessCheckbox"] = "Başarılı gönderimde mesaj göster",
                ["HotkeyLabel"] = "Gönderme kısayolu",
                ["NeedModifier"] = "Lütfen Ctrl, Alt veya Shift içeren bir kısayol tuşu girin.",
                ["InputErrorTitle"] = "Giriş Hatası",
                ["CopyApiDirectoryButton"] = "Dizini tüm API URL'lerine kopyala",
                ["CopyApiDirectoryDone"] = "API dizini Read API URL ve Cleanup API URL alanlarına kopyalandı.",
                ["CopyApiDirectoryFailed"] = "POST URL içinden /api/ dizini algılanamadı.",

                ["BasicAuthSection"] = "Basic Authentication",
                ["BasicUserLabel"] = "Kullanıcı adı",
                ["BasicPassLabel"] = "Parola",
                ["ShowBasicPass"] = "Parolayı göster",
                ["BasicIncomplete"] = "Basic Auth kullanıcı adı girilmişse parola da gereklidir.",

                ["CleanupSection"] = "Temizleme",
                ["CleanupUrlLabel"] = "Cleanup API'nin URL'si veya dizini (cleanup_api.php)",
                ["CleanupTokenLabel"] = "Token (ADMIN_TOKEN)",
                ["ShowCleanupToken"] = "Cleanup tokenını göster",
                ["DailyCheckbox"] = "Günde bir kez çalıştır",
                ["TimeLabel"] = "Saat",
                ["Hour"] = "Saat",
                ["Minute"] = "Dakika",
                ["EveryCheckbox"] = "Her X dakikada bir çalıştır",
                ["EveryMinutesLabel"] = "Dakika",
                ["PurgeBakButton"] = "Tüm yedekleri sil",
                ["PurgeMediaButton"] = "Tüm görselleri/dosyaları sil",

                ["BakCountNone"] = "Yedek sayısı: yok",
                ["BakCountLoading"] = "Yedek sayısı: yükleniyor...",
                ["BakCountFormat"] = "Yedek sayısı: {0}",
                ["BakCountFail"] = "Yedek sayısı: alınamadı",

                ["ImgCountNone"] = "Görsel sayısı: yok",
                ["ImgCountLoading"] = "Görsel sayısı: yükleniyor...",
                ["ImgCountFormat"] = "Görsel sayısı: {0}",
                ["ImgCountFail"] = "Görsel sayısı: alınamadı",

                ["FileCountNone"] = "Dosya sayısı: yok",
                ["FileCountLoading"] = "Dosya sayısı: yükleniyor...",
                ["FileCountFormat"] = "Dosya sayısı: {0}",
                ["FileCountFail"] = "Dosya sayısı: alınamadı",

                ["LangLabel"] = "Dil",
                ["Save"] = "Kaydet",
                ["Cancel"] = "İptal",
                ["SavedTitle"] = "Kaydedildi",
                ["SavedMsg"] = "Ayarlar kaydedildi.",

                ["ReceiveHotkeyLabel"] = "Metni Al kısayolu",
                ["ReceiveImageHotkeyLabel"] = "Görseli Al kısayolu",
                ["ReceiveFileHotkeyLabel"] = "Dosyayı Al kısayolu",
                ["ReceiveAutoPaste"] = "Alındıktan sonra otomatik yapıştır (Ctrl+V)",
                ["ClipboardStableWaitLabel"] = "Pano bekleme süresi (ms)",

                ["HotkeyClearHint"] = "Kısayol tuşları Delete veya Backspace tuşuyla temizlenebilir.",
                ["ReceiveLatestHotkeyLabel"] = "En son içeriği al kısayolu",
                ["ReceiveLatestTitle"] = "En Son İçeriği Al",
                ["ReceiveLatestEmpty"] = "En son pano türü bulunamadı",
                ["ReceiveLatestUnsupportedFormat"] = "Desteklenmeyen pano türü: {0}",

                ["TestConnection"] = "Bağlantıyı Test Et",
                ["UrlInvalid"] = "URL geçersiz.",
                ["TokenEmpty"] = "Token boş.",
                ["ConfirmTitle"] = "Onay",
                ["ConfirmPurgeBakBody"] = "Tüm yedek dosyaları silinsin mi?",
                ["ConfirmPurgeMediaBody"] = "Tüm görseller ve dosyalar silinsin mi?",
                ["PurgeBakDoneTitle"] = "Yedek Silme Tamamlandı",
                ["PurgeBakFailTitle"] = "Yedek Silme Başarısız",
                ["PurgeMediaDoneTitle"] = "Medya Silme Tamamlandı",
                ["PurgeMediaFailTitle"] = "Medya Silme Başarısız",

                ["TrayTitle"] = "Clipboard Sync",
                ["MenuEnabled"] = "Gönderme: Açık",
                ["MenuDisabled"] = "Gönderme: Kapalı",
                ["MenuDeleteInbox"] = "INBOX'u Sil",
                ["MenuSettings"] = "Ayarlar",
                ["MenuHelp"] = "Yardım",
                ["MenuAbout"] = "Hakkında",
                ["MenuExit"] = "Çıkış",

                ["BalloonEnabled"] = "Gönderme etkinleştirildi.",
                ["BalloonDisabled"] = "Gönderme devre dışı bırakıldı.",

                ["CleanupTitle"] = "Temizleme",
                ["CleanupRunning"] = "Temizleme çalışıyor...",
                ["CleanupDoneTitle"] = "Temizleme Tamamlandı",
                ["CleanupFailTitle"] = "Temizleme Başarısız",
                ["ConfirmDeleteInboxBody"] = "Tüm INBOX öğeleri silinsin mi?",

                ["SendOkTitle"] = "Gönderme Tamamlandı",
                ["SendFailTitle"] = "Gönderme Başarısız",

                ["ReceiveTitle"] = "Metni Al",
                ["ReceiveEmpty"] = "Alınacak metin yok.",
                ["ReceiveOkBalloon"] = "Metin başarıyla alındı.",

                ["ReceiveImageTitle"] = "Görseli Al",
                ["ReceiveImageEmpty"] = "Alınacak görsel yok.",
                ["ReceiveImageOkBalloon"] = "Görsel başarıyla alındı.",

                ["ReceiveFileTitle"] = "Dosyayı Al",
                ["ReceiveFileEmpty"] = "Alınacak dosya yok.",
                ["ReceiveFileOkBalloon"] = "Dosya başarıyla alındı.",

                ["ReceiveFailTitle"] = "Alma Başarısız",

                ["AutoCleanupTitle"] = "Otomatik Temizleme",
                ["AutoCleanupOk"] = "Otomatik temizleme tamamlandı.",
                ["AutoCleanupFail"] = "Otomatik temizleme başarısız oldu.",

                ["CleanupModeInfoFormat"] = "Temizleme modu: {0}",
                ["CleanupModeDailyAndEveryFormat"] = "Her gün {0:D2}:{1:D2} / Her {2} dk",
                ["CleanupModeDailyFormat"] = "Her gün {0:D2}:{1:D2}",
                ["CleanupModeEveryFormat"] = "Her {0} dk",
                ["CleanupModeOff"] = "Kapalı",

                ["CleanupLastNotYet"] = "Son temizleme: henüz yok",
                ["CleanupLastFormat"] = "Son temizleme: {0} ({1})",
                ["WordSuccess"] = "Başarılı",
                ["WordFail"] = "Başarısız",

                ["AboutTitle"] = "Hakkında",
                ["AboutBodyFormat"] = "{0}\nSürüm: {1}\n{2}",
                ["HelpLink"] = "Yardım",
                ["WebsiteLink"] = "GitHub",
                ["VersionLabel"] = "Sürüm",
            }
        };

    public static string CurrentLanguage => _currentLanguage;

    public static void SetLanguage(string? languageCode)
    {
        var code = NormalizeLanguage(languageCode);

        if (string.Equals(_currentLanguage, code, StringComparison.OrdinalIgnoreCase))
            return;

        _currentLanguage = code;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        var lang = NormalizeLanguage(_currentLanguage);

        if (_resources.TryGetValue(lang, out var dict) &&
            dict.TryGetValue(key, out var value))
            return value;

        if (_resources.TryGetValue("en", out var enDict) &&
            enDict.TryGetValue(key, out var enValue))
            return enValue;

        return key;
    }

    private static string NormalizeLanguage(string? languageCode)
    {
        var code = (languageCode ?? "").Trim().ToLowerInvariant();

        return code switch
        {
            "ja" => "ja",
            "jp" => "ja",
            "tr" => "tr",
            _ => "en"
        };
    }
}