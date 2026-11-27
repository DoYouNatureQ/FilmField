using System.Reflection;
using FilmField.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FilmField.Views;

public sealed partial class SettingsPage : Page
{
    private bool _syncing;

    public SettingsPage()
    {
        InitializeComponent();
    }

    internal string L(string key) => LocalizationService.L(key);

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _syncing = true;
        StartupToggle.IsOn = StartupService.IsEnabled();
        ThemeCombo.SelectedIndex = Math.Clamp(SettingsService.Current.ThemeMode, 0, 2);
        LanguageCombo.SelectedIndex = Math.Clamp(SettingsService.Current.LanguageMode, 0, 13);
        DecoderCombo.SelectedIndex = SettingsService.Current.UseFfmpeg ? 0 : 1;
        ResumeToggle.IsOn = SettingsService.Current.ResumeEnabled;
        ClickVideoToggle.IsOn = SettingsService.Current.ClickVideoToToggle;
        PausePanelToggle.IsOn = SettingsService.Current.PauseOnPanelSwitch;
        ArrowScanToggle.IsOn = SettingsService.Current.ArrowScanRate;
        SelectByTag(SkipCombo, SettingsService.Current.SkipSeconds);
        SelectByTag(SlowCombo, SettingsService.Current.ScanSlowRate);
        SelectByTag(FastCombo, SettingsService.Current.ScanFastRate);
        ScreenshotDirBox.Text = ScreenshotService.TargetDir;
        ScreenshotSubtitleToggle.IsOn = SettingsService.Current.ScreenshotSubtitle;
        AutoPosterToggle.IsOn = SettingsService.Current.AutoPosterFetch;
        SyncScanOptions();
        _syncing = false;
        UpdateCacheSize();
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        StartupService.SetEnabled(StartupToggle.IsOn);
        if (!StartupService.IsEnabled())
        {
            _syncing = true;
            StartupToggle.IsOn = false;
            _syncing = false;
        }
    }

    private void PausePanelToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.PauseOnPanelSwitch = PausePanelToggle.IsOn;
        SettingsService.Save();
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.LanguageMode = LanguageCombo.SelectedIndex;
        SettingsService.Save();
        LocalizationService.NotifyChanged();
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        ThemeService.SetMode(ThemeCombo.SelectedIndex);
    }

    private void DecoderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.UseFfmpeg = DecoderCombo.SelectedIndex == 0;
        SettingsService.Save();
    }

    private void ResumeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ResumeEnabled = ResumeToggle.IsOn;
        SettingsService.Save();
    }

    private void ClickVideoToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ClickVideoToToggle = ClickVideoToggle.IsOn;
        SettingsService.Save();
    }

    private void ArrowScanToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SyncScanOptions();
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ArrowScanRate = ArrowScanToggle.IsOn;
        SettingsService.Save();
    }

    private void SyncScanOptions()
    {
        var enabled = ArrowScanToggle.IsOn;
        ScanOptions.Opacity = enabled ? 1.0 : 0.45;
        SkipCombo.IsEnabled = enabled;
        SlowCombo.IsEnabled = enabled;
        FastCombo.IsEnabled = enabled;
    }

    private void SkipCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.SkipSeconds = (int)ReadTag(SkipCombo, 5);
        SettingsService.Save();
    }

    private void SlowCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ScanSlowRate = ReadTag(SlowCombo, 0.5);
        SettingsService.Save();
    }

    private void FastCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ScanFastRate = ReadTag(FastCombo, 2.0);
        SettingsService.Save();
    }

    private static void SelectByTag(ComboBox combo, double value)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem { Tag: string tag } && double.TryParse(tag, out var tagValue) &&
                Math.Abs(tagValue - value) < 0.001)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private static double ReadTag(ComboBox combo, double fallback) =>
        combo.SelectedItem is ComboBoxItem { Tag: string tag } && double.TryParse(tag, out var value) ? value : fallback;

    private void ScreenshotSubtitleToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.ScreenshotSubtitle = ScreenshotSubtitleToggle.IsOn;
        SettingsService.Save();
    }

    private void AutoPosterToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }
        SettingsService.Current.AutoPosterFetch = AutoPosterToggle.IsOn;
        SettingsService.Save();
    }

    private async void ScreenshotDirBrowse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        picker.FileTypeFilter.Add("*");

        if (MainWindow.Instance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));
        }

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return;
        }
        SettingsService.Current.ScreenshotDir = folder.Path;
        SettingsService.Save();
        ScreenshotDirBox.Text = folder.Path;
    }

    private void ScreenshotDirReset_Click(object sender, RoutedEventArgs e)
    {
        SettingsService.Current.ScreenshotDir = "";
        SettingsService.Save();
        ScreenshotDirBox.Text = ScreenshotService.DefaultDir;
    }

    private void OpenNotices_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md");
        if (!File.Exists(path))
        {
            return;
        }
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private async void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        PreviewService.ClearCache();
        LibraryService.ClearThumbnailCache();
        UpdateCacheSize();
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = L("清理完成"),
            Content = L("缩略图与预览缓存已删除，将在需要时重新生成。"),
            CloseButtonText = L("确定"),
            DefaultButton = ContentDialogButton.Close
        };
        await dialog.ShowAsync();
        Focus(FocusState.Pointer);
    }

    private void UpdateCacheSize()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText.Text = $"FilmField {version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
        var bytes = PreviewService.CacheSize() + LibraryService.ThumbnailCacheSize();
        CacheSizeText.Text = bytes > 0 ? LocalizationService.L("当前占用 {0} MB", (bytes / (1024.0 * 1024.0)).ToString("0.#")) : L("暂无缓存");
    }
}
