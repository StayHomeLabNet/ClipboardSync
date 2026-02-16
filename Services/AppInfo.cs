using System;
using System.Linq;
using System.Reflection;

internal static class AppInfo
{
    public static string GetProductName()
    {
        var asm = Assembly.GetExecutingAssembly();
        var prod = asm.GetCustomAttributes<AssemblyProductAttribute>().FirstOrDefault()?.Product;
        if (!string.IsNullOrWhiteSpace(prod)) return prod!;
        return asm.GetName().Name ?? "ClipboardSender";
    }

    public static string GetVersionString()
    {
        var asm = Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                      .FirstOrDefault()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(info)) return info!;

        var fv = asm.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .FirstOrDefault()?.Version;
        if (!string.IsNullOrWhiteSpace(fv)) return fv!;

        return asm.GetName().Version?.ToString() ?? "0.0.0";
    }
}