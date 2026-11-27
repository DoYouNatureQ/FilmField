using Microsoft.UI.Xaml;

namespace FilmField.Services;

public static class ThemeService
{
    public const int System = 0;
    public const int Light = 1;
    public const int Dark = 2;

    public static ElementTheme Current => SettingsService.Current.ThemeMode switch
    {
        Light => ElementTheme.Light,
        Dark => ElementTheme.Dark,
        _ => ElementTheme.Default
    };

    public static void SetMode(int mode)
    {
        SettingsService.Current.ThemeMode = mode;
        SettingsService.Save();
        MainWindow.Instance?.ApplyTheme();
    }
}
