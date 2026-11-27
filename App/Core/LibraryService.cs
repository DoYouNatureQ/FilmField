using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace FilmField.Services;

public enum LibraryEntryKind
{
    Video,
    Folder,
    Collection,
    Library
}

public sealed class LibraryEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public string Id { get; init; } = "";
    public LibraryEntryKind Kind { get; init; }
    public long SizeBytes { get; init; }
    public DateTime LastPlayed { get; init; }
    public int PlayCount { get; init; }
    public double DurationSeconds { get; init; }
    public double ProgressRatio { get; init; }
    public int ItemCount { get; init; }
    public int FolderCount { get; init; }
    public string? PosterSourcePath { get; init; }

    public bool IsFolder => Kind == LibraryEntryKind.Folder;
    public bool IsCollection => Kind == LibraryEntryKind.Collection;
    public bool IsVideo => Kind == LibraryEntryKind.Video;
    public bool IsLibrary => Kind == LibraryEntryKind.Library;

    public string ItemCountText => ItemCount.ToString();
    public string FolderCountText => FolderCount.ToString();
    public string SummaryText => LocalizationService.L("{0} 个文件夹 · {1} 个视频", FolderCount, ItemCount);

    public Visibility ProgressVisibility =>
        !IsFolder && ProgressRatio > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PillVisibility => IsCollection || IsLibrary ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VideoGlyphVisibility => IsCollection || IsLibrary ? Visibility.Collapsed : Visibility.Visible;
    public Visibility FolderGlyphVisibility => IsCollection || IsLibrary ? Visibility.Visible : Visibility.Collapsed;

    private bool _isDropTarget;

    public bool IsDropTarget
    {
        get => _isDropTarget;
        set
        {
            if (_isDropTarget != value)
            {
                _isDropTarget = value;
                Raise(nameof(IsDropTarget));
                Raise(nameof(DropRingVisibility));
            }
        }
    }

    public Visibility DropRingVisibility => IsDropTarget ? Visibility.Visible : Visibility.Collapsed;

    private ImageSource? _thumbnail;

    public ImageSource? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (!ReferenceEquals(_thumbnail, value))
            {
                _thumbnail = value;
                Raise();
            }
        }
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class LibrarySection : ObservableCollection<LibraryEntry>
{
    public LibrarySection(string key, string title, string subtitle, IEnumerable<LibraryEntry> items, bool expanded)
    {
        Key = key;
        Title = title;
        Subtitle = subtitle;
        All = [.. items];
        _expanded = expanded;
        if (expanded)
        {
            AddAll();
        }
    }

    public string Key { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public List<LibraryEntry> All { get; }
    public bool IsExpanded => _expanded;
    public string Glyph => _expanded ? "\uE70D" : "\uE76C";
    public Visibility HeaderVisibility => Title.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

    private bool _expanded;

    public void Toggle()
    {
        _expanded = !_expanded;
        if (_expanded)
        {
            AddAll();
        }
        else
        {
            Clear();
        }
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Glyph)));
    }

    private void AddAll()
    {
        foreach (var item in All)
        {
            Add(item);
        }
    }
}

public sealed class HistoryRecord
{
    public DateTime LastPlayed { get; set; }
    public int PlayCount { get; set; }
    public double DurationSeconds { get; set; }
}

public sealed class VideoCollection
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Parent { get; set; } = "";
    public List<string> Files { get; set; } = [];
}

public sealed class VideoLibrary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Folders { get; set; } = [];
    public List<string> Files { get; set; } = [];
    public List<string> Hidden { get; set; } = [];
    public Dictionary<string, List<string>> FolderItems { get; set; } = [];
    public List<VideoCollection> Collections { get; set; } = [];
    public Dictionary<string, int> Sorts { get; set; } = [];
    public Dictionary<string, HistoryRecord> History { get; set; } = [];
    public Dictionary<string, int> Daily { get; set; } = [];
}

public static class PosterDrag
{
    public static IReadOnlyList<LibraryEntry> Entries { get; set; } = [];
}

public sealed class LibraryService
{
    private static LibraryService? _instance;

    public static LibraryService Instance => _instance ??= new();

    public VideoLibrary Home => _store.Libraries[0];
    public IReadOnlyList<VideoLibrary> Libraries => _store.Libraries;
    public VideoLibrary Active { get; private set; }
    public string? CurrentFolder { get; private set; }
    public VideoCollection? CurrentCollection { get; private set; }
    public IReadOnlyList<LibraryEntry> Entries => _view;

    public bool IsOverview =>
        ReferenceEquals(Active, Home) && !_entered && CurrentFolder is null && CurrentCollection is null;

    public bool CanGoBack =>
        CurrentFolder is not null || CurrentCollection is not null || (ReferenceEquals(Active, Home) && _entered);

    public IReadOnlyList<string> Breadcrumbs
    {
        get
        {
            var names = FolderNames();
            if (CurrentCollection is { } collection)
            {
                names.Add(collection.Name);
            }
            return names;
        }
    }

    public string ViewKey => CurrentCollection is { } collection
        ? CollectionKey(collection.Id)
        : CurrentFolder ?? "";

    private static string CollectionKey(string id) => $"collection:{id}";

