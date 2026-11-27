using System.Collections.ObjectModel;
using FilmField.Services;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace FilmField.Views;

public sealed class PosterOption
{
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string Source { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public ImageSource? Image { get; init; }
}

public sealed partial class LibraryPage : Page
{
    private const int PageSize = 50;

    private readonly ObservableCollection<LibraryEntry> _entries = [];
    private readonly ObservableCollection<LibraryEntry> _libraryEntries = [];
    private readonly HashSet<string> _collapsed = [];
    private readonly DispatcherQueueTimer _noticeTimer;
    private LibraryEntry? _dropTarget;
    private int _noticeToken;
    private int _sortIndex;
    private int _videoPage;
    private int _videoTotal;
    private bool _initialized;
    private bool _syncing;

    public LibraryPage()
    {
        InitializeComponent();
        LibraryGrid.ItemsSource = _entries;
        LibraryShelf.ItemsSource = _libraryEntries;
        _noticeTimer = DispatcherQueue.CreateTimer();
        _noticeTimer.Interval = TimeSpan.FromSeconds(2.4);
        _noticeTimer.IsRepeating = false;
        _noticeTimer.Tick += (_, _) => PlayNotice(false);
        SortCombo.SelectedIndex = 0;
        _initialized = true;
    }

    internal string L(string key) => LocalizationService.L(key);

