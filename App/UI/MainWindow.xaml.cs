using System.Runtime.InteropServices;
using FilmField.Services;
using FilmField.Views;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace FilmField;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public event Action? WindowModeChanged;

    public bool IsFullscreen => _isFullscreen;

    public bool IsWindowFullscreen => _isWindowFullscreen;

    public int ActiveBackdropMode => _activeBackdropMode;

    public bool IsImmersive => _isFullscreen || _isWindowFullscreen;

    public bool IsWindowMaximized =>
        AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Maximized };

    private bool _isFullscreen;
    private bool _isWindowFullscreen;
    private bool _fullscreenBusy;
    private bool _restoreMaximized;
    private SizeInt32 _windowedSize;
    private PointInt32 _windowedPosition;
    private int _activeBackdropMode = -1;
    private Type? _lastPageType;
    private object? _lastPageParam;
    private Button? _paneToggle;
    private NavigationViewItem? _navDropTarget;
    private const double MinPaneWidth = 213;
    private const int CornerPreferenceAttribute = 33;
    private const int TransitionsDisabledAttribute = 3;
    private const int RoundCorners = 2;
    private const int DontRoundCorners = 1;
    private const int BorderColorAttribute = 34;
    private const int BorderColorNone = unchecked((int)0xFFFFFFFE);
    private const int BorderColorDefault = unchecked((int)0xFFFFFFFF);
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _sizeSaveTimer;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _scanTimer;
    private VirtualKey _scanKey;
    private bool _scanning;
    private double _scanOriginRate;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        Title = "FilmField";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ApplyTheme();
        RootGrid.ActualThemeChanged += (_, _) =>
        {
            UpdateTitleBarColors();
            ApplyBackdrop();
        };
        RestoreWindowBounds();

        RootGrid.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(RootGrid_KeyDown), true);
        RootGrid.AddHandler(UIElement.KeyUpEvent, new KeyEventHandler(RootGrid_KeyUp), true);
        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                CancelScan();
            }
        };
        RootGrid.Loaded += (_, _) =>
        {
            ApplyNavLanguage();
            RestoreLibraries();
            Nav.SelectedItem = Nav.MenuItems[0];
            SetWindowCorner(WindowNative.GetWindowHandle(this), RoundCorners);
            _paneToggle = FindDescendant<Button>(Nav, "TogglePaneButton");
            ApplyPaneToggleWidth();
        };

        Nav.OpenPaneLength = SettingsService.Current.PaneWidth;
        ContentFrame.Navigated += (_, e) =>
        {
            _lastPageType = e.SourcePageType;
            _lastPageParam = e.Parameter;
        };
        LocalizationService.LanguageChanged += OnLanguageChanged;
        ContentFrame.Navigating += (_, _) =>
        {
            if (SettingsService.Current.PauseOnPanelSwitch &&
                ContentFrame.Content is PlayerPage &&
                PlaybackService.Instance.IsPlaying)
            {
                PlaybackService.Instance.Pause();
            }
        };
        Nav.SizeChanged += (_, _) =>
        {
            Nav.OpenPaneLength = ClampPane(Nav.OpenPaneLength);
            UpdatePaneThumb();
        };
        Nav.PaneClosing += (_, _) =>
        {
            PaneThumb.Margin = new Thickness(Nav.CompactPaneLength - 3, 0, 0, 0);
            ApplyPaneToggleWidth();
        };
        Nav.PaneOpening += (_, _) => UpdatePaneThumb();

        AppWindow.Changed += OnAppWindowChanged;
        Closed += (_, _) => SaveWindowState();
        _sizeSaveTimer = DispatcherQueue.CreateTimer();
        _sizeSaveTimer.Interval = TimeSpan.FromMilliseconds(500);
        _sizeSaveTimer.IsRepeating = false;
        _sizeSaveTimer.Tick += (_, _) => SaveWindowState();

        _scanTimer = DispatcherQueue.CreateTimer();
        _scanTimer.Interval = TimeSpan.FromMilliseconds(320);
        _scanTimer.IsRepeating = false;
        _scanTimer.Tick += (_, _) => BeginScan();

        ApplyPersistedPlaybackSettings();
    }

    private void RestoreWindowBounds()
    {
        var settings = SettingsService.Current;
        var width = settings.WindowWidth >= 320 ? (int)settings.WindowWidth : 1180;
        var height = settings.WindowHeight >= 240 ? (int)settings.WindowHeight : 760;
        AppWindow.Resize(new SizeInt32(width, height));
        if (settings.WindowX >= 0 && settings.WindowY >= 0 &&
            IsWindowRectVisible((int)settings.WindowX, (int)settings.WindowY, width, height))
        {
            AppWindow.Move(new PointInt32((int)settings.WindowX, (int)settings.WindowY));
        }
        if (settings.IsMaximized)
        {
            GetOverlappedPresenter()?.Maximize();
        }
    }

    private bool IsWindowRectVisible(int x, int y, int width, int height)
    {
        var area = DisplayArea.GetFromPoint(
            new PointInt32(x + width / 2, y + height / 2), DisplayAreaFallback.Nearest);
        var bounds = area.OuterBounds;
        return x < bounds.X + bounds.Width && x + width > bounds.X &&
            y < bounds.Y + bounds.Height && y + height > bounds.Y;
    }

    public void ApplyTheme()
    {
        RootGrid.RequestedTheme = ThemeService.Current;
        UpdateTitleBarColors();
        ApplyBackdrop();
    }

    public void ApplyNavLanguage()
    {
        SetNavLabel("player", "nav.播放");
        SetNavLabel("queue", "播放队列");
        SetNavLabel("home", "主页");
        SetNavLabel("new-library", "新建库");
        SetNavLabel("history", "历史记录");
        if (Nav.SettingsItem is NavigationViewItem settingsItem)
        {
            settingsItem.Content = LocalizationService.L("设置");
        }
    }

    private void SetNavLabel(string tag, string key)
    {
        var item = Nav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == tag) ??
            Nav.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => (i.Tag as string) == tag);
        if (item is not null)
        {
            item.Content = LocalizationService.L(key);
        }
    }

    private void OnLanguageChanged()
    {
        ApplyNavLanguage();
        if (_lastPageType is not null && ContentFrame.Content is not null)
        {
            ContentFrame.Navigate(_lastPageType, _lastPageParam);
        }
    }

    public void ApplyBackdrop()
    {
        var mode = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621)
            ? AppSettings.BackdropAcrylic
            : AppSettings.BackdropOff;
        if (_activeBackdropMode != mode)
        {
            _activeBackdropMode = mode;
            SystemBackdrop = mode == AppSettings.BackdropAcrylic ? new DesktopAcrylicBackdrop() : null;
        }
        var immersive = IsImmersive;
        RootGrid.Background = immersive
            ? new SolidColorBrush(Colors.Black)
            : mode == AppSettings.BackdropOff
                ? ThemedBrush("AppBackgroundBrush")
                : ThemedBrush("AppBackdropTintBrush");
        (ContentFrame.Content as PlayerPage)?.SyncMaterial(!immersive && mode != AppSettings.BackdropOff);
    }

    public Brush ThemedBrush(string key)
    {
        var themeKey = RootGrid.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        return Application.Current.Resources.ThemeDictionaries[themeKey] is ResourceDictionary dict &&
            dict.TryGetValue(key, out var value) && value is Brush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);
    }

    private void UpdateTitleBarColors()
    {
        if (AppWindow.TitleBar is not { } titleBar)
        {
            return;
        }
        var theme = RootGrid.ActualTheme;
        var dark = theme == ElementTheme.Dark ||
            (theme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
        var foreground = dark ? Colors.White : Color.FromArgb(0xFF, 0x1A, 0x1A, 0x1A);
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedForegroundColor = foreground;
        titleBar.ButtonInactiveForegroundColor = dark
            ? Color.FromArgb(0xFF, 0x8A, 0x8A, 0x8A)
            : Color.FromArgb(0xFF, 0x70, 0x70, 0x70);
        titleBar.ButtonHoverBackgroundColor = dark
            ? Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x14, 0x00, 0x00, 0x00);
        titleBar.ButtonPressedBackgroundColor = dark
            ? Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x22, 0x00, 0x00, 0x00);
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange)
        {
            WindowModeChanged?.Invoke();
        }
        if (!args.DidSizeChange && !args.DidPositionChange || _isFullscreen)
        {
            return;
        }
        _sizeSaveTimer.Stop();
        _sizeSaveTimer.Start();
    }

    private void SaveWindowState()
    {
        var settings = SettingsService.Current;
        settings.IsMaximized = IsWindowMaximized;
        if (!IsImmersive && !IsWindowMaximized)
        {
            settings.WindowWidth = AppWindow.Size.Width;
            settings.WindowHeight = AppWindow.Size.Height;
            settings.WindowX = AppWindow.Position.X;
            settings.WindowY = AppWindow.Position.Y;
        }
        SettingsService.Save();
    }

    private double ClampPane(double width)
    {
        var max = Math.Max(MinPaneWidth, Nav.ActualWidth * 0.4);
        return Math.Clamp(width, MinPaneWidth, max);
    }

    private void UpdatePaneThumb()
    {
        PaneThumb.Margin = new Thickness(Nav.OpenPaneLength - 3, 0, 0, 0);
        ApplyPaneToggleWidth();
    }

    private void ApplyPaneToggleWidth()
    {
        if (_paneToggle is not null)
        {
            _paneToggle.MinWidth = Nav.IsPaneOpen ? Nav.OpenPaneLength : Nav.CompactPaneLength;
        }
    }

    private static T? FindDescendant<T>(DependencyObject node, string name) where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            if (VisualTreeHelper.GetChild(node, i) is not FrameworkElement child)
            {
                continue;
            }
            if (child is T match && child.Name == name)
            {
                return match;
            }
            if (FindDescendant<T>(child, name) is { } nested)
            {
                return nested;
            }
        }
        return null;
    }

    private void PaneThumb_DragMove(object? sender, PointerRoutedEventArgs e)
    {
        Nav.OpenPaneLength = ClampPane(e.GetCurrentPoint(Nav).Position.X);
        UpdatePaneThumb();
    }

    private void PaneThumb_DragEnd(object? sender, PointerRoutedEventArgs e)
    {
        SettingsService.Current.PaneWidth = Nav.OpenPaneLength;
        SettingsService.Save();
    }

    private static void ApplyPersistedPlaybackSettings()
    {
        var settings = SettingsService.Current;
        var playback = PlaybackService.Instance;
        playback.Volume = settings.Volume;
        playback.IsMuted = settings.Muted;
        playback.Rate = settings.Rate;
        PlaylistService.Instance.Repeat = (PlaylistRepeatMode)Math.Clamp(settings.Repeat, 0, 2);
        PlaylistService.Instance.IsShuffle = settings.Shuffle;
    }

    private void RestoreLibraries()
    {
        for (var i = Nav.MenuItems.Count - 1; i >= 0; i--)
        {
            if (Nav.MenuItems[i] is NavigationViewItem { Tag: string tag } &&
                tag.StartsWith("library:", StringComparison.Ordinal))
            {
                Nav.MenuItems.RemoveAt(i);
            }
        }
        foreach (var library in LibraryService.Instance.Libraries.Skip(1))
        {
            AddLibraryItem(library);
        }
    }

    private NavigationViewItem AddLibraryItem(VideoLibrary library)
    {
        var item = new NavigationViewItem
        {
            Content = library.Name,
            Tag = $"library:{library.Id}",
            Icon = new FontIcon { Glyph = "\uE8B7" }
        };
        item.ContextFlyout = BuildLibraryFlyout(item, library);
        Nav.MenuItems.Insert(Nav.MenuItems.IndexOf(NewLibraryItem), item);
        return item;
    }

    private MenuFlyout BuildLibraryFlyout(NavigationViewItem item, VideoLibrary library)
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(MenuAction(LocalizationService.L("重命名"), "\uE8AC", () => _ = RenameLibraryAsync(item, library)));
        flyout.Items.Add(MenuAction(LocalizationService.L("删除"), "\uE74D", () => _ = DeleteLibraryAsync(item, library)));
        return flyout;
    }

    private static MenuFlyoutItem MenuAction(string text, string glyph, Action action)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
        item.Click += (_, _) => action();
        return item;
    }

    private async Task RenameLibraryAsync(NavigationViewItem item, VideoLibrary library)
    {
        var nameBox = new TextBox
        {
            Text = library.Name,
            PlaceholderText = LocalizationService.L("库名称")
        };
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = LocalizationService.L("重命名库"),
            Content = nameBox,
            PrimaryButtonText = LocalizationService.L("确定"),
            CloseButtonText = LocalizationService.L("取消"),
            DefaultButton = ContentDialogButton.Primary
        };
        nameBox.Loaded += (_, _) =>
        {
            nameBox.Focus(FocusState.Programmatic);
            nameBox.SelectAll();
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            RootGrid.Focus(FocusState.Pointer);
            return;
        }
        RootGrid.Focus(FocusState.Pointer);
        LibraryService.Instance.Rename(library, nameBox.Text);
        item.Content = library.Name;
        if (ContentFrame.Content is LibraryPage page)
        {
            page.RefreshAfterChange();
        }
    }

    private async Task DeleteLibraryAsync(NavigationViewItem item, VideoLibrary library)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = LocalizationService.L("删除「{0}」？", library.Name),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = LocalizationService.L("只删除这个库与其中的观看记录，磁盘上的视频文件不会被删除。"),
            FontSize = 12,
            Opacity = 0.55,
            TextWrapping = TextWrapping.Wrap
        });

        var confirm = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = LocalizationService.L("删除库"),
            Content = panel,
            PrimaryButtonText = LocalizationService.L("删除"),
            CloseButtonText = LocalizationService.L("取消"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            RootGrid.Focus(FocusState.Pointer);
            return;
        }
        RootGrid.Focus(FocusState.Pointer);

        var wasSelected = ReferenceEquals(Nav.SelectedItem, item);
        if (!LibraryService.Instance.Delete(library))
        {
            return;
        }
        Nav.MenuItems.Remove(item);
        if (wasSelected)
        {
            Nav.SelectedItem = Nav.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(i => (i.Tag as string) == "home") ?? Nav.MenuItems[0];
        }
    }

    private async void Nav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var tag = (args.InvokedItemContainer as FrameworkElement)?.Tag?.ToString();
        if (tag == "new-library")
        {
            await CreateLibraryAsync();
            return;
        }
        if (tag is null || !ReferenceEquals(Nav.SelectedItem, args.InvokedItemContainer))
        {
            return;
        }
        LibraryService.Instance.ResetView();
        if (tag == "home")
        {
            ContentFrame.Navigate(typeof(LibraryPage));
            return;
        }
        if (tag.StartsWith("library:", StringComparison.Ordinal))
        {
            ContentFrame.Navigate(typeof(LibraryPage), tag["library:".Length..]);
        }
    }

    public void SyncLibraryName(VideoLibrary library)
    {
        var tag = $"library:{library.Id}";
        foreach (var item in Nav.MenuItems.OfType<NavigationViewItem>())
        {
            if ((item.Tag as string) == tag)
            {
                item.Content = library.Name;
            }
        }
    }

    public bool SelectLibrary(string id)
    {
        var tag = id == LibraryService.Instance.Home.Id ? "home" : $"library:{id}";
        var target = Nav.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (item.Tag as string) == tag);
        if (target is null || ReferenceEquals(Nav.SelectedItem, target))
        {
            return false;
        }
        Nav.SelectedItem = target;
        return true;
    }

    private async Task CreateLibraryAsync()
    {
        var libraries = LibraryService.Instance;
        var nameBox = new TextBox
        {
            Text = libraries.SuggestName(),
            PlaceholderText = LocalizationService.L("库名称")
        };
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = LocalizationService.L("新建库"),
            Content = nameBox,
            PrimaryButtonText = LocalizationService.L("创建"),
            CloseButtonText = LocalizationService.L("取消"),
            DefaultButton = ContentDialogButton.Primary
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            RootGrid.Focus(FocusState.Pointer);
            return;
        }
        RootGrid.Focus(FocusState.Pointer);
        var library = libraries.Create(nameBox.Text);
        Nav.SelectedItem = AddLibraryItem(library);
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }
        var tag = (args.SelectedItem as FrameworkElement)?.Tag?.ToString();
        if (tag is null)
        {
            return;
        }
        if (tag.StartsWith("library:", StringComparison.Ordinal))
        {
            LibraryService.Instance.ResetView();
            ContentFrame.Navigate(typeof(LibraryPage), tag["library:".Length..]);
            return;
        }
        switch (tag)
        {
            case "player":
                ContentFrame.Navigate(typeof(PlayerPage));
                break;
            case "home":
                LibraryService.Instance.ResetView();
                ContentFrame.Navigate(typeof(LibraryPage));
                break;
            case "queue":
                ContentFrame.Navigate(typeof(QueuePage));
                break;
            case "history":
                ContentFrame.Navigate(typeof(HistoryPage));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }

    public async Task PlayFromLibraryAsync(string path)
    {
        var paths = LibraryService.Instance.Entries
            .Where(e => e.IsVideo)
            .Select(e => e.Path)
            .ToList();
        if (!paths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            paths.Add(path);
        }

        var files = await Task.Run(async () =>
        {
            var list = new List<StorageFile>();
            foreach (var item in paths)
            {
                try
                {
                    list.Add(await StorageFile.GetFileFromPathAsync(item));
                }
                catch
                {
                }
            }
            return list;
        });

        if (files.Count == 0)
        {
            return;
        }

        var index = files.FindIndex(f => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (ContentFrame.Content is not PlayerPage)
        {
            Nav.SelectedItem = Nav.MenuItems[0];
        }
        if (ContentFrame.Content is PlayerPage page)
        {
            await page.LoadFilesAsync(files, startIndex: Math.Max(index, 0));
        }
    }

    public void ToggleFullscreen()
    {
        if (_fullscreenBusy)
        {
            return;
        }
        _fullscreenBusy = true;
        var handle = WindowNative.GetWindowHandle(this);
        try
        {
            SetWindowTransitions(handle, true);
            if (_isFullscreen)
            {
                ExitFullscreen();
            }
            else
            {
                if (_isWindowFullscreen)
                {
                    _isWindowFullscreen = false;
                }
                EnterFullscreen();
            }
            FinishWindowModeChange();
            DispatcherQueue.TryEnqueue(() => SetWindowTransitions(handle, false));
        }
        finally
        {
            _fullscreenBusy = false;
        }
    }

    public void ToggleWindowFullscreen()
    {
        if (_fullscreenBusy)
        {
            return;
        }
        _fullscreenBusy = true;
        try
        {
            if (_isFullscreen)
            {
                ExitFullscreen();
                _isWindowFullscreen = true;
            }
            else
            {
                _isWindowFullscreen = !_isWindowFullscreen;
            }
            FinishWindowModeChange();
        }
        finally
        {
            _fullscreenBusy = false;
        }
    }

    private OverlappedPresenter? GetOverlappedPresenter()
    {
        if (AppWindow.Presenter is not OverlappedPresenter)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }
        return AppWindow.Presenter as OverlappedPresenter;
    }

    private void EnterFullscreen()
    {
        _restoreMaximized = IsWindowMaximized;
        _windowedSize = AppWindow.Size;
        _windowedPosition = AppWindow.Position;
        if (_restoreMaximized && GetOverlappedPresenter() is { } presenter)
        {
            presenter.Restore();
        }
        _isFullscreen = true;
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void ExitFullscreen()
    {
        _isFullscreen = false;
        AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        if (_restoreMaximized)
        {
            GetOverlappedPresenter()?.Maximize();
        }
        else
        {
            AppWindow.MoveAndResize(new RectInt32(
                _windowedPosition.X, _windowedPosition.Y,
                _windowedSize.Width, _windowedSize.Height));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void SetWindowCorner(IntPtr handle, int preference)
    {
        var value = preference;
        DwmSetWindowAttribute(handle, CornerPreferenceAttribute, ref value, sizeof(int));
    }

    private void SetWindowTransitions(IntPtr handle, bool disabled)
    {
        var value = disabled ? 1 : 0;
        DwmSetWindowAttribute(handle, TransitionsDisabledAttribute, ref value, sizeof(int));
    }

    private void SetWindowBorderColor(IntPtr handle, int color)
    {
        var value = color;
        DwmSetWindowAttribute(handle, BorderColorAttribute, ref value, sizeof(int));
    }

    private void FinishWindowModeChange()
    {
        ApplyWindowChrome();
        WindowModeChanged?.Invoke();
    }

    private void ApplyWindowChrome()
    {
        var immersive = IsImmersive;
        AppTitleBar.Visibility = immersive ? Visibility.Collapsed : Visibility.Visible;
        TitleRow.Height = immersive ? new GridLength(0) : new GridLength(40);
        Nav.IsPaneVisible = !immersive;
        PaneThumb.Visibility = immersive ? Visibility.Collapsed : Visibility.Visible;
        if (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(ContentFrame)) is Grid contentGrid)
        {
            contentGrid.CornerRadius = immersive
                ? new CornerRadius(0)
                : new CornerRadius(8, 0, 0, 0);
        }
        SetWindowCorner(
            WindowNative.GetWindowHandle(this),
            immersive ? DontRoundCorners : RoundCorners);
        SetWindowBorderColor(
            WindowNative.GetWindowHandle(this),
            immersive ? BorderColorNone : BorderColorDefault);
        ApplyBackdrop();
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        var playback = PlaybackService.Instance;
        switch (e.Key)
        {
            case VirtualKey.Space:
                playback.Toggle();
                (ContentFrame.Content as PlayerPage)?.SyncPlayState();
                e.Handled = true;
                break;
            case VirtualKey.Left:
            case VirtualKey.Right:
                if (HandleScanKeyDown(e.Key))
                {
                    e.Handled = true;
                    break;
                }
                playback.Skip(e.Key == VirtualKey.Right ? SkipSeconds : -SkipSeconds);
                e.Handled = true;
                break;
            case VirtualKey.Up:
                playback.IsMuted = false;
                playback.Volume = Math.Min(1.0, playback.Volume + 0.05);
                CommitVolume(playback);
                e.Handled = true;
                break;
            case VirtualKey.Down:
                playback.Volume = Math.Max(0.0, playback.Volume - 0.05);
                CommitVolume(playback);
                e.Handled = true;
                break;
            case VirtualKey.M:
                playback.IsMuted = !playback.IsMuted;
                CommitVolume(playback);
                e.Handled = true;
                break;
            case VirtualKey.F:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case VirtualKey.Escape:
                if (_isFullscreen)
                {
                    ToggleFullscreen();
                    e.Handled = true;
                }
                else if (_isWindowFullscreen)
                {
                    ToggleWindowFullscreen();
                    e.Handled = true;
                }
                break;
        }
    }

    private void CommitVolume(PlaybackService playback)
    {
        PersistVolumeSettings(playback);
        if (ContentFrame.Content is PlayerPage page)
        {
            page.SyncVolumeUI();
            page.ShowVolumeOsd((int)Math.Round(playback.Volume * 100), playback.IsMuted);
        }
    }

    private void RootGrid_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (_scanKey == default || e.Key != _scanKey)
        {
            return;
        }
        _scanTimer.Stop();
        var key = _scanKey;
        var scanned = _scanning;
        _scanKey = default;
        _scanning = false;
        e.Handled = true;
        if (scanned)
        {
            PlaybackService.Instance.Rate = _scanOriginRate;
            (ContentFrame.Content as PlayerPage)?.HideRateOsd();
            return;
        }
        PlaybackService.Instance.Skip(key == VirtualKey.Right ? SkipSeconds : -SkipSeconds);
    }

    private bool HandleScanKeyDown(VirtualKey key)
    {
        if (!SettingsService.Current.ArrowScanRate)
        {
            return false;
        }
        if (_scanKey != default)
        {
            return true;
        }
        _scanKey = key;
        _scanTimer.Stop();
        _scanTimer.Start();
        return true;
    }

    private void BeginScan()
    {
        var playback = PlaybackService.Instance;
        if (_scanKey == default || playback.Session.PlaybackState != MediaPlaybackState.Playing)
        {
            return;
        }
        var rate = _scanKey == VirtualKey.Right
            ? Math.Clamp(SettingsService.Current.ScanFastRate, 1.0, 3.0)
            : Math.Clamp(SettingsService.Current.ScanSlowRate, 0.25, 0.99);
        _scanning = true;
        _scanOriginRate = playback.Rate;
        playback.Rate = rate;
        (ContentFrame.Content as PlayerPage)?.ShowRateOsd(rate);
    }

    private void CancelScan()
    {
        _scanTimer.Stop();
        if (_scanning)
        {
            PlaybackService.Instance.Rate = _scanOriginRate;
            (ContentFrame.Content as PlayerPage)?.HideRateOsd();
        }
        _scanKey = default;
        _scanning = false;
    }

    private static int SkipSeconds => Math.Clamp(SettingsService.Current.SkipSeconds, 1, 300);

    private static void PersistVolumeSettings(PlaybackService playback)
    {
        SettingsService.Current.Volume = playback.Volume;
        SettingsService.Current.Muted = playback.IsMuted;
        SettingsService.Save();
    }

    private void Nav_DragOver(object sender, DragEventArgs e)
    {
        var edge = Nav.IsPaneOpen ? Nav.OpenPaneLength : Nav.CompactPaneLength;
        var position = e.GetPosition(Nav);
        if (position.X < 0 || position.X > edge)
        {
            SetNavDropTarget(null);
            return;
        }
        var item = TargetNavItem(e);
        var library = LibraryOf(item);
        if (PosterDrag.Entries.Count == 0 ||
            library is null ||
            ReferenceEquals(library, LibraryService.Instance.Active) ||
            ReferenceEquals(library, LibraryService.Instance.Home))
        {
            SetNavDropTarget(null);
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }
        var copy = LibraryService.Instance.IsOverview;
        SetNavDropTarget(item);
        e.AcceptedOperation = copy ? DataPackageOperation.Copy : DataPackageOperation.Move;
        e.DragUIOverride.Caption = copy
            ? LocalizationService.L("复制到「{0}」", library.Name)
            : LocalizationService.L("移动到「{0}」", library.Name);
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsGlyphVisible = false;
    }

    private void Nav_DragLeave(object sender, DragEventArgs e) => SetNavDropTarget(null);

    private async void Nav_Drop(object sender, DragEventArgs e)
    {
        var item = TargetNavItem(e);
        var library = LibraryOf(item);
        var entries = PosterDrag.Entries;
        SetNavDropTarget(null);
        if (library is null || entries.Count == 0 ||
            ReferenceEquals(library, LibraryService.Instance.Active) ||
            ReferenceEquals(library, LibraryService.Instance.Home))
        {
            return;
        }
        var move = !LibraryService.Instance.IsOverview;
        foreach (var entry in entries)
        {
            await LibraryService.Instance.TransferAsync(entry, library, move);
        }
        (ContentFrame.Content as LibraryPage)?.RefreshAfterChange(LocalizationService.L(
            move ? "已移动到「{0}」" : "已复制到「{0}」", library.Name));
    }

    private static NavigationViewItem? TargetNavItem(DragEventArgs e) =>
        FindAncestor<NavigationViewItem>(e.OriginalSource as DependencyObject);

    private static VideoLibrary? LibraryOf(NavigationViewItem? item) => (item?.Tag as string) switch
    {
        "home" => LibraryService.Instance.Home,
        { } tag when tag.StartsWith("library:", StringComparison.Ordinal) =>
            LibraryService.Instance.Find(tag["library:".Length..]),
        _ => null
    };

    private void SetNavDropTarget(NavigationViewItem? item)
    {
        if (ReferenceEquals(_navDropTarget, item))
        {
            return;
        }
        _navDropTarget?.ClearValue(Control.BackgroundProperty);
        _navDropTarget = item;
        if (item is null)
        {
            return;
        }
        var color = Application.Current.Resources.TryGetValue("SystemAccentColor", out var accent) && accent is Color value
            ? value
            : Color.FromArgb(0xFF, 0x60, 0x60, 0x60);
        item.Background = new SolidColorBrush(Color.FromArgb(0x33, color.R, color.G, color.B));
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T match)
            {
                return match;
            }
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
    }

    private void RootGrid_DragOver(object sender, DragEventArgs e)
    {
        if (PosterDrag.Entries.Count > 0)
        {
            return;
        }
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
    }

    private async void RootGrid_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var items = await e.DataView.GetStorageItemsAsync();
        var files = items.OfType<StorageFile>()
            .Where(f => PlaybackService.VideoExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()))
            .ToList();
        var subtitleFiles = items.OfType<StorageFile>()
            .Where(f => SubtitleService.SubtitleExtensions.Contains(Path.GetExtension(f.Name).ToLowerInvariant()))
            .ToList();
        if (files.Count == 0 && subtitleFiles.Count == 0)
        {
            return;
        }

        if (ContentFrame.Content is not PlayerPage)
        {
            Nav.SelectedItem = Nav.MenuItems[0];
        }
        if (ContentFrame.Content is PlayerPage page)
        {
            if (files.Count > 0)
            {
                await page.LoadFilesAsync(files);
            }
            else
            {
                page.LoadSubtitlePath(subtitleFiles[0].Path);
            }
        }
    }
}

public sealed partial class PaneThumbBorder : Control
{
    public event EventHandler<PointerRoutedEventArgs>? DragMove;
    public event EventHandler<PointerRoutedEventArgs>? DragEnd;

    private bool _dragging;

    public PaneThumbBorder()
    {
        PointerPressed += (_, e) =>
        {
            _dragging = true;
            CapturePointer(e.Pointer);
        };
        PointerMoved += (_, e) =>
        {
            if (_dragging)
            {
                DragMove?.Invoke(this, e);
            }
        };
        PointerReleased += EndDrag;
        PointerCaptureLost += EndDrag;
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            ProtectedCursor = null;
        }
    }

    private void EndDrag(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }
        _dragging = false;
        ReleasePointerCapture(e.Pointer);
        ProtectedCursor = null;
        DragEnd?.Invoke(this, e);
    }
}
