using FFmpegInteropX;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace FilmField.Services;

public sealed class PlaybackService : IDisposable
{
    public static PlaybackService Instance { get; } = new();

    public static readonly string[] VideoExtensions =
    [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".m4v",
        ".flv", ".ts", ".mpg", ".mpeg", ".3gp"
    ];

    public MediaPlayer Player { get; }

    public event Action<MediaPlaybackState>? StateChanged;
    public event Action? MediaOpened;
    public event Action? MediaEnded;
    public event Action<string>? MediaFailed;

    private FFmpegMediaSource? _ffmpegSource;
    private MediaPlaybackItem? _currentItem;
    private bool _playRequested;
    private int _resumePlayAttempts;

    private PlaybackService()
    {
        Player = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Media,
            IsLoopingEnabled = false
        };

        Player.PlaybackSession.PlaybackStateChanged += (_, _) =>
        {
            var state = Player.PlaybackSession.PlaybackState;
            if (state == MediaPlaybackState.Playing)
            {
                _playRequested = true;
            }
            else if (state == MediaPlaybackState.Paused && _playRequested && _resumePlayAttempts < 2)
            {
                _resumePlayAttempts++;
                Player.Play();
            }
            StateChanged?.Invoke(state);
        };
        Player.MediaOpened += (_, _) =>
        {
            _resumePlayAttempts = 0;
            if (_playRequested)
            {
                Player.Play();
            }
            MediaOpened?.Invoke();
        };
        Player.MediaEnded += (_, _) => MediaEnded?.Invoke();
        Player.MediaFailed += (_, e) => MediaFailed?.Invoke(e.ErrorMessage);
    }

    public MediaPlaybackSession Session => Player.PlaybackSession;

    public MediaPlaybackItem? CurrentItem => _currentItem;

    public TimeSpan Position => Session.Position;
    public TimeSpan Duration => Session.NaturalDuration;

    public double Volume
    {
        get => Player.Volume;
        set => Player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    public bool IsMuted
    {
        get => Player.IsMuted;
        set => Player.IsMuted = value;
    }

    public double Rate
    {
        get => Session.PlaybackRate;
        set => Session.PlaybackRate = Math.Clamp(value, 0.25, 3.0);
    }

    public async Task<MediaPlaybackItem?> CreatePlaybackItemAsync(StorageFile file)
    {
        if (SettingsService.Current.UseFfmpeg)
        {
            try
            {
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var config = new MediaSourceConfig();
                config.Video.VideoDecoderMode = VideoDecoderMode.Automatic;
                _ffmpegSource = await FFmpegMediaSource.CreateFromStreamAsync(stream, config);
                return _ffmpegSource.CreateMediaPlaybackItem();
            }
            catch
            {
                _ffmpegSource = null;
            }
        }
        try
        {
            var source = MediaSource.CreateFromStorageFile(file);
            return new MediaPlaybackItem(source);
        }
        catch
        {
            return null;
        }
    }

    public void Load(MediaPlaybackItem item)
    {
        _currentItem = item;
        Player.Source = item;
    }

    public void StepFrame()
    {
        if (CurrentItem is null || Session.PlaybackState != MediaPlaybackState.Paused)
        {
            return;
        }
        try
        {
            Player.StepForwardOneFrame();
        }
        catch
        {
        }
    }

    public bool IsPlaying => _playRequested;

    public void Play()
    {
        _playRequested = true;
        _resumePlayAttempts = 0;
        Player.Play();
    }

    public void Pause()
    {
        _playRequested = false;
        _resumePlayAttempts = 0;
        Player.Pause();
    }

    public void Toggle()
    {
        if (_playRequested)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public void Seek(TimeSpan target)
    {
        if (target < TimeSpan.Zero)
        {
            target = TimeSpan.Zero;
        }
        if (Duration > TimeSpan.Zero && target > Duration)
        {
            target = Duration;
        }
        Session.Position = target;
    }

    public void Skip(double seconds)
    {
        Seek(Position + TimeSpan.FromSeconds(seconds));
    }

    public void Dispose() => Player.Dispose();
}
