using System;
using System.IO;
using System.Reflection;

internal static class EmbeddedIcon
{
    public static System.Drawing.Icon LoadByFileName(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("\\" + fileName, StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            {
                var stream = asm.GetManifestResourceStream(name);
                if (stream == null) continue;

                using (stream)
                {
                    using var ico = new System.Drawing.Icon(stream);
                    return (System.Drawing.Icon)ico.Clone();
                }
            }
        }

        throw new FileNotFoundException(
            $"Embedded resource not found: {fileName}\n" +
            $"確認: ico のビルドアクションが『埋め込みリソース』になっているか");
    }
}