    private List<string> FolderNames()
    {
        var folder = CurrentFolder;
        if (folder is null)
        {
            return [];
        }
        var names = new List<string>();
        var root = EntryRootOf(folder);
        if (root is null)
        {
            names.Add(CustomNameOf(folder) ?? FolderName(folder));
            return names;
        }
        names.Add(CustomNameOf(root) ?? FolderName(root));
        var relative = Path.GetRelativePath(root, folder);
        if (relative != ".")
        {
            var current = root;
            foreach (var segment in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
            {
                current = Path.Combine(current, segment);
                names.Add(CustomNameOf(current) ?? FolderName(current));
            }
        }
        return names;
    }

    private sealed class LibraryStore
    {
        public List<VideoLibrary> Libraries { get; set; } = [];
        public Dictionary<string, string> Names { get; set; } = [];
        public Dictionary<string, string> Posters { get; set; } = [];
        public List<string> PosterMisses { get; set; } = [];
    }

    private sealed class LegacyStore
    {
        public List<string> Folders { get; set; } = [];
        public Dictionary<string, HistoryRecord> History { get; set; } = [];
    }

    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FilmField");
    private static readonly string FilePath = Path.Combine(Dir, "libraries.json");
    private static readonly string LegacyPath = Path.Combine(Dir, "library.json");
    private static readonly string ThumbDir = Path.Combine(Dir, "thumbs");
    private const uint PosterWidth = 480;
    private const uint PosterHeight = 720;
    private const int PosterQuality = 88;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly LibraryStore _store = Load();
    private readonly List<LibraryEntry> _view = [];
    private bool _entered;

    private LibraryService()
    {
        Active = _store.Libraries[0];
    }

    private static LibraryStore Load()
    {
        var store = Read() ?? Migrate() ?? new LibraryStore();
        if (store.Libraries.Count == 0)
        {
            store.Libraries.Add(new VideoLibrary { Id = NewId(), Name = LocalizationService.L("主页") });
        }
        foreach (var library in store.Libraries)
        {
            library.History = new Dictionary<string, HistoryRecord>(library.History, StringComparer.OrdinalIgnoreCase);
            library.Sorts = new Dictionary<string, int>(library.Sorts, StringComparer.OrdinalIgnoreCase);
        }
        store.Names = new Dictionary<string, string>(store.Names, StringComparer.OrdinalIgnoreCase);
        store.Posters = new Dictionary<string, string>(store.Posters, StringComparer.OrdinalIgnoreCase);
        store.PosterMisses = store.PosterMisses
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Save(store);
        return store;
    }

    private static LibraryStore? Read()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<LibraryStore>(File.ReadAllText(FilePath), Options);
            }
        }
        catch
        {
        }
        return null;
    }

    private static LibraryStore? Migrate()
    {
        try
        {
            if (!File.Exists(LegacyPath))
            {
                return null;
            }
            var legacy = JsonSerializer.Deserialize<LegacyStore>(File.ReadAllText(LegacyPath), Options);
            if (legacy is null)
            {
                return null;
            }
            return new LibraryStore
            {
                Libraries =
                [
                    new VideoLibrary
                    {
                        Id = NewId(),
                        Name = LocalizationService.L("主页"),
                        Folders = legacy.Folders,
                        History = legacy.History
                    }
                ]
            };
        }
        catch
        {
            return null;
        }
    }

    private static void Save(LibraryStore store)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(store, Options));
        }
        catch
        {
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    public VideoLibrary? Find(string? id) =>
        id is null ? null : _store.Libraries.FirstOrDefault(l => l.Id == id);

    public VideoLibrary Open(string? id)
    {
        var library = Find(id) ?? Home;
        if (!ReferenceEquals(library, Active))
        {
            Active = library;
            _view.Clear();
            CurrentFolder = null;
            CurrentCollection = null;
            _entered = false;
        }
        return Active;
    }

    public void Enter(string id)
    {
        var library = Find(id) ?? Home;
        if (!ReferenceEquals(library, Active))
        {
            Active = library;
            _view.Clear();
        }
        CurrentFolder = null;
        CurrentCollection = null;
        _entered = true;
    }

    public bool OpenFolder(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }
        if (!Directory.Exists(path) || EntryRootOf(path) is null)
        {
            return false;
        }
        CurrentFolder = path;
        CurrentCollection = null;
        return true;
    }

    public bool OpenCollection(string id)
    {
        var collection = Active.Collections.FirstOrDefault(c => c.Id == id);
        if (collection is null)
        {
            return false;
        }
        CurrentCollection = collection;
        return true;
    }

    public bool GoUp()
    {
        if (CurrentCollection is not null)
        {
            CurrentCollection = null;
            return true;
        }
        if (CurrentFolder is null)
        {
            if (ReferenceEquals(Active, Home) && _entered)
            {
                _entered = false;
                return true;
            }
            return false;
        }
        var root = EntryRootOf(CurrentFolder);
        if (root is null || root.Equals(CurrentFolder, StringComparison.OrdinalIgnoreCase))
        {
            CurrentFolder = null;
            return true;
        }
        var parent = Path.GetDirectoryName(CurrentFolder);
        CurrentFolder = string.IsNullOrEmpty(parent) ? null : parent;
        return true;
    }

    public void ResetView()
    {
        CurrentFolder = null;
        CurrentCollection = null;
        _entered = false;
    }

    public int SortOf(string key) =>
        Active.Sorts.TryGetValue(key, out var value) ? value : 0;

    public void SetSort(string key, int value)
    {
        if (value <= 0)
        {
            if (Active.Sorts.Remove(key))
            {
                Save(_store);
            }
            return;
        }
        if (Active.Sorts.TryGetValue(key, out var current) && current == value)
        {
            return;
        }
        Active.Sorts[key] = value;
        Save(_store);
    }

    private string? EntryRootOf(string path) =>
        Active.Folders
            .Where(f => path.Equals(f, StringComparison.OrdinalIgnoreCase) || IsUnder(path, f))
            .OrderByDescending(f => f.Length)
            .FirstOrDefault();

    public VideoLibrary Create(string? name)
    {
        var library = new VideoLibrary { Id = NewId(), Name = Normalize(name) };
        _store.Libraries.Add(library);
        Save(_store);
        return library;
    }

    public void Rename(VideoLibrary library, string? name)
    {
        library.Name = Normalize(name);
        Save(_store);
    }

    public bool Delete(VideoLibrary library)
    {
        if (ReferenceEquals(library, Home) || !_store.Libraries.Remove(library))
        {
            return false;
        }
        if (ReferenceEquals(library, Active))
        {
            Active = Home;
            _view.Clear();
            CurrentFolder = null;
        }
        Save(_store);
        return true;
    }

    public string SuggestName()
    {
        var index = 1;
        while (_store.Libraries.Any(l => l.Name == LocalizationService.L("新建库 {0}", index)))
        {
            index++;
        }
        return LocalizationService.L("新建库 {0}", index);
    }

    private static string Normalize(string? name)
    {
        name = name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? LocalizationService.L("未命名库") : name;
    }

    public async Task AddFolderAsync(string folder)
    {
        folder = Path.GetFullPath(folder);
        if (!Active.Folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        {
            Active.Folders.Add(folder);
            Save(_store);
        }
        await ScanAsync();
    }

    public async Task RemoveFolderAsync(string folder)
    {
        if (Active.Folders.RemoveAll(f => f.Equals(folder, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            CurrentFolder = null;
            foreach (var key in Active.FolderItems.Keys
                .Where(k => k.Equals(folder, StringComparison.OrdinalIgnoreCase) || IsUnder(k, folder))
                .ToList())
            {
                Active.FolderItems.Remove(key);
            }
            Save(_store);
        }
        await ScanAsync();
    }

    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var added = false;
        foreach (var path in paths)
        {
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                continue;
            }
            if (!File.Exists(full) || Active.Files.Contains(full, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            Active.Files.Add(full);
            added = true;
        }
        if (added)
        {
            Save(_store);
        }
        await ScanAsync();
    }

    public async Task RemoveFileAsync(string path)
    {
        if (Active.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Save(_store);
        }
        await ScanAsync();
    }

    public string SuggestCollectionName()
    {
        var index = 1;
        while (Active.Collections.Any(c => c.Name == LocalizationService.L("新建文件夹 {0}", index)))
        {
            index++;
        }
        return LocalizationService.L("新建文件夹 {0}", index);
    }

    public async Task<VideoCollection> CreateCollectionAsync()
    {
        var collection = new VideoCollection
        {
            Id = NewId(),
            Name = SuggestCollectionName(),
            Parent = CurrentFolder ?? ""
        };
        Active.Collections.Add(collection);
        Save(_store);
        await ScanAsync();
        return collection;
    }

    public async Task CreateCollectionAsync(IEnumerable<string> paths)
    {
        var files = paths
            .Select(FullPath)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists)
            .ToList();
        if (files.Count < 2)
        {
            return;
        }
        Active.Collections.Add(new VideoCollection
        {
            Id = NewId(),
            Name = SuggestCollectionName(),
            Parent = CurrentFolder ?? "",
            Files = files
        });
        Save(_store);
        await ScanAsync();
    }

    public async Task AddToCollectionAsync(string id, IEnumerable<string> paths)
    {
        var collection = Active.Collections.FirstOrDefault(c => c.Id == id);
        if (collection is null)
        {
            return;
        }
        var changed = false;
        foreach (var path in paths.Select(FullPath).OfType<string>())
        {
            if (!File.Exists(path) || collection.Files.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (var other in Active.Collections.Where(c => c.Id != id))
            {
                other.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            foreach (var list in Active.FolderItems.Values)
            {
                list.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            Active.Hidden.RemoveAll(h => h.Equals(path, StringComparison.OrdinalIgnoreCase));
            collection.Files.Add(path);
            changed = true;
        }
        if (!changed)
        {
            return;
        }
        Save(_store);
        await ScanAsync();
    }

    public async Task RemoveFromCollectionAsync(IEnumerable<string> paths)
    {
        var collection = CurrentCollection;
        if (collection is null)
        {
            return;
        }
        var targets = new HashSet<string>(paths.Select(FullPath).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        if (collection.Files.RemoveAll(targets.Contains) == 0)
        {
            return;
        }
        Save(_store);
        await ScanAsync();
    }

    public void RenameCollection(VideoCollection collection, string? name)
    {
        collection.Name = Normalize(name);
        Save(_store);
    }

    private static string? CleanName(string? name)
    {
        name = name?.Trim();
        return string.IsNullOrEmpty(name) ? null : name;
    }

    public async Task<bool> RenamePathAsync(string path, string? name)
    {
        var full = FullPath(path);
        if (full is null)
        {
            return false;
        }
        name = CleanName(name);
        if (name is null || name == DefaultName(full))
        {
            _store.Names.Remove(full);
        }
        else
        {
            _store.Names[full] = name;
        }
        Save(_store);
        await ScanAsync();
        return true;
    }

    public string? CustomNameOf(string path)
    {
        var full = FullPath(path);
        return full is not null && _store.Names.TryGetValue(full, out var name) ? name : null;
    }

    public string DisplayNameOf(string path) =>
        CustomNameOf(path) ?? Path.GetFileNameWithoutExtension(path);

    private static string DefaultName(string path) =>
        Directory.Exists(path) ? FolderName(path) : Path.GetFileNameWithoutExtension(path);

    public async Task DissolveCollectionAsync(VideoCollection collection)
    {
        if (!Active.Collections.Remove(collection))
        {
            return;
        }
        Active.Sorts.Remove(CollectionKey(collection.Id));
        if (ReferenceEquals(CurrentCollection, collection))
        {
            CurrentCollection = null;
        }
        Save(_store);
        await ScanAsync();
    }

    public async Task AddToFolderAsync(string folder, IEnumerable<string> paths)
    {
        folder = FullPath(folder) ?? folder;
        var list = FolderMembers(Active, folder, create: true);
        var changed = false;
        foreach (var path in paths.Select(FullPath).OfType<string>())
        {
            if (!File.Exists(path) ||
                string.Equals(Path.GetDirectoryName(path), folder, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foreach (var other in Active.FolderItems.Values)
            {
                other.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            foreach (var collection in Active.Collections)
            {
                collection.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            Active.Hidden.RemoveAll(h => h.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (!list.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(path);
                changed = true;
            }
        }
        if (!changed)
        {
            return;
        }
        Save(_store);
        await ScanAsync();
    }

    public async Task<int> RemoveFromFolderAsync(string folder, IEnumerable<string> paths)
    {
        var targets = new HashSet<string>(paths.Select(FullPath).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        var removed = FolderMembers(Active, FullPath(folder) ?? folder).RemoveAll(targets.Contains);
        if (removed == 0)
        {
            return 0;
        }
        Save(_store);
        await ScanAsync();
        return removed;
    }

    public bool IsFolderMember(string folder, string path) =>
        FolderMembers(Active, FullPath(folder) ?? folder).Contains(path, StringComparer.OrdinalIgnoreCase);

    private static List<string> FolderMembers(VideoLibrary library, string folder, bool create = false)
    {
        foreach (var pair in library.FolderItems)
        {
            if (pair.Key.Equals(folder, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }
        if (!create)
        {
            return [];
        }
        var list = new List<string>();
        library.FolderItems[folder] = list;
        return list;
    }

    private static void MoveFolderItems(VideoLibrary source, VideoLibrary target, string folder)
    {
        foreach (var key in source.FolderItems.Keys
            .Where(k => k.Equals(folder, StringComparison.OrdinalIgnoreCase) || IsUnder(k, folder))
            .ToList())
        {
            var list = source.FolderItems[key];
            source.FolderItems.Remove(key);
            var targetList = FolderMembers(target, key, create: true);
            foreach (var path in list)
            {
                if (!targetList.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    targetList.Add(path);
                }
            }
        }
    }

    public async Task ScanAsync()
    {
        var library = Active;
        var folder = CurrentFolder;
        var current = CurrentCollection;
        var overview = IsOverview;
        var snapshots = overview ? _store.Libraries.Select(Snapshot).ToList() : [Snapshot(library)];
        var snapshot = snapshots.FirstOrDefault(s => s.Id == library.Id) ?? snapshots[0];
        var collection = current is null ? null : snapshot.Collections.FirstOrDefault(c => c.Id == current.Id);
        var resume = new Dictionary<string, double>(SettingsService.Current.ResumePositions, StringComparer.OrdinalIgnoreCase);
        var names = new Dictionary<string, string>(_store.Names, StringComparer.OrdinalIgnoreCase);

        var list = await Task.Run(() => overview
            ? BuildOverview(snapshots, resume, names)
            : BuildView(folder, collection, snapshot, resume, names));

        if (!ReferenceEquals(library, Active))
        {
            return;
        }
        _view.Clear();
        _view.AddRange(list);
    }

    private static VideoLibrary Snapshot(VideoLibrary library) => new()
    {
        Id = library.Id,
        Name = library.Name,
        Folders = [.. library.Folders],
        Files = [.. library.Files],
        Hidden = [.. library.Hidden],
        FolderItems = library.FolderItems.ToDictionary(pair => pair.Key, pair => new List<string>(pair.Value)),
        Collections = library.Collections.Select(c => new VideoCollection
        {
            Id = c.Id,
            Name = c.Name,
            Parent = c.Parent,
            Files = [.. c.Files]
        }).ToList(),
        History = new Dictionary<string, HistoryRecord>(library.History, StringComparer.OrdinalIgnoreCase),
        Daily = new Dictionary<string, int>(library.Daily, StringComparer.Ordinal)
    };

    public async Task<bool> DeleteEntryAsync(LibraryEntry entry)
    {
        if (entry.IsFolder)
        {
            await RemoveFolderAsync(entry.Path);
            return true;
        }
        var path = entry.Path;
        if (!await Task.Run(() => ShellDelete.Run(path)) && File.Exists(path))
        {
            return false;
        }
        foreach (var library in _store.Libraries)
        {
            library.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            library.Hidden.RemoveAll(h => h.Equals(path, StringComparison.OrdinalIgnoreCase));
            foreach (var collection in library.Collections)
            {
                collection.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            foreach (var list in library.FolderItems.Values)
            {
                list.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            library.History.Remove(path);
        }
        SettingsService.Current.ResumePositions.Remove(path);
        SettingsService.Save();
        _store.Names.Remove(path);
        _store.Posters.Remove(path);
        _store.PosterMisses.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
        DeletePosterCache(path);
        Save(_store);
        _view.Remove(entry);
        return true;
    }

    public async Task TransferAsync(LibraryEntry entry, VideoLibrary target, bool move)
    {
        var source = Active;
        if (ReferenceEquals(source, target))
        {
            return;
        }
        var path = entry.Path;
        if (entry.IsFolder)
        {
            if (!target.Folders.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                target.Folders.Add(path);
            }
            if (move &&
                source.Folders.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                if (CurrentFolder is not null &&
                    (CurrentFolder.Equals(path, StringComparison.OrdinalIgnoreCase) || IsUnder(CurrentFolder, path)))
                {
                    CurrentFolder = null;
                }
                MoveFolderItems(source, target, path);
            }
        }
        else
        {
            if (!target.Files.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                target.Files.Add(path);
            }
            target.Hidden.RemoveAll(h => h.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (move)
            {
                if (source.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase)) == 0 &&
                    !source.Hidden.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    source.Hidden.Add(path);
                }
                foreach (var collection in source.Collections)
                {
                    collection.Files.RemoveAll(f => f.Equals(path, StringComparison.OrdinalIgnoreCase));
                }
                foreach (var list in source.FolderItems.Values)
                {
                    list.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
                }
                _view.Remove(entry);
            }
        }
        Save(_store);
        if (move && entry.IsFolder)
        {
            await ScanAsync();
        }
    }

    public IReadOnlyList<VideoLibrary> TransferTargets =>
        _store.Libraries.Where(l => !ReferenceEquals(l, Active) && !ReferenceEquals(l, Home)).ToList();

    private static void DeletePosterCache(string sourcePath)
    {
        try
        {
            var cache = Path.Combine(ThumbDir, Hash($"{PosterWidth}x{PosterHeight}|{sourcePath}") + ".jpg");
            if (File.Exists(cache))
            {
                File.Delete(cache);
            }
            var fit = Path.Combine(ThumbDir, Hash($"fit{PosterWidth}x{PosterHeight}|{sourcePath}") + ".jpg");
            if (File.Exists(fit))
            {
                File.Delete(fit);
            }
        }
        catch
        {
        }
    }

    private static List<LibraryEntry> BuildOverview(
        IReadOnlyList<VideoLibrary> libraries,
        Dictionary<string, double> resume,
        IReadOnlyDictionary<string, string> names)
    {
        var items = new List<LibraryEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scanned = libraries
            .Select(library => (Library: library, Videos: AllVideos(library)))
            .ToList();
        foreach (var (library, videos) in scanned.Skip(1))
        {
            items.Add(new LibraryEntry
            {
                Id = library.Id,
                Name = library.Name,
                Kind = LibraryEntryKind.Library,
                ItemCount = videos.Count,
                FolderCount = library.Folders.Count,
                PosterSourcePath = videos.FirstOrDefault()
            });
        }
        foreach (var (library, videos) in scanned)
        {
            foreach (var path in videos)
            {
                if (seen.Add(path))
                {
                    items.Add(MakeEntry(path, library.History, resume, names));
                }
            }
        }
        return items;
    }

    private static List<string> AllVideos(VideoLibrary library)
    {
        var videos = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hidden = new HashSet<string>(
            library.Hidden.Select(FullPath).OfType<string>(),
            StringComparer.OrdinalIgnoreCase);
        foreach (var path in library.Files)
        {
            if (FullPath(path) is not { } full ||
                hidden.Contains(full) ||
                !File.Exists(full) ||
                !seen.Add(full))
            {
                continue;
            }
            videos.Add(full);
        }
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true
        };
        foreach (var folder in library.Folders)
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }
            try
            {
                foreach (var path in Directory.EnumerateFiles(folder, "*", options))
                {
                    if (!PlaybackService.VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()) ||
                        hidden.Contains(path) ||
                        !seen.Add(path))
                    {
                        continue;
                    }
                    videos.Add(path);
                }
            }
            catch
            {
            }
        }
        return videos;
    }

    private static List<LibraryEntry> BuildView(
        string? folder,
        VideoCollection? collection,
        VideoLibrary library,
        Dictionary<string, double> resume,
        IReadOnlyDictionary<string, string> names)
    {
        var items = new List<LibraryEntry>();
        var hidden = new HashSet<string>(
            library.Hidden.Select(FullPath).OfType<string>(),
            StringComparer.OrdinalIgnoreCase);
        var organized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in library.Collections)
        {
            organized.UnionWith(group.Files.Select(FullPath).OfType<string>());
        }
        foreach (var pair in library.FolderItems)
        {
            if (folder is null || !pair.Key.Equals(folder, StringComparison.OrdinalIgnoreCase))
            {
                organized.UnionWith(pair.Value.Select(FullPath).OfType<string>());
            }
        }
        try
        {
            if (collection is not null)
            {
                foreach (var path in collection.Files)
                {
                    if (File.Exists(path))
                    {
                        items.Add(MakeEntry(Path.GetFullPath(path), library.History, resume, names));
                    }
                }
                return items;
            }

            if (folder is null)
            {
                foreach (var path in library.Folders)
                {
                    if (Directory.Exists(path))
                    {
                        items.Add(MakeFolderEntry(path, names));
                    }
                }
                foreach (var group in library.Collections)
                {
                    if (group.Parent.Length == 0)
                    {
                        items.Add(MakeCollectionEntry(group));
                    }
                }
                foreach (var path in library.Files)
                {
                    if (File.Exists(path) && !hidden.Contains(path) && !organized.Contains(path))
                    {
                        items.Add(MakeEntry(Path.GetFullPath(path), library.History, resume, names));
                    }
                }
                return items;
            }

            if (!Directory.Exists(folder))
            {
                return items;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            };
            foreach (var path in Directory.EnumerateDirectories(folder, "*", options))
            {
                items.Add(MakeFolderEntry(path, names));
            }
            foreach (var group in library.Collections)
            {
                if (group.Parent.Equals(folder, StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(MakeCollectionEntry(group));
                }
            }
            foreach (var path in Directory.EnumerateFiles(folder, "*", options))
            {
                if (PlaybackService.VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()) &&
                    !hidden.Contains(path) &&
                    !organized.Contains(path) &&
                    seen.Add(path))
                {
                    items.Add(MakeEntry(Path.GetFullPath(path), library.History, resume, names));
                }
            }
            foreach (var path in FolderMembers(library, folder))
            {
                if (FullPath(path) is not { } full ||
                    !File.Exists(full) ||
                    hidden.Contains(full) ||
                    !seen.Add(full))
                {
                    continue;
                }
                items.Add(MakeEntry(full, library.History, resume, names));
            }
        }
        catch
        {
        }
        return items;
    }

    private static string? FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    private static LibraryEntry MakeFolderEntry(string path, IReadOnlyDictionary<string, string> names) => new()
    {
        Path = path,
        Name = names.TryGetValue(path, out var name) ? name : FolderName(path),
        Kind = LibraryEntryKind.Folder
    };

    private static LibraryEntry MakeCollectionEntry(VideoCollection collection) => new()
    {
        Id = collection.Id,
        Path = collection.Parent,
        Name = collection.Name,
        Kind = LibraryEntryKind.Collection,
        ItemCount = collection.Files.Count,
        PosterSourcePath = collection.Files.FirstOrDefault(File.Exists)
    };

    private static string FolderName(string path)
    {
        var name = new DirectoryInfo(path).Name;
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private static LibraryEntry MakeEntry(
        string path,
        Dictionary<string, HistoryRecord> history,
        Dictionary<string, double> resume,
        IReadOnlyDictionary<string, string> names)
    {
        history.TryGetValue(path, out var record);
        long size = 0;
        try
        {
            size = new FileInfo(path).Length;
        }
        catch
        {
        }

        var ratio = 0.0;
        if (record is not null && record.DurationSeconds > 1 && resume.TryGetValue(path, out var position))
        {
            ratio = Math.Clamp(position / record.DurationSeconds, 0, 1);
        }

        return new LibraryEntry
        {
            Path = path,
            Name = names.TryGetValue(path, out var alias) ? alias : Path.GetFileNameWithoutExtension(path),
            Kind = LibraryEntryKind.Video,
            SizeBytes = size,
            LastPlayed = record?.LastPlayed ?? DateTime.MinValue,
            PlayCount = record?.PlayCount ?? 0,
            DurationSeconds = record?.DurationSeconds ?? 0,
            ProgressRatio = ratio
        };
    }

    public void RecordPlayed(string path, double durationSeconds)
    {
        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            return;
        }
        var library = OwnerOf(path) ?? Home;
        if (!library.History.TryGetValue(path, out var record))
        {
            record = new HistoryRecord();
            library.History[path] = record;
        }
        record.LastPlayed = DateTime.Now;
        record.PlayCount++;
        var day = DateTime.Now.ToString("yyyy-MM-dd");
        library.Daily[day] = library.Daily.GetValueOrDefault(day) + 1;
        if (durationSeconds > 1)
        {
            record.DurationSeconds = durationSeconds;
        }
        Save(_store);
    }

    private VideoLibrary? OwnerOf(string path) =>
        _store.Libraries.FirstOrDefault(l =>
            !l.Hidden.Contains(path, StringComparer.OrdinalIgnoreCase) &&
            (l.Files.Contains(path, StringComparer.OrdinalIgnoreCase) || l.Folders.Any(f => IsUnder(path, f))));

    private static bool IsUnder(string path, string folder)
    {
        var prefix = folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] PosterNames =
    [
        "poster.jpg", "poster.png", "folder.jpg", "folder.png", "cover.jpg", "cover.png"
    ];

    public static bool FolderHasPoster(string folder) =>
        PosterNames.Any(name => File.Exists(Path.Combine(folder, name)));

    public bool HasPosterOverride(string path) =>
        _store.Posters.TryGetValue(path, out var file) && File.Exists(file);

    public void SetPosterOverride(string path, string posterFile)
    {
        var old = _store.Posters.TryGetValue(path, out var previous) ? previous : null;
        _store.Posters[path] = posterFile;
        _store.PosterMisses.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));
        Save(_store);
        if (old is not null && !old.Equals(posterFile, StringComparison.OrdinalIgnoreCase))
        {
            DeletePosterCache(old);
            ReleasePosterFile(old);
        }
    }

    public void ClearPosterOverride(string path)
    {
        if (!_store.Posters.TryGetValue(path, out var file))
        {
            return;
        }
        _store.Posters.Remove(path);
        Save(_store);
        DeletePosterCache(file);
        ReleasePosterFile(file);
    }

    public void ResetPosterSearch(string path)
    {
        ClearPosterOverride(path);
        if (_store.PosterMisses.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Save(_store);
        }
    }

    private void ReleasePosterFile(string file)
    {
        if (_store.Posters.Values.Any(v => v.Equals(file, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        try
        {
            File.Delete(file);
        }
        catch
        {
        }
    }

    public bool IsPosterMissed(string path) =>
        _store.PosterMisses.Any(p => p.Equals(path, StringComparison.OrdinalIgnoreCase));

    public void RecordPosterMiss(string path)
    {
        if (_store.PosterMisses.Any(p => p.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        _store.PosterMisses.Add(path);
        Save(_store);
    }

    public static async Task<string?> GetPosterPathAsync(LibraryEntry entry) =>
        entry switch
        {
            { IsFolder: true } => await PosterOverrideAsync(entry.Path) ?? await GetFolderPosterPathAsync(entry.Path),
            { PosterSourcePath: { } source } => await PosterOverrideAsync(source) ?? await GetPosterAsync(source),
            _ => await PosterOverrideAsync(entry.Path) ?? await GetPosterAsync(entry.Path)
        };

    private static async Task<string?> PosterOverrideAsync(string path)
    {
        if (!Instance._store.Posters.TryGetValue(path, out var file) || !File.Exists(file))
        {
            return null;
        }
        return await GetPosterFitAsync(file) ?? file;
    }

    public static async Task<string?> GetFolderPosterPathAsync(string folder)
    {
        try
        {
            string? poster = null;
            string? video = null;
            await Task.Run(() =>
            {
                foreach (var name in PosterNames)
                {
                    var candidate = Path.Combine(folder, name);
                    if (File.Exists(candidate))
                    {
                        poster = candidate;
                        return;
                    }
                }
                video = FindFirstVideo(folder);
            });
            poster ??= video;
            return poster is null ? null : await GetPosterAsync(poster);
        }
        catch
        {
            return null;
        }
    }

    private static string? FindFirstVideo(string folder)
    {
        try
        {
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };
            foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*", options))
            {
                if (PlaybackService.VideoExtensions.Contains(file.Extension.ToLowerInvariant()))
                {
                    return file.FullName;
                }
            }
        }
        catch
        {
        }
        return null;
    }

    private static async Task<string?> GetPosterAsync(string sourcePath)
    {
        try
        {
            var cache = Path.Combine(ThumbDir, Hash($"{PosterWidth}x{PosterHeight}|{sourcePath}") + ".jpg");
            if (File.Exists(cache))
            {
                return cache;
            }
            var file = await StorageFile.GetFileFromPathAsync(sourcePath);
            using var source = await OpenSourceAsync(file);
            if (source is null || source.Size == 0)
            {
                return null;
            }
            var decoder = await BitmapDecoder.CreateAsync(source);
            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;
            if (width == 0 || height == 0)
            {
                return null;
            }
            var transform = CenterCrop(width, height);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);
            using var memory = new InMemoryRandomAccessStream();
            var properties = new BitmapPropertySet
            {
                { "ImageQuality", new BitmapTypedValue(PosterQuality / 100f, PropertyType.Single) }
            };
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memory, properties);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync();

            var bytes = new byte[memory.Size];
            using (var reader = new DataReader(memory.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)memory.Size);
                reader.ReadBytes(bytes);
            }
            Directory.CreateDirectory(ThumbDir);
            await File.WriteAllBytesAsync(cache, bytes);
            return cache;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> GetPosterFitAsync(string sourcePath)
    {
        var cache = Path.Combine(ThumbDir, Hash($"fit{PosterWidth}x{PosterHeight}|{sourcePath}") + ".jpg");
        if (File.Exists(cache))
        {
            return cache;
        }
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(sourcePath);
            using var source = await OpenSourceAsync(file);
            if (source is null || source.Size == 0)
            {
                return null;
            }
            var decoder = await BitmapDecoder.CreateAsync(source);
            var width = decoder.PixelWidth;
            var height = decoder.PixelHeight;
            if (width == 0 || height == 0)
            {
                return null;
            }
            var target = PosterWidth / (double)PosterHeight;
            if (Math.Abs(width / (double)height - target) <= 0.03)
            {
                return await GetPosterAsync(sourcePath);
            }
            var fitScale = Math.Min(PosterWidth / (double)width, PosterHeight / (double)height);
            var fitWidth = (uint)Math.Max(1, (uint)Math.Round(width * fitScale));
            var fitHeight = (uint)Math.Max(1, (uint)Math.Round(height * fitScale));
            using var foreground = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                new BitmapTransform
                {
                    ScaledWidth = fitWidth,
                    ScaledHeight = fitHeight,
                    InterpolationMode = BitmapInterpolationMode.Fant
                },
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);
            using var small = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                new BitmapTransform
                {
                    Bounds = CenterCrop(width, height).Bounds,
                    ScaledWidth = 24,
                    ScaledHeight = 36,
                    InterpolationMode = BitmapInterpolationMode.Fant
                },
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.ColorManageToSRgb);
            using var background = await ResampleAsync(small, PosterWidth, PosterHeight);
            var composed = await ComposeAsync(background, foreground, fitWidth, fitHeight);
            using var memory = new InMemoryRandomAccessStream();
            var properties = new BitmapPropertySet
            {
                { "ImageQuality", new BitmapTypedValue(PosterQuality / 100f, PropertyType.Single) }
            };
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, memory, properties);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                PosterWidth,
                PosterHeight,
                96,
                96,
                composed);
            await encoder.FlushAsync();
            var bytes = new byte[memory.Size];
            using (var reader = new DataReader(memory.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)memory.Size);
                reader.ReadBytes(bytes);
            }
            Directory.CreateDirectory(ThumbDir);
            await File.WriteAllBytesAsync(cache, bytes);
            return cache;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<SoftwareBitmap> ResampleAsync(SoftwareBitmap source, uint width, uint height)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(source);
        await encoder.FlushAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height,
                InterpolationMode = BitmapInterpolationMode.Fant
            },
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.ColorManageToSRgb);
    }

    private static async Task<byte[]> PixelsAsync(SoftwareBitmap bitmap)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var data = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.ColorManageToSRgb);
        return data.DetachPixelData();
    }

    private static async Task<byte[]> ComposeAsync(SoftwareBitmap background, SoftwareBitmap foreground, uint fitWidth, uint fitHeight)
    {
        var backdrop = await PixelsAsync(background);
        var overlay = await PixelsAsync(foreground);
        var offsetX = (int)((PosterWidth - fitWidth) / 2);
        var offsetY = (int)((PosterHeight - fitHeight) / 2);
        var stride = (int)PosterWidth * 4;
        for (var y = 0; y < (int)fitHeight; y++)
        {
            var source = y * (int)fitWidth * 4;
            var target = (y + offsetY) * stride + offsetX * 4;
            Array.Copy(overlay, source, backdrop, target, (int)fitWidth * 4);
        }
        return backdrop;
    }

    private static async Task<IRandomAccessStream?> OpenSourceAsync(StorageFile file)
    {
        if (PlaybackService.VideoExtensions.Contains(file.FileType.ToLowerInvariant()))
        {
            return await file.GetThumbnailAsync(ThumbnailMode.VideosView, 720);
        }
        var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 720);
        if (thumbnail.Size > 0)
        {
            return thumbnail;
        }
        thumbnail.Dispose();
        return await file.OpenReadAsync();
    }

    private static BitmapTransform CenterCrop(uint width, uint height)
    {
        var inset = Math.Max(2u, (uint)Math.Round(Math.Min(width, height) * 0.008));
        var usableWidth = Math.Max(1u, width - inset * 2);
        var usableHeight = Math.Max(1u, height - inset * 2);
        var target = PosterWidth / (double)PosterHeight;
        double cropWidth = usableWidth;
        double cropHeight = usableHeight;
        if (usableWidth / (double)usableHeight > target)
        {
            cropWidth = usableHeight * target;
        }
        else
        {
            cropHeight = usableWidth / target;
        }
        return new BitmapTransform
        {
            Bounds = new BitmapBounds
            {
                X = inset + (uint)Math.Round((usableWidth - cropWidth) / 2),
                Y = inset + (uint)Math.Round((usableHeight - cropHeight) / 2),
                Width = Math.Max(1u, (uint)Math.Round(cropWidth)),
                Height = Math.Max(1u, (uint)Math.Round(cropHeight))
            },
            InterpolationMode = BitmapInterpolationMode.Fant
        };
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];

    public static void ClearThumbnailCache()
    {
        try
        {
            if (Directory.Exists(ThumbDir))
            {
                Directory.Delete(ThumbDir, true);
            }
        }
        catch
        {
        }
    }

    public static long ThumbnailCacheSize()
    {
        try
        {
            if (!Directory.Exists(ThumbDir))
            {
                return 0;
            }
            return Directory.EnumerateFiles(ThumbDir, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }
}
