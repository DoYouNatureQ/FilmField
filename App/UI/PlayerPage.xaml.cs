using FilmField.Services;
using FFmpegInteropX;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FilmField.Views;

public sealed partial class PlayerPage : Page
{
    private const double SeekDragThreshold = 4;

    private static string _title = "";

    private List<SubtitleCue> _cues = [];
    private SubtitleCue? _renderedCue;
    private double? _pendingResumeSeconds;
    private double _lastSavedResume;
    private bool _isSeeking;
    private bool _seekDragUnlocked;
    private double _seekPressX;
    private double _seekAnchorValue;
    private bool _resumeAfterSeek;
    private bool _suppressRestore;
    private string[]? _previewFrames;
    private BitmapImage?[]? _previewBitmaps;
    private int _previewFrameIndex = -1;
    private double _previewRatio;
    private double _previewPageX;
    private bool _pointerOnSlider;
    private bool _tapTogglePending;
    private bool _playingBeforeTap;
    private long _tapToggleTick;
    private long _doubleTapTick;
    private CancellationTokenSource? _previewCts;
    private FrameGrabber? _previewGrabber;
    private string? _previewGrabberPath;
    private CancellationTokenSource? _exactCts;
    private readonly SemaphoreSlim _exactGate = new(1, 1);
    private readonly Dictionary<int, BitmapImage> _exactCache = [];
    private int _embeddedTrackIndex = -1;
    private bool _overlayShown = true;
    private int _loadGen;

    private readonly DispatcherQueueTimer _positionTimer;
    private readonly DispatcherQueueTimer _hideTimer;
    private readonly DispatcherQueueTimer _previewShowTimer;
    private readonly DispatcherQueueTimer _previewExactTimer;
    private readonly DispatcherQueueTimer _osdTimer;
    private readonly DispatcherQueueTimer _volumeOsdTimer;
    private readonly DispatcherQueueTimer _toastTimer;
    private readonly DispatcherQueueTimer _scaleFlushTimer;

    private PlaybackService Playback => PlaybackService.Instance;
    private PlaylistService Playlist => PlaylistService.Instance;

    internal string L(string key) => LocalizationService.L(key);

    private SpriteVisual? _barBlurVisual;
    private CompositionRoundedRectangleGeometry? _barBlurClip;

