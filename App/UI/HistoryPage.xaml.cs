using System.ComponentModel;
using FilmField.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI;

namespace FilmField.Views;

public sealed class HistoryEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public string PlayCountText { get; init; } = "";
    public DateTime LastPlayed { get; init; }

    private ImageSource? _thumbnail;

    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (!ReferenceEquals(_thumbnail, value))
            {
                _thumbnail = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
            }
        }
    }
}

public sealed partial class HistoryPage : Page
{
    private const int WeekCount = 53;
    private const double CellGap = 3;
    private const double MaxCellSize = 14;
    private const double PageInset = 32;
    private const double CardPadding = 36;
    private const double WeekdayWidth = 20;
    private const double StatsWidth = 180;
    private const double ColumnSpacing = 24;
    private readonly HashSet<string> _collapsed = [];
    private double _heatSize;

    public HistoryPage()
    {
        InitializeComponent();
        ActualThemeChanged += (_, _) =>
        {
            if (_heatSize > 0)
            {
                BuildHeatmap(_heatSize);
            }
        };
    }

    internal string L(string key) => LocalizationService.L(key);

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _ = LoadRecentAsync();
    }

    private void PageScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var available = e.NewSize.Width - PageInset - CardPadding - WeekdayWidth - StatsWidth - ColumnSpacing * 2 - 1;
        if (available < 474)
        {
            return;
        }
        var size = Math.Min(MaxCellSize, (available - CellGap * (WeekCount - 1)) / WeekCount);
        if (Math.Abs(size - _heatSize) < 0.4)
        {
            return;
        }
        _heatSize = size;
        BuildHeatmap(size);
    }

    private void BuildHeatmap(double size)
    {
        var daily = CollectDaily();
        var today = DateTime.Today;
        var lastMonday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var firstMonday = lastMonday.AddDays(-7 * (WeekCount - 1));

        var dark = ActualTheme == ElementTheme.Dark ||
            (ActualTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
        var accent = Application.Current.Resources.TryGetValue("SystemAccentColor", out var value) && value is Color color
            ? color
            : Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);

        Color Swatch(int level)
        {
            if (level <= 0)
            {
                return dark
                    ? Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x12, 0x00, 0x00, 0x00);
            }
            var alpha = level switch
            {
                1 => 0x4D,
                2 => 0x80,
                3 => 0xBF,
                _ => 0xFF
            };
            return Color.FromArgb((byte)alpha, accent.R, accent.G, accent.B);
        }

        HeatWeeks.Children.Clear();
        MonthCanvas.Children.Clear();
        WeekdayColumn.Children.Clear();
        LegendSwatches.Children.Clear();

        var total = 0;
        var activeDays = 0;
        var lastMonth = -1;

        for (var week = 0; week < WeekCount; week++)
        {
            var monday = firstMonday.AddDays(week * 7);
            for (var row = 0; row < 7; row++)
            {
                var day = monday.AddDays(row);
                if (day.Day != 1 || lastMonth == day.Month)
                {
                    continue;
                }
                var label = new TextBlock
                {
                    Text = LocalizationService.L("{0}月", day.Month),
                    FontSize = Math.Clamp(size * 0.72, 8, 12),
                    Opacity = 0.45
                };
                Canvas.SetLeft(label, week * (size + CellGap));
                MonthCanvas.Children.Add(label);
                lastMonth = day.Month;
                break;
            }

            var column = new StackPanel { Orientation = Orientation.Vertical, Spacing = CellGap };
            for (var row = 0; row < 7; row++)
            {
                var date = monday.AddDays(row);
                var count = daily.GetValueOrDefault(date.ToString("yyyy-MM-dd"));
                var box = new Border
                {
                    Width = size,
                    Height = size,
                    CornerRadius = new CornerRadius(Math.Max(2, size * 0.16)),
                    Background = new SolidColorBrush(Swatch(LevelOf(count)))
                };
                if (date > today)
                {
                    box.Opacity = 0;
                    box.IsHitTestVisible = false;
                }
                else if (count > 0)
                {
                    total += count;
                    activeDays++;
                    ToolTipService.SetToolTip(box, $"{date.ToString(L("yyyy年M月d日"))} · {LocalizationService.L("播放 {0} 次", count)}");
                }
                column.Children.Add(box);
            }
            HeatWeeks.Children.Add(column);
        }

        MonthCanvas.Width = WeekCount * (size + CellGap) - CellGap;
        MonthCanvas.Height = Math.Clamp(size * 0.72, 8, 12) + 5;

        for (var level = 0; level <= 4; level++)
        {
            LegendSwatches.Children.Add(new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(Math.Max(2, size * 0.16)),
                Background = new SolidColorBrush(Swatch(level))
            });
        }

        for (var row = 0; row < 7; row++)
        {
            WeekdayColumn.Children.Add(new TextBlock
            {
                Text = row switch
                {
                    0 => L("一"),
                    2 => L("三"),
                    4 => L("五"),
                    6 => L("日"),
                    _ => ""
                },
                FontSize = Math.Clamp(size * 0.7, 7, 11),
                LineHeight = size,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                Height = size,
                Opacity = 0.45
            });
        }

        StatTotal.Text = LocalizationService.L("{0} 次", total);
        StatDays.Text = LocalizationService.L("{0} 天", activeDays);
        StatStreak.Text = LocalizationService.L("{0} 天", LongestStreak(daily, firstMonday, today));
        StatLast.Text = FormatLast(CollectLastPlayed());
    }

    private static int LongestStreak(IReadOnlyDictionary<string, int> daily, DateTime from, DateTime to)
    {
        var streak = 0;
        var run = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (daily.ContainsKey(day.ToString("yyyy-MM-dd")))
            {
                run++;
                streak = Math.Max(streak, run);
            }
            else
            {
                run = 0;
            }
        }
        return streak;
    }

    private static DateTime CollectLastPlayed()
    {
        var last = DateTime.MinValue;
        foreach (var library in LibraryService.Instance.Libraries)
        {
            foreach (var record in library.History.Values)
            {
                if (record.LastPlayed > last)
                {
                    last = record.LastPlayed;
                }
            }
        }
        return last;
    }

    private static string FormatLast(DateTime time) => time == DateTime.MinValue
        ? "—"
        : time.Date == DateTime.Today
            ? LocalizationService.L("今天 {0}", time.ToString("HH:mm"))
            : time.Year == DateTime.Today.Year
                ? time.ToString(LocalizationService.L("M月d日"))
                : time.ToString(LocalizationService.L("yyyy年M月d日"));

    private static int LevelOf(int count) => count switch
    {
        <= 0 => 0,
        1 => 1,
        <= 3 => 2,
        <= 6 => 3,
        _ => 4
    };

    private static Dictionary<string, int> CollectDaily()
    {
        var totals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var library in LibraryService.Instance.Libraries)
        {
            foreach (var (day, count) in library.Daily)
            {
                if (count > 0)
                {
                    totals[day] = totals.GetValueOrDefault(day) + count;
                }
            }
            foreach (var record in library.History.Values)
            {
                if (record.LastPlayed == DateTime.MinValue)
                {
                    continue;
                }
                var day = record.LastPlayed.ToString("yyyy-MM-dd");
                if (!library.Daily.ContainsKey(day))
                {
                    totals[day] = totals.GetValueOrDefault(day) + 1;
                }
            }
        }
        return totals;
    }

    private async Task LoadRecentAsync()
    {
        var libraries = LibraryService.Instance;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<HistoryEntry>();
        foreach (var library in libraries.Libraries)
        {
            foreach (var (path, record) in library.History)
            {
                if (record.LastPlayed == DateTime.MinValue || !seen.Add(path))
                {
                    continue;
                }
                items.Add(new HistoryEntry
                {
                    Path = path,
                    Name = libraries.DisplayNameOf(path),
                    PlayCountText = LocalizationService.L("播放 {0} 次", record.PlayCount),
                    LastPlayed = record.LastPlayed
                });
            }
        }

        var ordered = items.OrderByDescending(item => item.LastPlayed).ToList();
        HistoryGrid.ItemsSource = ordered;
        RecentHeader.Visibility = ordered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        RecentEmpty.Visibility = ordered.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        RecentCountText.Text = ordered.Count > 0 ? LocalizationService.L("{0} 个视频", ordered.Count) : "";
        CountText.Text = ordered.Count > 0 ? LocalizationService.L("{0} 个条目", ordered.Count) : "";
        ApplyCollapse();
        await LoadThumbnailsAsync(ordered);
    }

    private async Task LoadThumbnailsAsync(IEnumerable<HistoryEntry> entries)
    {
        using var gate = new SemaphoreSlim(4);
        var dispatcher = DispatcherQueue;
        var tasks = entries.Select(async entry =>
        {
            await gate.WaitAsync();
            try
            {
                var item = new LibraryEntry { Path = entry.Path, Kind = LibraryEntryKind.Video };
                var cachePath = await LibraryService.GetPosterPathAsync(item);
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
            catch
            {
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks);
    }

    private void ApplyCollapse()
    {
        var collapsed = _collapsed.Contains("recent");
        HistoryGrid.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
        RecentChevron.Glyph = collapsed ? "\uE76C" : "\uE70D";
    }

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

    private void HistoryGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is HistoryEntry entry && MainWindow.Instance is { } window)
        {
            _ = window.PlayFromLibraryAsync(entry.Path);
        }
    }
}
