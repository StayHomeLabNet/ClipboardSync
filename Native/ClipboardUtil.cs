using System.Threading.Tasks;
using System.Windows.Forms;

internal static class ClipboardUtil
{
    public static async Task TrySetTextAsync(string text)
    {
        text = (text ?? "").Replace("\r\n", "\n");

        for (int i = 0; i < 5; i++)
        {
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                return;
            }
            catch
            {
                await Task.Delay(40);
            }
        }
    }
}