    public PlayerPage()
    {
        InitializeComponent();

        PlayerElement.SetMediaPlayer(Playback.Player);
        SyncVideoVisibility();
        SetupBarBlur();

        Playback.StateChanged += OnPlaybackStateChanged;
        Playback.MediaOpened += OnMediaOpened;
        Playback.MediaEnded += OnMediaEnded;
        Playback.MediaFailed += OnMediaFailed;

        Playlist.QueueChanged += OnPlaylistQueueChanged;
        Playlist.CurrentIndexChanged += OnPlaylistCurrentIndexChanged;

        _positionTimer = DispatcherQueue.CreateTimer();
        _positionTimer.Interval = TimeSpan.FromMilliseconds(200);
        _positionTimer.Tick += (_, _) =>
        {
            UpdatePositionUI();
            UpdateSubtitle();
            SaveResumeProgress();
        };

        ProgressSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(ProgressSlider_PointerPressed), true);
        ProgressSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(ProgressSlider_PointerReleased), true);
        ProgressSlider.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(ProgressSlider_PointerCaptureLost), true);
        ProgressSlider.AddHandler(PointerMovedEvent, new PointerEventHandler(ProgressSlider_PointerMoved), true);

        _hideTimer = DispatcherQueue.CreateTimer();
        _hideTimer.Interval = TimeSpan.FromSeconds(2.5);
        _hideTimer.IsRepeating = false;
        _hideTimer.Tick += (_, _) => HideOverlay();

        _previewShowTimer = DispatcherQueue.CreateTimer();
        _previewShowTimer.Interval = TimeSpan.FromMilliseconds(300);
        _previewShowTimer.IsRepeating = false;
        _previewShowTimer.Tick += (_, _) =>
        {
            if (_pointerOnSlider)
            {
                ShowPreview();
            }
        };

        _previewExactTimer = DispatcherQueue.CreateTimer();
        _previewExactTimer.Interval = TimeSpan.FromMilliseconds(200);
        _previewExactTimer.IsRepeating = false;
        _previewExactTimer.Tick += (_, _) =>
        {
            if (_pointerOnSlider || _isSeeking)
            {
                _ = LoadExactPreviewAsync();
            }
        };

        _osdTimer = DispatcherQueue.CreateTimer();
        _osdTimer.Interval = TimeSpan.FromMilliseconds(1200);
        _osdTimer.IsRepeating = false;
        _osdTimer.Tick += (_, _) => RateOsd.Visibility = Visibility.Collapsed;

        _volumeOsdTimer = DispatcherQueue.CreateTimer();
        _volumeOsdTimer.Interval = TimeSpan.FromMilliseconds(1200);
        _volumeOsdTimer.IsRepeating = false;
        _volumeOsdTimer.Tick += (_, _) => VolumeOsd.Visibility = Visibility.Collapsed;

        _toastTimer = DispatcherQueue.CreateTimer();
        _toastTimer.Interval = TimeSpan.FromSeconds(3.5);
        _toastTimer.IsRepeating = false;
        _toastTimer.Tick += (_, _) => TitleHost.Visibility = Visibility.Collapsed;

        _scaleFlushTimer = DispatcherQueue.CreateTimer();
        _scaleFlushTimer.Interval = TimeSpan.FromMilliseconds(200);
        _scaleFlushTimer.IsRepeating = false;
        _scaleFlushTimer.Tick += (_, _) => FlushPresentation();

        Root.AddHandler(TappedEvent, new TappedEventHandler(Root_Tapped), true);
        Root.SizeChanged += (_, _) =>
        {
            if (_renderedCue?.Image is { } image)
            {
                _ = RenderPgsAsync(image, _renderedCue);
            }
        };

        Unloaded += OnPageUnloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        SyncVolumeUI();
        SyncRateUI();
        SyncModeButtons();
        RefreshTransportButtons();
        SyncPlayState();
        if (MainWindow.Instance is { } main)
        {
            main.WindowModeChanged -= SyncWindowMode;
            main.WindowModeChanged += SyncWindowMode;
        }
        SyncWindowMode();
        SyncMaterial(MainWindow.Instance is not { } window ||
            window.IsImmersive ||
            window.ActiveBackdropMode == AppSettings.BackdropOff);
        Playback.StepFrame();
        ShowTitle(_title);
        _positionTimer.Start();
        ShowOverlay();
        _ = TryRestoreLastFileAsync();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        Playback.StateChanged -= OnPlaybackStateChanged;
        Playback.MediaOpened -= OnMediaOpened;
        Playback.MediaEnded -= OnMediaEnded;
        Playback.MediaFailed -= OnMediaFailed;
        Playlist.QueueChanged -= OnPlaylistQueueChanged;
        Playlist.CurrentIndexChanged -= OnPlaylistCurrentIndexChanged;
        if (MainWindow.Instance is { } main)
        {
            main.WindowModeChanged -= SyncWindowMode;
        }
        _positionTimer.Stop();
        _hideTimer.Stop();
        _previewShowTimer.Stop();
        _osdTimer.Stop();
        _toastTimer.Stop();
        _scaleFlushTimer.Stop();
        _previewCts?.Cancel();
        PlayerElement.SetMediaPlayer(null);
    }

    public async Task LoadFilesAsync(IReadOnlyList<StorageFile> files, bool autoplay = true, int startIndex = 0)
    {
        if (files.Count == 0)
        {
            return;
        }
        _suppressRestore = true;
        Playlist.SetQueue(files, startIndex);
        await PlayCurrentAsync(autoplay);
    }

    private async Task PlayCurrentAsync(bool autoplay = true)
    {
        var gen = ++_loadGen;
        var file = Playlist.Current;
        if (file is null)
        {
            return;
        }
        _lastSavedResume = 0;
        ShowVideoTitle(file.Path);
        _previewCts?.Cancel();
        _previewCts?.Dispose();
        _previewCts = new CancellationTokenSource();
        _exactCts?.Cancel();
        _exactCts?.Dispose();
        _exactCts = new CancellationTokenSource();
        _previewFrames = PreviewService.GetFramePaths(file.Path);
        _previewBitmaps = null;
        _previewFrameIndex = -1;
        _exactCache.Clear();
        try
        {
            var item = await Playback.CreatePlaybackItemAsync(file);
            if (gen != _loadGen)
            {
                return;
            }
            if (item is null)
            {
                throw new InvalidOperationException(LocalizationService.L("无法创建播放项"));
            }
            Playback.Load(item);
            SwitchCover.Visibility = Visibility.Visible;
            SyncVideoVisibility();
            ResetEmbeddedTracks();
            PrepareResumePosition(file, autoplay);
            if (autoplay)
            {
                Playback.Play();
            }
            else
            {
                Playback.Pause();
            }
            SyncPlayState();
            _cues = LoadCuesFor(file.Path);
            _renderedCue = null;
            SubtitleText.Text = "";
            BuildSubtitleMenu();
            SettingsService.Current.LastFile = file.Path;
            SettingsService.Save();
            RefreshTransportButtons();
            ShowOverlay();
        }
        catch (Exception ex)
        {
            SwitchCover.Visibility = Visibility.Collapsed;
            ShowTitle(LocalizationService.L("无法打开：{0}", ex.Message));
            ShowOverlay();
        }
    }

    private async Task TryRestoreLastFileAsync()
    {
        if (_suppressRestore || Playlist.Count > 0)
        {
            return;
        }
        var last = SettingsService.Current.LastFile;
        if (string.IsNullOrEmpty(last) || !File.Exists(last))
        {
            return;
        }
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(last);
            if (_suppressRestore || Playlist.Count > 0)
            {
                return;
            }
            await LoadFilesAsync([file], autoplay: false);
        }
        catch
        {
        }
    }

    private void OnPlaylistCurrentIndexChanged(int index)
    {
        DispatcherQueue.TryEnqueue(() => _ = PlayCurrentAsync());
    }

    private void OnPlaylistQueueChanged()
    {
        DispatcherQueue.TryEnqueue(RefreshTransportButtons);
    }

    private void OnPlaybackStateChanged(MediaPlaybackState state)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (state is MediaPlaybackState.Playing or MediaPlaybackState.Paused or MediaPlaybackState.None)
            {
                SetPlayIcon(state == MediaPlaybackState.Playing);
            }
            UpdatePositionUI();
            ShowOverlay();
        });
    }

    private void OnMediaOpened()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            SwitchCover.Visibility = Visibility.Collapsed;
            ApplyPendingResume();
            var file = Playlist.Current;
            if (file is not null)
            {
                LibraryService.Instance.RecordPlayed(file.Path, Playback.Duration.TotalSeconds);
                _ = PreviewService.EnsureFramesAsync(
                    file.Path, Playback.Duration.TotalSeconds, _previewCts?.Token ?? CancellationToken.None);
            }
        });
    }

    private void OnMediaEnded()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var file = Playlist.Current;
            if (file is not null && SettingsService.Current.ResumePositions.Remove(file.Path))
            {
                SettingsService.Save();
            }
            if (Playlist.Repeat == PlaylistRepeatMode.One)
            {
                Playback.Seek(TimeSpan.Zero);
                Playback.Play();
            }
            else if (!Playlist.MoveNext())
            {
                Playback.Seek(TimeSpan.Zero);
                Playback.Pause();
                ShowOverlay();
            }
        });
    }

    private void OnMediaFailed(string message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            SwitchCover.Visibility = Visibility.Collapsed;
            ShowTitle(LocalizationService.L("解码失败：{0}", message));
            ShowOverlay();
        });
    }

    private void SyncVideoVisibility()
    {
        var hasMedia = Playback.CurrentItem is not null;
        EmptyHint.Visibility = hasMedia ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PrepareResumePosition(StorageFile file, bool autoplay)
    {
        if (!autoplay || !SettingsService.Current.ResumeEnabled ||
            !SettingsService.Current.ResumePositions.TryGetValue(file.Path, out var seconds))
        {
            _pendingResumeSeconds = null;
            return;
        }
        _pendingResumeSeconds = seconds;
    }

    private void ApplyPendingResume()
    {
        var file = Playlist.Current;
        if (file is null || _pendingResumeSeconds is not { } seconds)
        {
            return;
        }
        _pendingResumeSeconds = null;
        var duration = Playback.Duration;
        if (duration.TotalSeconds > 0 && seconds > 5 && seconds < duration.TotalSeconds * 0.95)
        {
            Playback.Seek(TimeSpan.FromSeconds(seconds));
            UpdatePositionUI();
        }
    }

    private void SaveResumeProgress()
    {
        if (!SettingsService.Current.ResumeEnabled ||
            Playback.Session.PlaybackState != MediaPlaybackState.Playing)
        {
            return;
        }
        var file = Playlist.Current;
        if (file is null)
        {
            return;
        }
        var duration = Playback.Duration;
        var position = Playback.Position;
        if (duration.TotalSeconds <= 0 || position.TotalSeconds < 5)
        {
            return;
        }
        if (position / duration >= 0.95)
        {
            if (SettingsService.Current.ResumePositions.Remove(file.Path))
            {
                SettingsService.Save();
            }
            return;
        }
        if (Math.Abs(position.TotalSeconds - _lastSavedResume) < 5)
        {
            return;
        }
        _lastSavedResume = position.TotalSeconds;
        SettingsService.Current.ResumePositions[file.Path] = position.TotalSeconds;
        SettingsService.Save();
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Playback.Toggle();
        SyncPlayState();
    }

    private void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        Playlist.MovePrevious();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        Playlist.MoveNext();
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        Playback.IsMuted = !Playback.IsMuted;
        SettingsService.Current.Muted = Playback.IsMuted;
        SettingsService.Save();
        SyncVolumeUI();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        _ = PickAndPlayAsync();
    }

    private async Task PickAndPlayAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
            ViewMode = PickerViewMode.List
        };
        foreach (var ext in PlaybackService.VideoExtensions)
        {
            picker.FileTypeFilter.Add(ext);
        }

        if (MainWindow.Instance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));
        }

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await LoadFilesAsync(files.ToList());
        }
    }

    private void FullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ToggleWindowFullscreen();
    }

    private void BarFullscreenButton_Click(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance?.ToggleFullscreen();
    }

    public void SyncMaterial(bool materialOn)
    {
        var main = MainWindow.Instance;
        if (main is null)
        {
            return;
        }
        Root.Background = materialOn
            ? main.ThemedBrush("AppBackdropTintBrush")
            : new SolidColorBrush(Colors.Black);
        PageBackdrop.Background = materialOn
            ? new SolidColorBrush(Colors.Transparent)
            : main.ThemedBrush("AppBackgroundBrush");
        ControlBarBorder.Background = CreateBarBrush(main);
    }

    private static Brush CreateBarBrush(MainWindow main)
    {
        var color = main.ThemedBrush("LibraryCardFillBrush") is SolidColorBrush solid
            ? solid.Color
            : Colors.Black;
        return new SolidColorBrush(new Windows.UI.Color { A = 0x99, R = color.R, G = color.G, B = color.B });
    }

    private void SetupBarBlur()
    {
        try
        {
            var compositor = ElementCompositionPreview.GetElementVisual(ControlBarBorder).Compositor;
            var blur = new GaussianBlurEffect
            {
                Name = "Blur",
                BlurAmount = 24,
                BorderMode = EffectBorderMode.Hard,
                Source = new CompositionEffectSourceParameter("backdrop")
            };
            var brush = compositor.CreateEffectFactory(blur).CreateBrush();
            brush.SetSourceParameter("backdrop", compositor.CreateBackdropBrush());
            _barBlurClip = compositor.CreateRoundedRectangleGeometry();
            _barBlurClip.CornerRadius = new Vector2(10, 10);
            _barBlurVisual = compositor.CreateSpriteVisual();
            _barBlurVisual.Brush = brush;
            _barBlurVisual.Clip = compositor.CreateGeometricClip(_barBlurClip);
            ElementCompositionPreview.SetElementChildVisual(BarGlassHost, _barBlurVisual);
            ControlBarBorder.SizeChanged += (_, _) => UpdateBarGlassLayout();
            Root.SizeChanged += (_, _) => UpdateBarGlassLayout();
            UpdateBarGlassLayout();
        }
        catch
        {
        }
    }

    private void UpdateBarGlassLayout()
    {
        if (_barBlurVisual is null || _barBlurClip is null)
        {
            return;
        }
        var origin = ControlBarBorder.TransformToVisual(BarGlassHost).TransformPoint(new Windows.Foundation.Point(0, 0));
        var size = new Vector2((float)ControlBarBorder.ActualWidth, (float)ControlBarBorder.ActualHeight);
        _barBlurVisual.Offset = new Vector3((float)origin.X, (float)origin.Y, 0);
        _barBlurVisual.Size = size;
        _barBlurClip.Size = size;
    }

    private void SyncWindowMode()
    {
        var main = MainWindow.Instance;
        var fullscreen = main?.IsFullscreen == true;
        var windowFullscreen = main?.IsWindowFullscreen == true;
        var immersive = fullscreen || windowFullscreen;
        PageBackdrop.Visibility = immersive ? Visibility.Collapsed : Visibility.Visible;
        FullscreenIcon.Glyph = windowFullscreen ? "\uE73F" : "\uE9A6";
        ToolTipService.SetToolTip(FullscreenButton, windowFullscreen ? L("退出窗口全屏") : L("窗口全屏"));
        BarFullscreenIcon.Glyph = fullscreen ? "\uE73F" : "\uE740";
        ToolTipService.SetToolTip(BarFullscreenButton, fullscreen ? L("退出全屏") : L("全屏"));
        Playback.StepFrame();
        _scaleFlushTimer.Stop();
        _scaleFlushTimer.Start();
    }

    private void FlushPresentation()
    {
        if (Playback.CurrentItem is null)
        {
            return;
        }
        if (Playback.Session.PlaybackState == MediaPlaybackState.Playing)
        {
            Playback.Seek(Playback.Position);
        }
        else
        {
            Playback.StepFrame();
        }
    }

    private void Root_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _doubleTapTick = Environment.TickCount64;
        if (_tapTogglePending && _doubleTapTick - _tapToggleTick < 600)
        {
            _tapTogglePending = false;
            if (Playback.IsPlaying != _playingBeforeTap)
            {
                Playback.Toggle();
                SyncPlayState();
            }
        }
        if (e.OriginalSource is DependencyObject source &&
            (IsDescendantOf(source, OverlayHost) || IsDescendantOf(source, PreviewPopup)))
        {
            return;
        }
        MainWindow.Instance?.ToggleFullscreen();
    }

    private void Root_Tapped(object sender, TappedRoutedEventArgs e)
    {
        _tapTogglePending = false;
        if (Environment.TickCount64 - _doubleTapTick < 400)
        {
            return;
        }
        if (!SettingsService.Current.ClickVideoToToggle || Playback.CurrentItem is null)
        {
            return;
        }
        if (e.OriginalSource is DependencyObject source &&
            (IsDescendantOf(source, OverlayHost) || IsDescendantOf(source, PreviewPopup) || IsDescendantOf(source, TitleHost)))
        {
            return;
        }
        _playingBeforeTap = Playback.IsPlaying;
        Playback.Toggle();
        SyncPlayState();
        _tapTogglePending = true;
        _tapToggleTick = Environment.TickCount64;
    }

    public void SyncPlayState()
    {
        SetPlayIcon(Playback.IsPlaying);
    }

    private void SetPlayIcon(bool playing)
    {
        PlayIcon.Glyph = playing ? "\uE769" : "\uE768";
    }

    private static bool IsDescendantOf(DependencyObject node, DependencyObject ancestor)
    {
        while (node is not null)
        {
            if (node == ancestor)
            {
                return true;
            }
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private void RepeatButton_Click(object sender, RoutedEventArgs e)
    {
        Playlist.Repeat = Playlist.Repeat switch
        {
            PlaylistRepeatMode.Off => PlaylistRepeatMode.All,
            PlaylistRepeatMode.All => PlaylistRepeatMode.One,
            _ => PlaylistRepeatMode.Off
        };
        SettingsService.Current.Repeat = (int)Playlist.Repeat;
        SettingsService.Save();
        SyncModeButtons();
    }

    private void ShuffleButton_Click(object sender, RoutedEventArgs e)
    {
        Playlist.IsShuffle = !Playlist.IsShuffle;
        SettingsService.Current.Shuffle = Playlist.IsShuffle;
        SettingsService.Save();
        SyncModeButtons();
    }

    private void SyncModeButtons()
    {
        RepeatIcon.Glyph = Playlist.Repeat == PlaylistRepeatMode.One ? "\uE8ED" : "\uE8EE";
        RepeatButton.Opacity = Playlist.Repeat == PlaylistRepeatMode.Off ? 0.5 : 1.0;
        ToolTipService.SetToolTip(RepeatButton, Playlist.Repeat switch
        {
            PlaylistRepeatMode.All => L("循环全部"),
            PlaylistRepeatMode.One => L("单曲循环"),
            _ => L("循环关闭")
        });
        ShuffleButton.Opacity = Playlist.IsShuffle ? 1.0 : 0.5;
    }

    private async void LoadSubtitleItem_Click(object sender, RoutedEventArgs e)
    {
        if (_embeddedTrackIndex >= 0)
        {
            SetEmbeddedTrack(-1);
        }
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        foreach (var ext in SubtitleService.SubtitleExtensions)
        {
            picker.FileTypeFilter.Add(ext);
        }

        if (MainWindow.Instance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));
        }

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }
        LoadSubtitlePath(file.Path);
    }

    private static List<SubtitleCue> LoadCuesFor(string videoPath)
    {
        if (!SettingsService.Current.ExternalSubtitles.TryGetValue(videoPath, out var saved))
        {
            return SubtitleService.TryLoadMatched(videoPath) ?? [];
        }
        if (saved.Length == 0)
        {
            return [];
        }
        if (!File.Exists(saved))
        {
            return SubtitleService.TryLoadMatched(videoPath) ?? [];
        }
        try
        {
            return SubtitleService.LoadFromFile(saved);
        }
        catch
        {
            return [];
        }
    }

    public void LoadSubtitlePath(string path)
    {
        try
        {
            _cues = SubtitleService.LoadFromFile(path);
            _renderedCue = null;
            SubtitleText.Text = "";
            if (Playlist.Current is { } current)
            {
                SettingsService.Current.ExternalSubtitles[current.Path] = path;
                SettingsService.Save();
            }
        }
        catch (Exception ex)
        {
            ShowTitle(LocalizationService.L("字幕加载失败：{0}", ex.Message));
        }
    }

    private void CloseSubtitleItem_Click(object sender, RoutedEventArgs e)
    {
        if (_embeddedTrackIndex >= 0)
        {
            SetEmbeddedTrack(-1);
            return;
        }
        _cues = [];
        SubtitleText.Text = "";
        if (Playlist.Current is { } current)
        {
            SettingsService.Current.ExternalSubtitles[current.Path] = "";
            SettingsService.Save();
        }
    }

    private void ResetEmbeddedTracks()
    {
        _embeddedTrackIndex = -1;
        var item = Playback.CurrentItem;
        if (item is null)
        {
            return;
        }
        var tracks = item.TimedMetadataTracks;
        for (var i = 0; i < tracks.Count; i++)
        {
            try
            {
                tracks.SetPresentationMode((uint)i, TimedMetadataTrackPresentationMode.Disabled);
            }
            catch
            {
            }
        }
    }

    private void BuildSubtitleMenu()
    {
        var menu = new MenuFlyout();
        var load = new MenuFlyoutItem { Text = L("加载字幕文件...") };
        load.Click += LoadSubtitleItem_Click;
        menu.Items.Add(load);

        var item = Playback.CurrentItem;
        if (item is not null && item.TimedMetadataTracks.Count > 0)
        {
            var tracks = item.TimedMetadataTracks;
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                var label = !string.IsNullOrWhiteSpace(track.Label)
                    ? track.Label
                    : !string.IsNullOrWhiteSpace(track.Language)
                        ? track.Language
                        : LocalizationService.L("内封轨道 {0}", i + 1);
                var entry = new ToggleMenuFlyoutItem { Text = label, IsChecked = i == _embeddedTrackIndex };
                var index = i;
                entry.Click += (_, _) => SetEmbeddedTrack(index == _embeddedTrackIndex ? -1 : index);
                menu.Items.Add(entry);
            }
        }

        var close = new MenuFlyoutItem { Text = L("关闭字幕") };
        close.Click += CloseSubtitleItem_Click;
        menu.Items.Add(close);
        SubtitleButton.Flyout = menu;
    }

    private void SetEmbeddedTrack(int index)
    {
        var item = Playback.CurrentItem;
        if (item is null)
        {
            return;
        }
        var tracks = item.TimedMetadataTracks;
        if (_embeddedTrackIndex >= 0 && _embeddedTrackIndex < tracks.Count)
        {
            var old = tracks[_embeddedTrackIndex];
            old.CueEntered -= EmbeddedCueEntered;
            old.CueExited -= EmbeddedCueExited;
            try
            {
                tracks.SetPresentationMode((uint)_embeddedTrackIndex, TimedMetadataTrackPresentationMode.Disabled);
            }
            catch
            {
            }
        }
        _embeddedTrackIndex = -1;
        _cues = [];
        SubtitleText.Text = "";

        if (index >= 0 && index < tracks.Count)
        {
            try
            {
                tracks.SetPresentationMode((uint)index, TimedMetadataTrackPresentationMode.ApplicationPresented);
            }
            catch
            {
            }
            var track = tracks[index];
            track.CueEntered += EmbeddedCueEntered;
            track.CueExited += EmbeddedCueExited;
            _embeddedTrackIndex = index;
        }
        BuildSubtitleMenu();
    }

    private void EmbeddedCueEntered(TimedMetadataTrack sender, MediaCueEventArgs args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_embeddedTrackIndex < 0)
            {
                return;
            }
            if (args.Cue is TimedTextCue cue)
            {
                SubtitleText.Text = string.Join(Environment.NewLine, cue.Lines.Select(l => l.Text));
            }
        });
    }

    private void EmbeddedCueExited(TimedMetadataTrack sender, MediaCueEventArgs args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_embeddedTrackIndex >= 0)
            {
                SubtitleText.Text = "";
            }
        });
    }

    private void RateItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string tag && double.TryParse(tag, out var rate))
        {
            Playback.Rate = rate;
            SettingsService.Current.Rate = rate;
            SettingsService.Save();
            SyncRateUI();
        }
    }

    public void SyncVolumeUI()
    {
        VolumeSlider.Value = Playback.Volume * 100;
        VolumeIcon.Glyph = Playback.IsMuted || Playback.Volume <= 0 ? "\uE74F" : "\uE767";
    }

    private void SyncRateUI()
    {
        RateText.Text = $"{Playback.Rate:0.0#}\u00D7";
    }

    public void ShowRateOsd(double rate)
    {
        RateOsdText.Text = $"{rate:0.0#}\u00D7";
        RateOsd.Visibility = Visibility.Visible;
        _osdTimer.Stop();
    }

    public void HideRateOsd()
    {
        _osdTimer.Stop();
        RateOsd.Visibility = Visibility.Collapsed;
    }

    public void ShowVolumeOsd(int percent, bool muted)
    {
        if (VolumeOsd is null)
        {
            return;
        }
        VolumeOsdText.Text = $"{percent}%";
        VolumeOsdIcon.Glyph = muted || percent <= 0 ? "\uE74F" : "\uE767";
        VolumeOsd.Visibility = Visibility.Visible;
        _volumeOsdTimer.Stop();
        _volumeOsdTimer.Start();
    }

    private void RefreshTransportButtons()
    {
        PrevButton.IsEnabled = Playlist.CurrentIndex > 0;
        NextButton.IsEnabled = Playlist.CurrentIndex < Playlist.Count - 1;
        EpisodeButton.Visibility = Playlist.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        if (EpisodeFlyout.IsOpen)
        {
            BuildEpisodePanel();
        }
    }

    private void EpisodeFlyout_Opened(object? sender, object e) => BuildEpisodePanel();

    private void BuildEpisodePanel()
    {
        EpisodeList.Children.Clear();
        var items = Playlist.Items;
        EpisodeHeader.Text = LocalizationService.L("播放列表 · {0} 集", items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            EpisodeList.Children.Add(BuildEpisodeRow(items[i], i));
        }
    }

    private Button BuildEpisodeRow(StorageFile file, int index)
    {
        var current = index == Playlist.CurrentIndex;
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new FontIcon
        {
            Glyph = "\uE768",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = current ? Visibility.Visible : Visibility.Collapsed
        };
        var label = new TextBlock
        {
            Text = LibraryService.Instance.DisplayNameOf(file.Path),
            FontSize = 13,
            FontFamily = (FontFamily)Application.Current.Resources[current ? "AppUiFontBold" : "AppUiFont"],
            FontWeight = current ? FontWeights.Bold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        Grid.SetColumn(label, 1);
        row.Children.Add(icon);
        row.Children.Add(label);

        var button = new Button
        {
            Content = row,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(8, 6, 8, 6),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Colors.Transparent)
        };
        button.Click += (_, _) =>
        {
            EpisodeFlyout.Hide();
            Playlist.JumpTo(index);
        };
        return button;
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (VolumeSlider is null)
        {
            return;
        }
        Playback.Volume = VolumeSlider.Value / 100.0;
        SettingsService.Current.Volume = Playback.Volume;
        SettingsService.Save();
        VolumeIcon.Glyph = VolumeSlider.Value <= 0 ? "\uE74F" : "\uE767";
    }

    private void VolumeSlider_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        Root.Focus(FocusState.Programmatic);
    }

    private void UpdatePositionUI()
    {
        var duration = Playback.Duration;
        var position = Playback.Position;
        if (duration.TotalSeconds > 0)
        {
            ProgressSlider.Maximum = duration.TotalSeconds;
            if (!_isSeeking)
            {
                ProgressSlider.Value = position.TotalSeconds;
            }
        }
        CurrentTimeText.Text = FormatTime(position);
        DurationText.Text = FormatTime(duration);
    }

    private void UpdateSubtitle()
    {
        var position = Playback.Position;
        SubtitleCue? current = null;
        foreach (var cue in _cues)
        {
            if (cue.Start > position)
            {
                break;
            }
            if (cue.End > position)
            {
                current = cue;
                break;
            }
        }
        if (current?.Image is { } image)
        {
            if (!ReferenceEquals(current, _renderedCue))
            {
                _ = RenderPgsAsync(image, current);
            }
            SubtitleImage.Visibility = Visibility.Visible;
            SubtitleText.Text = "";
            return;
        }
        _renderedCue = null;
        SubtitleImage.Visibility = Visibility.Collapsed;
        var text = current?.Text ?? "";
        if (SubtitleText.Text != text)
        {
            SubtitleText.Text = text;
        }
    }

    private async Task RenderPgsAsync(PgsImage image, SubtitleCue cue)
    {
        var vw = (int)Playback.Session.NaturalVideoWidth;
        var vh = (int)Playback.Session.NaturalVideoHeight;
        if (vw <= 0 || vh <= 0)
        {
            vw = image.VideoWidth;
            vh = image.VideoHeight;
        }
        var rw = Root.ActualWidth;
        var rh = Root.ActualHeight;
        if (rw <= 0 || rh <= 0)
        {
            return;
        }
        var scale = Math.Min(rw / vw, rh / vh);
        var dpi = XamlRoot?.RasterizationScale ?? 1.0;
        if (dpi <= 0)
        {
            dpi = 1;
        }
        var targetWidth = Math.Max(1, (int)Math.Round(image.Width * scale * dpi));
        var targetHeight = Math.Max(1, (int)Math.Round(image.Height * scale * dpi));
        try
        {
            var buffer = new byte[targetWidth * targetHeight * 4];
            for (var y = 0; y < targetHeight; y++)
            {
                var sy = (int)((long)y * image.Height / targetHeight);
                var srcRow = sy * image.Width;
                for (var x = 0; x < targetWidth; x++)
                {
                    var sx = (int)((long)x * image.Width / targetWidth);
                    var color = image.Palette[image.Indices[srcRow + sx]];
                    if (color is null || color[3] == 0)
                    {
                        continue;
                    }
                    var a = color[3];
                    var dst = (y * targetWidth + x) * 4;
                    buffer[dst] = (byte)(color[0] * a / 255);
                    buffer[dst + 1] = (byte)(color[1] * a / 255);
                    buffer[dst + 2] = (byte)(color[2] * a / 255);
                    buffer[dst + 3] = a;
                }
            }
            using var writer = new DataWriter();
            writer.WriteBytes(buffer);
            var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
                writer.DetachBuffer(),
                BitmapPixelFormat.Bgra8,
                targetWidth,
                targetHeight,
                BitmapAlphaMode.Premultiplied);
            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);
            _renderedCue = cue;
            SubtitleImage.Width = targetWidth / dpi;
            SubtitleImage.Height = targetHeight / dpi;
            SubtitleImage.Margin = new Thickness(
                (rw - vw * scale) / 2 + image.OffsetX * scale,
                (rh - vh * scale) / 2 + image.OffsetY * scale,
                0, 0);
            SubtitleImage.Source = source;
        }
        catch
        {
            SubtitleImage.Visibility = Visibility.Collapsed;
        }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T found)
            {
                return found;
            }
            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }
        return null;
    }

    private static string FormatTime(TimeSpan t)
    {
        t = t < TimeSpan.Zero ? TimeSpan.Zero : t;
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes}:{t.Seconds:00}";
    }

    private void ProgressSlider_PointerEntered(object sender, PointerRoutedEventArgs e) => QueuePreview(e);

    private void ProgressSlider_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        HoldSeekAnchor(e);
        QueuePreview(e);
    }

    private void HoldSeekAnchor(PointerRoutedEventArgs e)
    {
        if (!_isSeeking || _seekDragUnlocked)
        {
            return;
        }
        var x = e.GetCurrentPoint(ProgressSlider).Position.X;
        if (Math.Abs(x - _seekPressX) >= SeekDragThreshold)
        {
            _seekDragUnlocked = true;
            return;
        }
        if (ProgressSlider.Value != _seekAnchorValue)
        {
            ProgressSlider.Value = _seekAnchorValue;
        }
    }

    private void ProgressSlider_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _pointerOnSlider = false;
        _previewShowTimer.Stop();
        if (!_isSeeking)
        {
            HidePreview();
        }
    }

    private void QueuePreview(PointerRoutedEventArgs e)
    {
        _pointerOnSlider = true;
        CapturePreviewPoint(e);
        if (_isSeeking || PreviewPopup.Visibility == Visibility.Visible)
        {
            ShowPreview();
            return;
        }
        _previewShowTimer.Stop();
        _previewShowTimer.Start();
    }

    private void CapturePreviewPoint(PointerRoutedEventArgs e)
    {
        var sliderPoint = e.GetCurrentPoint(ProgressSlider).Position;
        var x = _isSeeking && !_seekDragUnlocked ? _seekPressX : sliderPoint.X;
        var thumb = FindDescendant<Thumb>(ProgressSlider);
        var inset = thumb?.ActualWidth / 2 ?? 12;
        _previewRatio = ProgressSlider.ActualWidth > inset * 2
            ? Math.Clamp((x - inset) / (ProgressSlider.ActualWidth - inset * 2), 0, 1)
            : Math.Clamp(x / ProgressSlider.ActualWidth, 0, 1);
        _previewPageX = e.GetCurrentPoint(this).Position.X;
    }

    private void ShowPreview()
    {
        var duration = Playback.Duration;
        if (duration.TotalSeconds <= 0)
        {
            return;
        }

        PreviewTime.Text = FormatTime(TimeSpan.FromSeconds(Math.Round(duration.TotalSeconds * _previewRatio)));

        if (_previewFrames is { Length: > 0 } frames)
        {
            var index = Math.Clamp((int)Math.Round(_previewRatio * frames.Length - 0.5), 0, frames.Length - 1);
            ShowPreviewFrame(frames, index);
        }

        _previewExactTimer.Stop();
        _previewExactTimer.Start();

        var hostWidth = OverlayHost.ActualWidth;
        var barRight = 14 + (hostWidth > 0 ? hostWidth : 600);
        var left = Math.Clamp(_previewPageX - 86, 14, Math.Max(14, barRight - 174));
        PreviewPopup.Margin = new Thickness(left, 0, 0, 14 + OverlayHost.ActualHeight + 6);
        PreviewPopup.Visibility = Visibility.Visible;
    }

    private void ShowPreviewFrame(string[] frames, int index)
    {
        _previewFrameIndex = index;
        if (_previewBitmaps is null || _previewBitmaps.Length != frames.Length)
        {
            _previewBitmaps = new BitmapImage?[frames.Length];
        }

        var actual = NearestFrame(frames, index);
        if (actual < 0)
        {
            PreviewImage.Source = null;
            PreviewImage.Visibility = Visibility.Collapsed;
            return;
        }
        if (_previewBitmaps[actual] is { } cached)
        {
            PreviewImage.Source = cached;
            PreviewImage.Visibility = Visibility.Visible;
            return;
        }

        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _ = LoadPreviewFrameAsync(frames, actual);
    }

    private static int NearestFrame(string[] frames, int index)
    {
        for (var offset = 0; offset < frames.Length; offset++)
        {
            var left = index - offset;
            if (left >= 0 && File.Exists(frames[left]))
            {
                return left;
            }
            var right = index + offset;
            if (right < frames.Length && right != left && File.Exists(frames[right]))
            {
                return right;
            }
        }
        return -1;
    }

    private async Task LoadPreviewFrameAsync(string[] frames, int index)
    {
        try
        {
            using var stream = File.OpenRead(frames[index]);
            var bitmap = new BitmapImage { DecodePixelWidth = PreviewService.FrameWidth };
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            if (_previewBitmaps is null || _previewBitmaps.Length != frames.Length)
            {
                return;
            }
            _previewBitmaps[index] = bitmap;
            if (PreviewPopup.Visibility == Visibility.Visible &&
                _previewFrameIndex >= 0 &&
                NearestFrame(frames, _previewFrameIndex) == index)
            {
                var seconds = Playback.Duration.TotalSeconds * _previewRatio;
                if (_exactCache.ContainsKey((int)Math.Round(seconds * 2)))
                {
                    return;
                }
                PreviewImage.Source = bitmap;
                PreviewImage.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            PreviewService.Log($"load frame {index} failed: {ex.GetType().Name} {ex.Message}");
        }
    }

    private void HidePreview()
    {
        _previewShowTimer.Stop();
        _previewExactTimer.Stop();
        _exactCts?.Cancel();
        PreviewPopup.Visibility = Visibility.Collapsed;
    }

    private async Task LoadExactPreviewAsync()
    {
        var duration = Playback.Duration;
        var seconds = duration.TotalSeconds * _previewRatio;
        if (seconds <= 0 || duration.TotalSeconds <= 0)
        {
            return;
        }
        if (seconds >= duration.TotalSeconds)
        {
            seconds = duration.TotalSeconds - 0.1;
            if (seconds <= 0)
            {
                return;
            }
        }

        var key = (int)Math.Round(seconds * 2);
        if (_exactCache.TryGetValue(key, out var cached))
        {
            ApplyExactPreview(cached);
            return;
        }

        _exactCts?.Cancel();
        _exactCts?.Dispose();
        _exactCts = new CancellationTokenSource();
        var token = _exactCts.Token;
        var file = Playlist.Current;
        if (file is null)
        {
            return;
        }

        var locked = false;
        try
        {
            await _exactGate.WaitAsync(token);
            locked = true;
            if (_exactCache.TryGetValue(key, out cached))
            {
                token.ThrowIfCancellationRequested();
                ApplyExactPreview(cached);
                return;
            }
            if (_previewGrabber is null || _previewGrabberPath != file.Path)
            {
                _previewGrabber?.Dispose();
                _previewGrabber = await PreviewService.OpenGrabberAsync(file.Path);
                _previewGrabberPath = file.Path;
            }
            var bytes = await PreviewService.ExtractExactFrameAsync(_previewGrabber, seconds, token);
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            await image.SetSourceAsync(stream.AsRandomAccessStream());
            token.ThrowIfCancellationRequested();
            if (_exactCache.Count > 40)
            {
                _exactCache.Clear();
            }
            _exactCache[key] = image;
            ApplyExactPreview(image);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            PreviewService.Log($"exact preview failed @{seconds:0.0}: {ex.GetType().Name} {ex.Message}");
        }
        finally
        {
            if (locked)
            {
                _exactGate.Release();
            }
        }
    }

    private void ApplyExactPreview(BitmapImage image)
    {
        if (PreviewPopup.Visibility != Visibility.Visible)
        {
            return;
        }
        PreviewImage.Source = image;
        PreviewImage.Visibility = Visibility.Visible;
    }

    private void ProgressSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isSeeking = true;
        _seekDragUnlocked = false;
        _seekPressX = e.GetCurrentPoint(ProgressSlider).Position.X;
        _seekAnchorValue = ProgressSlider.Value;
        _resumeAfterSeek = Playback.Session.PlaybackState == MediaPlaybackState.Playing;
        if (_resumeAfterSeek)
        {
            Playback.Pause();
        }
    }

    private void ProgressSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        FinishSeek();
    }

    private void ProgressSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        FinishSeek();
    }

    private void FinishSeek()
    {
        if (!_isSeeking)
        {
            return;
        }
        _isSeeking = false;
        _seekDragUnlocked = false;
        Playback.Seek(TimeSpan.FromSeconds(ProgressSlider.Value));
        if (_resumeAfterSeek)
        {
            Playback.Play();
        }
        else
        {
            Playback.StepFrame();
        }
        _resumeAfterSeek = false;
        UpdatePositionUI();
        HidePreview();
        Root.Focus(FocusState.Programmatic);
    }

    private async void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        var file = Playlist.Current;
        if (file is null || Playback.Duration <= TimeSpan.Zero)
        {
            return;
        }
        ScreenshotButton.IsEnabled = false;
        try
        {
            var position = Playback.Position;
            var subtitleText = SubtitleText.Text;
            var pgs = SnapshotPgsBitmap();
            string? path = null;
            var png = await ScreenshotService.RenderFrameAsync(file.Path, position);
            if (png is not null)
            {
                if (SettingsService.Current.ScreenshotSubtitle &&
                    (pgs is not null || !string.IsNullOrWhiteSpace(subtitleText)))
                {
                    png = await ComposeSubtitleAsync(png, subtitleText, pgs) ?? png;
                }
                path = await ScreenshotService.SaveAsync(file.Path, png);
            }
            ShowToast(path is null ? L("截图失败") : LocalizationService.L("已保存截图：{0}", path));
        }
        finally
        {
            ScreenshotButton.IsEnabled = true;
        }
    }

    private sealed record PgsSnapshot(SoftwareBitmap Bitmap, double X, double Y, double Width, double Height);

    private PgsSnapshot? SnapshotPgsBitmap()
    {
        if (SubtitleImage.Visibility != Visibility.Visible || _renderedCue?.Image is not { } image)
        {
            return null;
        }
        try
        {
            var buffer = new byte[image.Width * image.Height * 4];
            for (var i = 0; i < image.Width * image.Height; i++)
            {
                var color = image.Palette[image.Indices[i]];
                if (color is null || color[3] == 0)
                {
                    continue;
                }
                var a = color[3];
                buffer[i * 4] = (byte)(color[0] * a / 255);
                buffer[i * 4 + 1] = (byte)(color[1] * a / 255);
                buffer[i * 4 + 2] = (byte)(color[2] * a / 255);
                buffer[i * 4 + 3] = a;
            }
            using var writer = new DataWriter();
            writer.WriteBytes(buffer);
            var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
                writer.DetachBuffer(),
                BitmapPixelFormat.Bgra8,
                image.Width,
                image.Height,
                BitmapAlphaMode.Premultiplied);
            return new PgsSnapshot(
                bitmap,
                image.OffsetX / (double)image.VideoWidth,
                image.OffsetY / (double)image.VideoHeight,
                image.Width / (double)image.VideoWidth,
                image.Height / (double)image.VideoHeight);
        }
        catch
        {
            return null;
        }
    }

    private async Task<byte[]?> ComposeSubtitleAsync(byte[] framePng, string subtitle, PgsSnapshot? pgs)
    {
        try
        {
            var scale = XamlRoot.RasterizationScale;
            using var frameStream = new MemoryStream(framePng).AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(frameStream);
            var frameWidth = (int)decoder.PixelWidth;
            var frameHeight = (int)decoder.PixelHeight;
            if (frameWidth <= 0 || frameHeight <= 0 || scale <= 0)
            {
                return null;
            }
            var width = frameWidth / scale;
            var height = frameHeight / scale;

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));

            var host = new Grid { Width = width, Height = height };
            host.Children.Add(new Image
            {
                Source = source,
                Width = width,
                Height = height,
                Stretch = Stretch.Fill
            });
            if (pgs is not null)
            {
                var pgsSource = new SoftwareBitmapSource();
                await pgsSource.SetBitmapAsync(pgs.Bitmap);
                host.Children.Add(new Image
                {
                    Source = pgsSource,
                    Width = pgs.Width * width,
                    Height = pgs.Height * height,
                    Margin = new Thickness(pgs.X * width, pgs.Y * height, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stretch = Stretch.Fill
                });
            }
            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                host.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = Math.Max(14, height * 0.033),
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = width * 0.92,
                    Margin = new Thickness(0, 0, 0, height * 0.12)
                });
            }

            var container = new Grid
            {
                IsHitTestVisible = false,
                Children =
                {
                    host,
                    new Border { Background = new SolidColorBrush(Colors.Black) }
                }
            };

            Root.Children.Insert(0, container);
            try
            {
                container.UpdateLayout();
                var bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(host);
                var pixels = await bitmap.GetPixelsAsync();
                var pixelBytes = new byte[pixels.Length];
                DataReader.FromBuffer(pixels).ReadBytes(pixelBytes);
                using var output = new InMemoryRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)bitmap.PixelWidth,
                    (uint)bitmap.PixelHeight,
                    96, 96,
                    pixelBytes);
                await encoder.FlushAsync();
                return await ScreenshotService.ReadStreamAsync(output);
            }
            finally
            {
                Root.Children.Remove(container);
                source.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }

    private void ShowToast(string text)
    {
        TitleText.Text = text;
        TitleHost.Visibility = Visibility.Visible;
        TitleHost.IsHitTestVisible = _overlayShown;
        FadeTo(TitleHost, _overlayShown ? 1 : 0);
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ShowTitle(string text)
    {
        _title = text;
        _toastTimer.Stop();
        TitleText.Text = text;
        TitleHost.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        TitleHost.IsHitTestVisible = _overlayShown;
        if (TitleHost.Visibility == Visibility.Visible)
        {
            FadeTo(TitleHost, _overlayShown ? 1 : 0);
        }
    }

    private void ShowVideoTitle(string path)
    {
        ShowTitle(BuildVideoTitle(path));
    }

    private static string BuildVideoTitle(string path)
    {
        var name = LibraryService.Instance.DisplayNameOf(path);
        var folder = Path.GetDirectoryName(path);
        var folderName = folder is null ? "" : LibraryService.Instance.DisplayNameOf(folder);
        return string.IsNullOrEmpty(folderName) ? name : $"{folderName}-{name}";
    }

    private static void FadeTo(FrameworkElement element, double opacity)
    {
        var animation = new DoubleAnimation
        {
            To = opacity,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void ShowOverlay()
    {
        if (!_overlayShown)
        {
            _overlayShown = true;
            TitleHost.IsHitTestVisible = true;
            FadeTo(OverlayHost, 1);
            FadeTo(BarGlassHost, 1);
            if (TitleHost.Visibility == Visibility.Visible)
            {
                FadeTo(TitleHost, 1);
            }
        }
        if (Playback.CurrentItem is not null)
        {
            RestartHideTimer();
        }
        else
        {
            _hideTimer.Stop();
        }
    }

    private void HideOverlay()
    {
        if (_overlayShown)
        {
            _overlayShown = false;
            TitleHost.IsHitTestVisible = false;
            FadeTo(OverlayHost, 0);
            FadeTo(BarGlassHost, 0);
            if (TitleHost.Visibility == Visibility.Visible)
            {
                FadeTo(TitleHost, 0);
            }
        }
        HidePreview();
    }

    private void RestartHideTimer()
    {
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void Root_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ShowOverlay();
    }

    private void Root_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        HideOverlay();
    }
}
