// Program.cs
using System;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        I18n.SetLanguage(SettingsStore.Current.Language);

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }
}