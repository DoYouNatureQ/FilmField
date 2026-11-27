using System.Text.Json;

namespace FilmField.Services;

public sealed class AppSettings
{
    public const int BackdropOff = 0;
    public const int BackdropAcrylic = 1;

    public double Volume { get; set; } = 1.0;
    public bool Muted { get; set; }
    public double Rate { get; set; } = 1.0;
    public string? LastFile { get; set; }
    public int ThemeMode { get; set; } = 1;
    public int LanguageMode { get; set; }
    public bool UseFfmpeg { get; set; } = true;
    public bool ResumeEnabled { get; set; } = true;
    public bool ClickVideoToToggle { get; set; } = true;
    public bool PauseOnPanelSwitch { get; set; } = true;
    public bool ArrowScanRate { get; set; } = true;
    public int SkipSeconds { get; set; } = 5;
    public double ScanSlowRate { get; set; } = 0.5;
    public double ScanFastRate { get; set; } = 2.0;
    public double WindowWidth { get; set; }
    public double WindowHeight { get; set; }
    public double WindowX { get; set; } = -1;
    public double WindowY { get; set; } = -1;
    public bool IsMaximized { get; set; }
    public double PaneWidth { get; set; } = 213.0;
    public int Repeat { get; set; }
    public bool Shuffle { get; set; }
    public string ScreenshotDir { get; set; } = "";
    public bool ScreenshotSubtitle { get; set; } = true;
    public Dictionary<string, double> ResumePositions { get; set; } = [];
    public Dictionary<string, string> ExternalSubtitles { get; set; } = [];
    public bool AutoPosterFetch { get; set; }
}

public static class SettingsService
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FilmField");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Current { get; private set; } = Load();

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
            }
        }
        catch
        {
        }
        return new AppSettings();
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, Options));
        }
        catch
        {
        }
    }
}