    private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _syncing)
        {
            return;
        }
        _sortIndex = SortCombo.SelectedIndex;
        LibraryService.Instance.SetSort(LibraryService.Instance.ViewKey, _sortIndex);
        ApplySort();
        UpdateState();
        _ = LoadThumbnailsAsync();
    }

    private void SyncSort()
    {
        var index = Math.Clamp(
            LibraryService.Instance.SortOf(LibraryService.Instance.ViewKey),
            0,
            SortCombo.Items.Count - 1);
        if (_sortIndex == index && SortCombo.SelectedIndex == index)
        {
            return;
        }
        _sortIndex = index;
        _syncing = true;
        SortCombo.SelectedIndex = index;
        _syncing = false;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        LibraryService.Instance.Open(e.Parameter as string);
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: true);
    }

    private async Task RefreshAsync(bool rescan)
    {
        SyncSort();
        ScanningText.Visibility = rescan ? Visibility.Visible : Visibility.Collapsed;
        if (rescan)
        {
            await LibraryService.Instance.ScanAsync();
        }
        ScanningText.Visibility = Visibility.Collapsed;
        ApplySort();
        UpdateState();
        _ = LoadThumbnailsAsync();
        _ = AutoFetchPostersAsync();
    }

    private void ApplySort()
    {
        var library = LibraryService.Instance;
        var entries = library.Entries;
        var overview = library.IsOverview;

        _entries.Clear();
        _libraryEntries.Clear();
        VideoHeader.Visibility = overview ? Visibility.Visible : Visibility.Collapsed;

        if (overview)
        {
            foreach (var entry in entries
                .Where(e => e.IsLibrary)
                .OrderBy(e => e.Name, StringComparer.CurrentCulture))
            {
                _libraryEntries.Add(entry);
            }
        }
        else
        {
            foreach (var entry in entries
                .Where(e => !e.IsVideo)
                .OrderBy(e => e.Name, StringComparer.CurrentCulture))
            {
                _entries.Add(entry);
            }
        }
        if (overview)
        {
            _videoTotal = entries.Count(e => e.IsVideo);
            var pages = TotalPages;
            _videoPage = Math.Clamp(_videoPage, 0, pages - 1);
            foreach (var entry in SortedVideos(entries.Where(e => e.IsVideo))
                .Skip(_videoPage * PageSize)
                .Take(PageSize))
            {
                _entries.Add(entry);
            }
        }
        else
        {
            _videoTotal = 0;
            _videoPage = 0;
            foreach (var entry in SortedVideos(entries.Where(e => e.IsVideo)))
            {
                _entries.Add(entry);
            }
        }
        LibraryShelfSection.Visibility = _libraryEntries.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        ApplyCollapse();
        UpdatePager();
    }

    private int TotalPages => Math.Max(1, (int)Math.Ceiling(_videoTotal / (double)PageSize));

    private void UpdatePager()
    {
        var show = LibraryService.Instance.IsOverview
            && _videoTotal > PageSize
            && LibraryGrid.Visibility == Visibility.Visible;
        PagerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
        {
            return;
        }
        PagerText.Text = LocalizationService.L("第 {0} / {1} 页", _videoPage + 1, TotalPages);
        PrevPageButton.IsEnabled = _videoPage > 0;
        NextPageButton.IsEnabled = _videoPage < TotalPages - 1;
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_videoPage <= 0)
        {
            return;
        }
        _videoPage--;
        ApplySort();
        UpdateState();
        _ = LoadThumbnailsAsync();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_videoPage >= TotalPages - 1)
        {
            return;
        }
        _videoPage++;
        ApplySort();
        UpdateState();
        _ = LoadThumbnailsAsync();
    }

    private void ClearView()
    {
        _entries.Clear();
        _libraryEntries.Clear();
        _videoPage = 0;
    }

    private void ApplyCollapse()
    {
        var librariesCollapsed = _collapsed.Contains("libraries");
        LibraryShelf.Visibility = librariesCollapsed ? Visibility.Collapsed : Visibility.Visible;
        LibraryChevron.Glyph = librariesCollapsed ? "\uE76C" : "\uE70D";

        var videosCollapsed = _collapsed.Contains("videos");
        LibraryGrid.Visibility = videosCollapsed ? Visibility.Collapsed : Visibility.Visible;
        VideoChevron.Glyph = videosCollapsed ? "\uE76C" : "\uE70D";
        UpdatePager();
    }

    private IEnumerable<LibraryEntry> SortedVideos(IEnumerable<LibraryEntry> videos) => _sortIndex switch
    {
        1 => videos.OrderBy(e => e.Name, StringComparer.CurrentCulture),
        2 => videos.OrderByDescending(e => e.SizeBytes).ThenBy(e => e.Name, StringComparer.CurrentCulture),
        _ => videos.OrderByDescending(e => e.LastPlayed).ThenBy(e => e.Name, StringComparer.CurrentCulture)
    };

    private void Section_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string key)
        {
            return;
        }
        if (!_collapsed.Remove(key))
        {
            _collapsed.Add(key);
        }
        ApplyCollapse();
    }

    private void UpdateHeader()
    {
        var library = LibraryService.Instance;
        var crumbs = library.Breadcrumbs;
        BackButton.Visibility = library.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        TitleText.Text = crumbs.Count > 0 ? crumbs[^1] : library.Active.Name;
        PathText.Text = crumbs.Count > 1 ? string.Join("  ›  ", crumbs.Take(crumbs.Count - 1)) : "";
        PathText.Visibility = crumbs.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateState()
    {
        UpdateHeader();

        var library = LibraryService.Instance;
        var inCollection = library.CurrentCollection is not null;
        var inFolder = library.CurrentFolder is not null || inCollection;
        var hasSources = library.Active.Folders.Count > 0 || library.Active.Files.Count > 0;
        var libraryCount = _libraryEntries.Count;
        var folderCount = _entries.Count(e => e.IsFolder || e.IsCollection);
        var videoCount = LibraryService.Instance.IsOverview ? _videoTotal : _entries.Count(e => e.IsVideo);

        EmptyState.Visibility = _entries.Count == 0 && _libraryEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        EmptyTitle.Text = inCollection
            ? L("这个文件夹里还没有视频")
            : inFolder
                ? L("这个文件夹里没有视频")
                : hasSources
                    ? L("没有找到视频文件")
                    : L("这里还没有视频");
        EmptyDesc.Text = inCollection
            ? L("把海报拖进来就能放进文件夹")
            : inFolder
                ? L("当前文件夹内没有视频文件或子文件夹")
                : hasSources
                    ? L("确认文件夹里是否有视频，或再添加其他文件夹")
                    : L("添加文件夹或视频文件，自动生成封面网格与观看记录");
        EmptyActions.Visibility = inFolder ? Visibility.Collapsed : Visibility.Visible;

        var parts = new List<string>();
        if (libraryCount > 0)
        {
            parts.Add(LocalizationService.L("{0} 个库", libraryCount));
        }
        if (folderCount > 0)
        {
            parts.Add(LocalizationService.L("{0} 个文件夹", folderCount));
        }
        if (videoCount > 0)
        {
            parts.Add(LocalizationService.L("{0} 个视频", videoCount));
        }
        CountText.Text = string.Join(" · ", parts);
        LibraryCountText.Text = LocalizationService.L("{0} 个库", libraryCount);
        VideoCountText.Text = LocalizationService.L("{0} 个视频", videoCount);
    }

    private async Task LoadThumbnailsAsync()
    {
        using var gate = new SemaphoreSlim(4);
        var snapshot = _entries.Where(e => !e.IsLibrary).ToList();
        var dispatcher = DispatcherQueue;
        var tasks = snapshot.Select(async entry =>
        {
            if (entry.Thumbnail is not null)
            {
                return;
            }
            await gate.WaitAsync();
            try
            {
                var cachePath = await LibraryService.GetPosterPathAsync(entry);
                if (cachePath is null)
                {
                    return;
                }
                dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        entry.Thumbnail = new BitmapImage
                        {
                            DecodePixelWidth = 320,
                            UriSource = new Uri(cachePath)
                        };
                    }
                    catch
                    {
                    }
                });
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        if (MainWindow.Instance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));
        }
        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
        {
            return;
        }
        await LibraryService.Instance.AddFolderAsync(folder.Path);
        await RefreshAsync(rescan: false);
    }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary
        };
        foreach (var extension in PlaybackService.VideoExtensions)
        {
            picker.FileTypeFilter.Add(extension);
        }
        if (MainWindow.Instance is not null)
        {
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(MainWindow.Instance));
        }
        var files = await picker.PickMultipleFilesAsync();
        if (files is null || files.Count == 0)
        {
            return;
        }
        await LibraryService.Instance.AddFilesAsync(files.Select(f => f.Path));
        await RefreshAsync(rescan: false);
    }

    private async void ManageSources_Click(object sender, RoutedEventArgs e)
    {
        var library = LibraryService.Instance.Active;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = LocalizationService.L("{0}的来源", library.Name),
            CloseButtonText = L("关闭"),
            DefaultButton = ContentDialogButton.Close
        };

        async Task Remove(Func<Task> action)
        {
            dialog.Hide();
            await action();
            await RefreshAsync(rescan: false);
        }

        var panel = new StackPanel { Spacing = 2 };
        if (library.Folders.Count == 0 && library.Files.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = L("还没有添加任何来源"), Opacity = 0.6 });
        }
        if (library.Folders.Count > 0)
        {
            panel.Children.Add(SectionHeader(L("section.文件夹")));
            foreach (var folder in library.Folders.ToList())
            {
                panel.Children.Add(SourceRow(folder, folder, () => Remove(() => LibraryService.Instance.RemoveFolderAsync(folder))));
            }
        }
        if (library.Files.Count > 0)
        {
            panel.Children.Add(SectionHeader(L("文件")));
            foreach (var file in library.Files.ToList())
            {
                panel.Children.Add(SourceRow(Path.GetFileName(file), file, () => Remove(() => LibraryService.Instance.RemoveFileAsync(file))));
            }
        }

        dialog.Content = new ScrollViewer
        {
            Content = panel,
            MaxHeight = 380,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        await dialog.ShowAsync();
        Focus(FocusState.Pointer);
    }

    private static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontSize = 12,
        FontFamily = (FontFamily)Application.Current.Resources["AppUiFontBold"],
        FontWeight = FontWeights.Bold,
        Opacity = 0.6,
        Margin = new Thickness(0, 10, 0, 2)
    };

    private static Grid SourceRow(string text, string tooltip, Func<Task> remove)
    {
        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Opacity = 0.8,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        ToolTipService.SetToolTip(label, tooltip);

        var button = new Button { Content = LocalizationService.L("移除"), Padding = new Thickness(10, 4, 10, 4) };
        button.Click += async (_, _) => await remove();

        Grid.SetColumn(label, 0);
        Grid.SetColumn(button, 1);
        row.Children.Add(label);
        row.Children.Add(button);
        return row;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync(rescan: true);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (!LibraryService.Instance.GoUp())
        {
            return;
        }
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: true);
    }

    private void LibraryGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not LibraryEntry entry)
        {
            return;
        }
        if (entry.IsLibrary)
        {
            _ = OpenLibraryAsync(entry.Id);
            return;
        }
        if (entry.IsCollection)
        {
            if (LibraryService.Instance.OpenCollection(entry.Id))
            {
                ClearView();
                UpdateState();
                _ = RefreshAsync(rescan: true);
            }
            return;
        }
        if (entry.IsFolder)
        {
            if (LibraryService.Instance.OpenFolder(entry.Path))
            {
                ClearView();
                UpdateState();
                _ = RefreshAsync(rescan: true);
            }
            return;
        }
        if (MainWindow.Instance is not null)
        {
            _ = MainWindow.Instance.PlayFromLibraryAsync(entry.Path);
        }
    }

    private void LibraryShelf_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is LibraryEntry { IsLibrary: true } entry)
        {
            _ = OpenLibraryAsync(entry.Id);
        }
    }

    private async Task OpenLibraryAsync(string id)
    {
        if (MainWindow.Instance is { } window && window.SelectLibrary(id))
        {
            return;
        }
        LibraryService.Instance.Enter(id);
        ClearView();
        UpdateState();
        await RefreshAsync(rescan: true);
    }

    private void LibraryGrid_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var entries = e.Items.OfType<LibraryEntry>().Where(x => x.IsVideo || x.IsFolder).ToList();
        PosterDrag.Entries = entries;
        if (entries.Count == 0)
        {
            e.Cancel = true;
            return;
        }
        e.Data.SetText("filmfield/poster");
        e.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void LibraryGrid_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        PosterDrag.Entries = [];
        SetDropTarget(null);
    }

    private void LibraryGrid_DragOver(object sender, DragEventArgs e)
    {
        var dragging = PosterDrag.Entries;
        var entry = TargetEntry(e);
        var library = LibraryService.Instance;
        var inCollection = library.CurrentCollection is not null;
        var inFolder = library.CurrentFolder is not null;
        var videos = dragging.Count > 0 && dragging.All(x => x.IsVideo);
        var accepts = videos &&
            entry is not null &&
            !dragging.Contains(entry) &&
            (entry.IsCollection || entry.IsFolder || (!inCollection && entry.IsVideo)) &&
            !(entry.IsFolder && AlreadyInside(entry, dragging));
        if (accepts)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = entry!.IsVideo ? L("组成文件夹") : LocalizationService.L("加入「{0}」", entry.Name);
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
            SetDropTarget(entry);
            return;
        }
        SetDropTarget(null);
        if (videos && entry is null && (inCollection || inFolder))
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = L("移出文件夹");
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = false;
            return;
        }
        e.AcceptedOperation = DataPackageOperation.None;
    }

    private async void LibraryGrid_Drop(object sender, DragEventArgs e)
    {
        var dragging = PosterDrag.Entries;
        var entry = TargetEntry(e);
        SetDropTarget(null);
        if (dragging.Count == 0)
        {
            return;
        }

        var library = LibraryService.Instance;
        var paths = dragging.Select(x => x.Path).ToList();
        var videos = dragging.All(x => x.IsVideo);
        var inCollection = library.CurrentCollection is not null;
        if (!videos)
        {
            return;
        }
        if (entry is { IsCollection: true })
        {
            await library.AddToCollectionAsync(entry.Id, paths);
            Notice(LocalizationService.L("已加入「{0}」", entry.Name));
            await ReloadAsync();
            return;
        }
        if (entry is { IsFolder: true })
        {
            await library.AddToFolderAsync(entry.Path, paths);
            Notice(LocalizationService.L("已加入「{0}」", entry.Name));
            await ReloadAsync();
            return;
        }
        if (entry is { IsVideo: true } && !inCollection && !dragging.Contains(entry))
        {
            paths.Add(entry.Path);
            await library.CreateCollectionAsync(paths);
            Notice(L("已组成文件夹"));
            await ReloadAsync();
            return;
        }
        if (entry is not null)
        {
            return;
        }
        if (inCollection)
        {
            await library.RemoveFromCollectionAsync(paths);
            Notice(L("已移出文件夹"));
            await ReloadAsync();
            return;
        }
        if (library.CurrentFolder is { } folder && await library.RemoveFromFolderAsync(folder, paths) > 0)
        {
            Notice(L("已移出文件夹"));
            await ReloadAsync();
        }
    }

    private static bool AlreadyInside(LibraryEntry folder, IReadOnlyList<LibraryEntry> dragging) =>
        dragging.All(x => string.Equals(
            Path.GetDirectoryName(x.Path),
            folder.Path,
            StringComparison.OrdinalIgnoreCase));

    private static LibraryEntry? TargetEntry(DragEventArgs e) =>
        FindAncestor<GridViewItem>(e.OriginalSource as DependencyObject)?.Content as LibraryEntry;

    private void SetDropTarget(LibraryEntry? entry)
    {
        if (ReferenceEquals(_dropTarget, entry))
        {
            return;
        }
        if (_dropTarget is not null)
        {
            _dropTarget.IsDropTarget = false;
        }
        _dropTarget = entry;
        if (entry is not null)
        {
            entry.IsDropTarget = true;
        }
    }

    private async Task ReloadAsync()
    {
        ClearView();
        UpdateState();
        await RefreshAsync(rescan: false);
    }

    public void RefreshAfterChange(string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Notice(message);
        }
        _ = RefreshAsync(rescan: true);
    }

    private void LibraryGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        var container = FindAncestor<GridViewItem>(args.OriginalSource as DependencyObject);
        if (container?.Content is not LibraryEntry entry)
        {
            ShowBlankMenu(args);
            return;
        }
        args.Handled = true;

        var flyout = new MenuFlyout();
        flyout.Items.Add(MenuAction(L("详细"), "\uE946", () => _ = ShowDetailsAsync(entry)));

        if (entry.IsLibrary)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(MenuAction(L("打开"), "\uE8A7", () => _ = OpenLibraryAsync(entry.Id)));
            flyout.Items.Add(MenuAction(L("重命名"), "\uE8AC", () => _ = RenameLibraryAsync(entry)));
            ShowFlyout(flyout, container, args);
            return;
        }

        if (entry.IsCollection)
        {
            var collection = LibraryService.Instance.Active.Collections.FirstOrDefault(c => c.Id == entry.Id);
            if (collection is not null)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
                flyout.Items.Add(MenuAction(L("打开"), "\uE8A7", () => OpenCollection(entry)));
                flyout.Items.Add(MenuAction(L("重命名"), "\uE8AC", () => _ = RenameCollectionAsync(collection)));
                flyout.Items.Add(MenuAction(L("解散文件夹"), "\uE74D", () => _ = DissolveCollectionAsync(collection)));
            }
            ShowFlyout(flyout, container, args);
            return;
        }

        var targets = LibraryService.Instance.TransferTargets;
        if (targets.Count > 0)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(TransferMenu(entry, targets, move: false));
            if (!LibraryService.Instance.IsOverview)
            {
                flyout.Items.Add(TransferMenu(entry, targets, move: true));
            }
        }

        if (entry.IsFolder)
        {
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(MenuAction(L("重命名"), "\uE8AC", () => _ = RenameFolderAsync(entry)));
            if (LibraryService.Instance.Active.Folders.Contains(entry.Path, StringComparer.OrdinalIgnoreCase))
            {
                flyout.Items.Add(MenuAction(L("从库中移除"), "\uE738", () => _ = RemoveSourceAsync(entry)));
            }
            if (PosterEligible(entry))
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
                flyout.Items.Add(MenuAction(L("联网查找海报"), "\uE753", () => _ = ShowPosterSearchAsync(entry)));
                flyout.Items.Add(MenuAction(L("选择本地图片"), "\uE722", () => _ = PickLocalPosterAsync(entry)));
            }
        }
        else
        {
            var library = LibraryService.Instance;
            var inFolder = library.CurrentCollection is not null ||
                library.CurrentFolder is { } folder && library.IsFolderMember(folder, entry.Path);
            flyout.Items.Add(new MenuFlyoutSeparator());
            flyout.Items.Add(MenuAction(L("重命名"), "\uE8AC", () => _ = RenameVideoAsync(entry)));
            if (PosterEligible(entry))
            {
                flyout.Items.Add(MenuAction(L("联网查找海报"), "\uE753", () => _ = ShowPosterSearchAsync(entry)));
                flyout.Items.Add(MenuAction(L("选择本地图片"), "\uE722", () => _ = PickLocalPosterAsync(entry)));
            }
            if (inFolder)
            {
                flyout.Items.Add(MenuAction(L("从文件夹移除"), "\uE8CB", () => _ = RemoveFromFolderAsync(entry)));
            }
            flyout.Items.Add(MenuAction(L("删除"), "\uE74D", () => _ = DeleteAsync(entry)));
        }

        ShowFlyout(flyout, container, args);
    }

    private void ShowBlankMenu(ContextRequestedEventArgs args)
    {
        args.Handled = true;
        var flyout = new MenuFlyout();
        flyout.Items.Add(MenuAction(L("新建文件夹"), "\uE8B7", () => _ = NewFolderAsync()));
        ShowFlyout(flyout, LibraryGrid, args);
    }

    private async Task NewFolderAsync()
    {
        var library = LibraryService.Instance;
        if (library.CurrentCollection is not null && library.GoUp())
        {
            ClearView();
            UpdateState();
        }
        var collection = await library.CreateCollectionAsync();
        await ReloadAsync();
        await RenameCollectionAsync(collection);
    }

    private static void ShowFlyout(MenuFlyout flyout, FrameworkElement target, ContextRequestedEventArgs args)
    {
        if (args.TryGetPosition(target, out var position))
        {
            flyout.ShowAt(target, new FlyoutShowOptions { Position = position });
            return;
        }
        flyout.ShowAt(target);
    }

    private void OpenCollection(LibraryEntry entry)
    {
        if (!LibraryService.Instance.OpenCollection(entry.Id))
        {
            return;
        }
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: true);
    }

    private async Task<string?> PromptNameAsync(string title, string value, string placeholder)
    {
        var nameBox = new TextBox { Text = value, PlaceholderText = placeholder };
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = title,
            Content = nameBox,
            PrimaryButtonText = L("确定"),
            CloseButtonText = L("取消"),
            DefaultButton = ContentDialogButton.Primary
        };
        nameBox.Loaded += (_, _) =>
        {
            nameBox.Focus(FocusState.Programmatic);
            nameBox.SelectAll();
        };
        var result = await dialog.ShowAsync();
        Focus(FocusState.Pointer);
        return result == ContentDialogResult.Primary ? nameBox.Text : null;
    }

    private async Task RenameCollectionAsync(VideoCollection collection, string? name = null)
    {
        name ??= await PromptNameAsync(L("重命名文件夹"), collection.Name, L("文件夹名称"));
        if (name is null)
        {
            return;
        }
        LibraryService.Instance.RenameCollection(collection, name);
        ClearView();
        UpdateState();
        await RefreshAsync(rescan: true);
    }

    private async Task RenameLibraryAsync(LibraryEntry entry, string? name = null)
    {
        if (LibraryService.Instance.Find(entry.Id) is not { } library)
        {
            return;
        }
        name ??= await PromptNameAsync(L("重命名库"), library.Name, L("库名称"));
        if (name is null)
        {
            return;
        }
        LibraryService.Instance.Rename(library, name);
        MainWindow.Instance?.SyncLibraryName(library);
        ClearView();
        UpdateState();
        await RefreshAsync(rescan: true);
        Notice(L("已重命名"));
    }

    private async Task RenameFolderAsync(LibraryEntry entry, string? name = null) =>
        await RenameEntryAsync("kind.文件夹", L("文件夹名称"), entry, name);

    private async Task RenameVideoAsync(LibraryEntry entry, string? name = null) =>
        await RenameEntryAsync("kind.视频", L("视频名称"), entry, name);

    private async Task RenameEntryAsync(string kindKey, string placeholder, LibraryEntry entry, string? name = null)
    {
        name ??= await PromptNameAsync(LocalizationService.L("重命名{0}", L(kindKey)), entry.Name, placeholder);
        if (name is null || name.Trim() == entry.Name)
        {
            return;
        }
        if (!await LibraryService.Instance.RenamePathAsync(entry.Path, name))
        {
            Notice(L("重命名失败"));
            return;
        }
        LibraryService.Instance.ResetPosterSearch(entry.Path);
        ClearView();
        UpdateState();
        await RefreshAsync(rescan: false);
        Notice(L("已重命名"));
    }

    private async Task DissolveCollectionAsync(VideoCollection collection)
    {
        await LibraryService.Instance.DissolveCollectionAsync(collection);
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: false);
        Notice(L("已解散文件夹"));
    }

    private async Task RemoveFromFolderAsync(LibraryEntry entry)
    {
        var library = LibraryService.Instance;
        if (library.CurrentCollection is not null)
        {
            await library.RemoveFromCollectionAsync([entry.Path]);
        }
        else if (library.CurrentFolder is { } folder)
        {
            await library.RemoveFromFolderAsync(folder, [entry.Path]);
        }
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: false);
        Notice(L("已移出文件夹"));
    }

    private async Task ShowDetailsAsync(LibraryEntry entry)
    {
        var head = new Grid { ColumnSpacing = 16 };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var poster = new Border
        {
            Width = 108,
            Height = 162,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0x16, 0x80, 0x80, 0x80))
        };
        poster.Child = entry.Thumbnail is { } thumbnail
            ? new Image { Source = thumbnail, Stretch = Stretch.UniformToFill }
            : new FontIcon
            {
                Glyph = entry.IsVideo ? "\uE8B9" : "\uE8B7",
                FontSize = 26,
                Opacity = 0.3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

        var nameBox = new TextBox
        {
            Text = entry.Name,
            PlaceholderText = L("名称"),
            FontSize = 17,
            FontFamily = (FontFamily)Application.Current.Resources["AppUiFontBold"],
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = false,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0,
            Background = new SolidColorBrush(Colors.Transparent)
        };
        nameBox.Loaded += (_, _) =>
        {
            nameBox.Focus(FocusState.Programmatic);
            nameBox.SelectAll();
        };

        var title = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        title.Children.Add(nameBox);
        title.Children.Add(new TextBlock
        {
            Text = entry.IsLibrary
                ? LocalizationService.L("库 · {0} 个视频", entry.ItemCount)
                : entry.IsCollection
                    ? LocalizationService.L("文件夹 · {0} 个视频", entry.ItemCount)
                    : entry.IsFolder
                        ? L("kind.文件夹")
                        : LocalizationService.L("视频 · {0}", FormatSize(entry.SizeBytes)),
            FontSize = 12,
            Opacity = 0.6
        });

        Grid.SetColumn(poster, 0);
        Grid.SetColumn(title, 1);
        head.Children.Add(poster);
        head.Children.Add(title);

        var rows = new StackPanel { Spacing = 8 };
        if (entry.IsLibrary)
        {
            rows.Children.Add(InfoRow(L("类型"), L("kind.库")));
            rows.Children.Add(InfoRow(L("数量"), LocalizationService.L("{0} 个文件夹 · {1} 个视频", entry.FolderCount, entry.ItemCount)));
        }
        else if (entry.IsCollection)
        {
            rows.Children.Add(InfoRow(L("位置"), string.IsNullOrEmpty(entry.Path)
                ? LibraryService.Instance.Active.Name
                : entry.Path));
            rows.Children.Add(InfoRow(L("类型"), L("kind.文件夹")));
            rows.Children.Add(InfoRow(L("数量"), LocalizationService.L("{0} 个视频", entry.ItemCount)));
        }
        else if (entry.IsFolder)
        {
            rows.Children.Add(InfoRow(L("位置"), entry.Path));
            rows.Children.Add(InfoRow(L("类型"), L("kind.文件夹")));
        }
        else
        {
            rows.Children.Add(InfoRow(L("位置"), entry.Path));
            var subtitlePath = SubtitlePathOf(entry);
            if (subtitlePath is not null)
            {
                rows.Children.Add(InfoRow(L("字幕"), subtitlePath));
            }
            rows.Children.Add(InfoRow(L("大小"), FormatSize(entry.SizeBytes)));
            rows.Children.Add(InfoRow(L("时长"), entry.DurationSeconds > 1
                ? FormatDuration(TimeSpan.FromSeconds(entry.DurationSeconds))
                : L("未知")));
            rows.Children.Add(InfoRow(L("观看"), entry.PlayCount > 0 ? LocalizationService.L("{0} 次", entry.PlayCount) : L("未播放")));
            rows.Children.Add(InfoRow(L("上次"), FormatLastPlayed(entry.LastPlayed)));
            rows.Children.Add(InfoRow(L("进度"), entry.ProgressRatio > 0
                ? $"{entry.ProgressRatio * 100:0}%"
                : L("未开始")));
        }

        var body = new StackPanel { Spacing = 16, Width = 420 };
        body.Children.Add(head);
        body.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x1A, 0x80, 0x80, 0x80))
        });
        body.Children.Add(rows);

        var result = await new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = L("详细信息"),
            Content = body,
            PrimaryButtonText = L("保存名称"),
            CloseButtonText = L("关闭"),
            DefaultButton = ContentDialogButton.Primary
        }.ShowAsync();
        Focus(FocusState.Pointer);
        if (result != ContentDialogResult.Primary)
        {
            return;
        }
        var name = nameBox.Text;
        if (name.Trim() == entry.Name)
        {
            return;
        }
        if (entry.IsLibrary)
        {
            await RenameLibraryAsync(entry, name);
            return;
        }
        if (entry.IsCollection)
        {
            var collection = LibraryService.Instance.Active.Collections.FirstOrDefault(c => c.Id == entry.Id);
            if (collection is not null)
            {
                await RenameCollectionAsync(collection, name);
            }
            return;
        }
        if (entry.IsFolder)
        {
            await RenameFolderAsync(entry, name);
            return;
        }
        await RenameVideoAsync(entry, name);
    }

    private static string? SubtitlePathOf(LibraryEntry entry)
    {
        if (!entry.IsVideo)
        {
            return null;
        }
        if (SettingsService.Current.ExternalSubtitles.TryGetValue(entry.Path, out var saved))
        {
            if (saved.Length == 0)
            {
                return null;
            }
            if (File.Exists(saved))
            {
                return saved;
            }
        }
        return SubtitleService.FindMatchedPath(entry.Path);
    }

    private const string CandidateTemplateXaml = """
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
          <Grid Width="116" Margin="3">
            <Grid.RowDefinitions>
              <RowDefinition Height="Auto" />
              <RowDefinition Height="Auto" />
              <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <Border Grid.Row="0" Height="156" CornerRadius="8" Background="#16808080">
              <Image Source="{Binding Image}" Stretch="Uniform" />
            </Border>
            <TextBlock Grid.Row="1" Text="{Binding Title}" FontSize="12" TextTrimming="CharacterEllipsis" Margin="0,6,0,0" ToolTipService.ToolTip="{Binding Title}" />
            <TextBlock Grid.Row="2" Text="{Binding Subtitle}" FontSize="11" Opacity="0.55" TextTrimming="CharacterEllipsis" />
          </Grid>
        </DataTemplate>
        """;

    private bool PosterEligible(LibraryEntry entry)
    {
        var library = LibraryService.Instance;
        if (library.CurrentFolder is not null || library.CurrentCollection is not null)
        {
            return false;
        }
        return entry.IsFolder && library.Active.Folders.Contains(entry.Path, StringComparer.OrdinalIgnoreCase)
            || entry.IsVideo && library.Active.Files.Contains(entry.Path, StringComparer.OrdinalIgnoreCase);
    }

    private async Task ShowPosterSearchAsync(LibraryEntry entry)
    {
        var title = PosterSearchService.ParseTitle(entry.Name, out var parsedYear, out var parsedSeason);
        var queryBox = new TextBox { Text = title, PlaceholderText = L("搜索关键词") };
        var searchButton = new Button { Content = L("搜索"), Padding = new Thickness(12, 4, 12, 4) };
        var status = new TextBlock { FontSize = 12, Opacity = 0.55, TextWrapping = TextWrapping.Wrap };
        var candidates = new ObservableCollection<PosterOption>();
        var grid = new GridView
        {
            ItemsSource = candidates,
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 350,
            ItemTemplate = (DataTemplate)XamlReader.Load(CandidateTemplateXaml)
        };

        var head = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 8) };
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(queryBox, 0);
        Grid.SetColumn(searchButton, 1);
        head.Children.Add(queryBox);
        head.Children.Add(searchButton);

        var body = new StackPanel { Spacing = 8, Width = 480 };
        body.Children.Add(head);
        body.Children.Add(grid);
        body.Children.Add(status);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = entry.Name,
            Content = body,
            PrimaryButtonText = L("使用选中海报"),
            CloseButtonText = L("取消"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false
        };
        if (LibraryService.Instance.HasPosterOverride(entry.Path))
        {
            dialog.SecondaryButtonText = L("恢复默认");
        }

        async Task RunSearchAsync()
        {
            var query = queryBox.Text.Trim();
            if (query.Length == 0)
            {
                return;
            }
            searchButton.IsEnabled = false;
            dialog.IsPrimaryButtonEnabled = false;
            grid.SelectedItem = null;
            candidates.Clear();
            status.Text = L("正在搜索…");
            try
            {
                var results = await PosterSearchService.SearchAsync(query, parsedYear, parsedSeason);
                foreach (var candidate in results)
                {
                    candidates.Add(new PosterOption
                    {
                        Title = candidate.Title,
                        Subtitle = candidate.Subtitle,
                        ImageUrl = candidate.ImageUrl,
                        Image = new BitmapImage(new Uri(candidate.ImageUrl)) { DecodePixelWidth = 240 }
                    });
                }
                status.Text = results.Count == 0
                    ? L("没有找到候选，试试修改关键词")
                    : LocalizationService.L("找到 {0} 个候选", results.Count) +
                      (parsedSeason is { } n ? LocalizationService.L("· 已识别第{0}季，对应海报排前", n) : "") +
                      L("· 数据来自 IMDb / TVmaze / Bangumi");
            }
            catch
            {
                status.Text = L("搜索失败，请检查网络连接");
            }
            searchButton.IsEnabled = true;
        }

        searchButton.Click += async (_, _) => await RunSearchAsync();
        queryBox.KeyDown += (_, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                _ = RunSearchAsync();
            }
        };
        grid.SelectionChanged += (_, _) =>
            dialog.IsPrimaryButtonEnabled = grid.SelectedItem is PosterOption;

        _ = RunSearchAsync();
        var result = await dialog.ShowAsync();
        Focus(FocusState.Pointer);
        if (result == ContentDialogResult.Primary && grid.SelectedItem is PosterOption pick)
        {
            var file = await PosterSearchService.DownloadAsync(pick.ImageUrl);
            if (file is null)
            {
                Notice(L("海报下载失败，请重试"));
                return;
            }
            LibraryService.Instance.SetPosterOverride(entry.Path, file);
            await RefreshEntryThumbnailAsync(entry);
            Notice(L("已更新海报"));
        }
        else if (result == ContentDialogResult.Secondary)
        {
            LibraryService.Instance.ClearPosterOverride(entry.Path);
            await RefreshEntryThumbnailAsync(entry);
            Notice(L("已恢复默认封面"));
        }
    }

    private async Task PickLocalPosterAsync(LibraryEntry entry)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp" })
        {
            picker.FileTypeFilter.Add(extension);
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
        var saved = await PosterSearchService.SaveLocalPosterAsync(file.Path);
        if (saved is null)
        {
            Notice(L("图片文件无效"));
            return;
        }
        LibraryService.Instance.SetPosterOverride(entry.Path, saved);
        await RefreshEntryThumbnailAsync(entry);
        Notice(L("已更新海报"));
    }

    private async Task RefreshEntryThumbnailAsync(LibraryEntry entry)
    {
        entry.Thumbnail = null;
        var cachePath = await LibraryService.GetPosterPathAsync(entry);
        if (cachePath is null)
        {
            return;
        }
        entry.Thumbnail = new BitmapImage
        {
            CreateOptions = BitmapCreateOptions.IgnoreImageCache,
            DecodePixelWidth = 320,
            UriSource = new Uri(cachePath)
        };
    }

    private async Task AutoFetchPostersAsync()
    {
        if (!SettingsService.Current.AutoPosterFetch)
        {
            return;
        }
        var library = LibraryService.Instance;
        if (library.CurrentFolder is not null || library.CurrentCollection is not null)
        {
            return;
        }
        var targets = library.Entries
            .Where(e => e.IsFolder && library.Active.Folders.Contains(e.Path, StringComparer.OrdinalIgnoreCase)
                || e.IsVideo && library.Active.Files.Contains(e.Path, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (targets.Count == 0)
        {
            return;
        }
        var dispatcher = DispatcherQueue;
        await PosterSearchService.AutoFetchAsync(targets, entry =>
            dispatcher.TryEnqueue(() => _ = RefreshEntryThumbnailAsync(entry)));
    }

    private MenuFlyoutSubItem TransferMenu(LibraryEntry entry, IReadOnlyList<VideoLibrary> targets, bool move)
    {
        var sub = new MenuFlyoutSubItem
        {
            Text = move ? L("移动至其他库") : L("复制至其他库"),
            Icon = new FontIcon { Glyph = move ? "\uE8DE" : "\uE8C8" }
        };
        foreach (var target in targets)
        {
            var item = new MenuFlyoutItem { Text = target.Name };
            item.Click += (_, _) => _ = TransferAsync(entry, target, move);
            sub.Items.Add(item);
        }
        return sub;
    }

    private async Task TransferAsync(LibraryEntry entry, VideoLibrary target, bool move)
    {
        await LibraryService.Instance.TransferAsync(entry, target, move);
        if (move)
        {
            if (entry.IsFolder)
            {
                ClearView();
                UpdateState();
                _ = RefreshAsync(rescan: false);
            }
            else
            {
                UpdateState();
            }
        }
        Notice(LocalizationService.L(move ? "已移动到「{0}」" : "已复制到「{0}」", target.Name));
    }

    private void Notice(string message)
    {
        NoticeText.Text = message;
        NoticeToast.Visibility = Visibility.Visible;
        _noticeTimer.Stop();
        _noticeTimer.Start();
        PlayNotice(true);
    }

    private void PlayNotice(bool show)
    {
        var duration = TimeSpan.FromMilliseconds(show ? 180 : 150);
        var ease = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var fade = new DoubleAnimation { To = show ? 1 : 0, Duration = duration, EasingFunction = ease };
        var slide = new DoubleAnimation { From = show ? 6 : 0, To = show ? 0 : -6, Duration = duration, EasingFunction = ease };
        Storyboard.SetTarget(fade, NoticeToast);
        Storyboard.SetTargetProperty(fade, "Opacity");
        Storyboard.SetTarget(slide, NoticeSlide);
        Storyboard.SetTargetProperty(slide, "Y");
        var board = new Storyboard();
        board.Children.Add(fade);
        board.Children.Add(slide);
        var token = ++_noticeToken;
        if (!show)
        {
            board.Completed += (_, _) =>
            {
                if (token == _noticeToken)
                {
                    NoticeToast.Visibility = Visibility.Collapsed;
                }
            };
        }
        board.Begin();
    }

    private async Task DeleteAsync(LibraryEntry entry)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = entry.Name,
            FontFamily = (FontFamily)Application.Current.Resources["AppUiFontBold"],
            FontWeight = FontWeights.Bold,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = entry.Path,
            FontSize = 12,
            Opacity = 0.55,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = L("文件将移入回收站。"),
            FontSize = 12,
            Opacity = 0.55
        });

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = L("删除视频"),
            Content = panel,
            PrimaryButtonText = L("删除"),
            CloseButtonText = L("取消"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            Focus(FocusState.Pointer);
            return;
        }
        Focus(FocusState.Pointer);
        if (await LibraryService.Instance.DeleteEntryAsync(entry))
        {
            _entries.Remove(entry);
            if (LibraryService.Instance.IsOverview)
            {
                _videoTotal--;
            }
            UpdateState();
            UpdatePager();
            return;
        }
        await new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = L("无法删除"),
            Content = new TextBlock
            {
                Text = L("文件可能正在被使用，或当前账户没有权限。"),
                TextWrapping = TextWrapping.Wrap
            },
            CloseButtonText = L("知道了")
        }.ShowAsync();
        Focus(FocusState.Pointer);
    }

    private async Task RemoveSourceAsync(LibraryEntry entry)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = entry.Path, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock
        {
            Text = L("只从当前库移除这个来源，不会删除磁盘上的文件。"),
            FontSize = 12,
            Opacity = 0.55,
            TextWrapping = TextWrapping.Wrap
        });

        var confirm = new ContentDialog
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ThemeService.Current,
            Title = L("从库中移除"),
            Content = panel,
            PrimaryButtonText = L("移除"),
            CloseButtonText = L("取消"),
            DefaultButton = ContentDialogButton.Close
        };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary)
        {
            Focus(FocusState.Pointer);
            return;
        }
        Focus(FocusState.Pointer);
        await LibraryService.Instance.RemoveFolderAsync(entry.Path);
        ClearView();
        UpdateState();
        _ = RefreshAsync(rescan: false);
    }

    private static MenuFlyoutItem MenuAction(string text, string glyph, Action action)
    {
        var item = new MenuFlyoutItem { Text = text, Icon = new FontIcon { Glyph = glyph } };
        item.Click += (_, _) => action();
        return item;
    }
    private static Grid InfoRow(string label, string value)
    {
        var row = new Grid { ColumnSpacing = 12 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var key = new TextBlock { Text = label, FontSize = 12, Opacity = 0.55 };
        var val = new TextBlock { Text = value, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        ToolTipService.SetToolTip(val, value);

        Grid.SetColumn(key, 0);
        Grid.SetColumn(val, 1);
        row.Children.Add(key);
        row.Children.Add(val);
        return row;
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

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return LocalizationService.L("未知");
        }
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan time) =>
        time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes}:{time.Seconds:00}";

    private static string FormatLastPlayed(DateTime time)
    {
        if (time == DateTime.MinValue)
        {
            return LocalizationService.L("从未播放");
        }
        var today = DateTime.Today;
        if (time.Date == today)
        {
            return LocalizationService.L("今天 {0}", time.ToString("HH:mm"));
        }
        if (time.Date == today.AddDays(-1))
        {
            return LocalizationService.L("昨天 {0}", time.ToString("HH:mm"));
        }
        return time.Year == today.Year
            ? time.ToString(LocalizationService.L("M月d日 HH:mm"))
            : time.ToString(LocalizationService.L("yyyy年M月d日 HH:mm"));
    }
}
