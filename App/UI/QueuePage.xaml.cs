using FilmField.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;

namespace FilmField.Views;

public sealed class QueueEntry
{
    public QueueEntry(StorageFile file, bool isCurrent)
    {
        File = file;
        IsCurrent = isCurrent;
    }

    public StorageFile File { get; }
    public string DisplayName => LibraryService.Instance.CustomNameOf(File.Path) ?? File.Name;
    public bool IsCurrent { get; }
    public Visibility PlayingVisibility => IsCurrent ? Visibility.Visible : Visibility.Collapsed;
    public string RemoveTooltip => LocalizationService.L("移除");
}

public sealed partial class QueuePage : Page
{
    public QueuePage()
    {
        InitializeComponent();
        PlaylistService.Instance.QueueChanged += OnPlaylistChanged;
        PlaylistService.Instance.CurrentIndexChanged += OnCurrentIndexChanged;
        Unloaded += (_, _) =>
        {
            PlaylistService.Instance.QueueChanged -= OnPlaylistChanged;
            PlaylistService.Instance.CurrentIndexChanged -= OnCurrentIndexChanged;
        };
    }

    internal string L(string key) => LocalizationService.L(key);

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        RefreshList();
    }

    private void OnPlaylistChanged()
    {
        DispatcherQueue.TryEnqueue(RefreshList);
    }

    private void OnCurrentIndexChanged(int index)
    {
        DispatcherQueue.TryEnqueue(RefreshList);
    }

    private void RefreshList()
    {
        var playlist = PlaylistService.Instance;
        QueueList.ItemsSource = playlist.Items
            .Select((f, i) => new QueueEntry(f, i == playlist.CurrentIndex))
            .ToList();
        QueueList.SelectedIndex = playlist.CurrentIndex;
        EmptyState.Visibility = playlist.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        QueueList.Visibility = playlist.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        CountText.Text = playlist.Count > 0 ? LocalizationService.L("{0} 个条目", playlist.Count) : "";
    }

    private void QueueList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is QueueEntry entry)
        {
            PlaylistService.Instance.JumpTo(PlaylistService.Instance.IndexOf(entry.File));
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is QueueEntry entry)
        {
            PlaylistService.Instance.RemoveAt(PlaylistService.Instance.IndexOf(entry.File));
        }
    }
}
