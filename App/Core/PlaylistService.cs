using Windows.Storage;

namespace FilmField.Services;

public enum PlaylistRepeatMode
{
    Off,
    All,
    One
}

public sealed class PlaylistService
{
    public static PlaylistService Instance { get; } = new();

    private readonly List<StorageFile> _items = [];
    private readonly Random _random = new();

    public event Action? QueueChanged;
    public event Action<int>? CurrentIndexChanged;

    public IReadOnlyList<StorageFile> Items => _items;
    public int Count => _items.Count;
    public int CurrentIndex { get; private set; } = -1;
    public PlaylistRepeatMode Repeat { get; set; } = PlaylistRepeatMode.Off;
    public bool IsShuffle { get; set; }

    public StorageFile? Current =>
        CurrentIndex >= 0 && CurrentIndex < _items.Count ? _items[CurrentIndex] : null;

    public int IndexOf(StorageFile file) => _items.IndexOf(file);

    public void SetQueue(IEnumerable<StorageFile> files, int startIndex = 0)
    {
        _items.Clear();
        _items.AddRange(files);
        CurrentIndex = _items.Count > 0 ? Math.Clamp(startIndex, 0, _items.Count - 1) : -1;
        QueueChanged?.Invoke();
    }

    public bool MoveNext()
    {
        if (_items.Count == 0)
        {
            return false;
        }
        if (IsShuffle && _items.Count > 1)
        {
            var picked = CurrentIndex;
            while (picked == CurrentIndex)
            {
                picked = _random.Next(_items.Count);
            }
            CurrentIndex = picked;
            CurrentIndexChanged?.Invoke(CurrentIndex);
            return true;
        }
        if (CurrentIndex + 1 >= _items.Count)
        {
            if (Repeat != PlaylistRepeatMode.All)
            {
                return false;
            }
            CurrentIndex = 0;
            CurrentIndexChanged?.Invoke(CurrentIndex);
            return true;
        }
        CurrentIndex++;
        CurrentIndexChanged?.Invoke(CurrentIndex);
        return true;
    }

    public bool MovePrevious()
    {
        if (CurrentIndex <= 0)
        {
            return false;
        }
        CurrentIndex--;
        CurrentIndexChanged?.Invoke(CurrentIndex);
        return true;
    }

    public void JumpTo(int index)
    {
        if (index < 0 || index >= _items.Count || index == CurrentIndex)
        {
            return;
        }
        CurrentIndex = index;
        CurrentIndexChanged?.Invoke(CurrentIndex);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return;
        }
        _items.RemoveAt(index);
        if (CurrentIndex >= _items.Count)
        {
            CurrentIndex = _items.Count - 1;
        }
        else if (index < CurrentIndex)
        {
            CurrentIndex--;
        }
        QueueChanged?.Invoke();
    }
}
