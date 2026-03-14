using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System; // ★追加

internal static class ClipboardUtil
{
    // ★追加：この時刻までのクリップボード更新は「自分がセットしたもの」として無視する
    private static DateTime _suppressUntilUtc = DateTime.MinValue;

    public static bool IsSuppressedNow()
    {
        return DateTime.UtcNow < _suppressUntilUtc;
    }

    private static void SuppressFor(int milliseconds)
    {
        var until = DateTime.UtcNow.AddMilliseconds(milliseconds);
        if (until > _suppressUntilUtc) _suppressUntilUtc = until;
    }

    public static async Task TrySetTextAsync(string text)
    {
        text = (text ?? "").Replace("\r\n", "\n");

        // ★追加：クリップボードに入れる前に抑止（少し長めに）
        SuppressFor(1200);

        for (int i = 0; i < 5; i++)
        {
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                return;
            }
            catch { await Task.Delay(40); }
        }
    }

    public static async Task TrySetImageAsync(byte[] imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0) return;

        // ★追加
        SuppressFor(1500);

        for (int i = 0; i < 5; i++)
        {
            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var img = Image.FromStream(ms);
                Clipboard.SetImage(img);
                return;
            }
            catch { await Task.Delay(40); }
        }
    }

    public static async Task TrySetFileDropListAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        // ★追加（ファイルはシェル側の都合で反映に時間かかることがあるので少し長め）
        SuppressFor(2000);

        for (int i = 0; i < 5; i++)
        {
            try
            {
                var fileList = new StringCollection { filePath };
                Clipboard.SetFileDropList(fileList);
                return;
            }
            catch { await Task.Delay(40); }
        }
    }
}