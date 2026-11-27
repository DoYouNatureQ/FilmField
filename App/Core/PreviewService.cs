using FFmpegInteropX;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace FilmField.Services;

public static class PreviewService
{
    public const int FrameCount = 24;
    public const int FrameWidth = 320;
    public const int FrameHeight = 180;

    private const string Version = "v5";

    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FilmField");

    private static readonly string Dir = Path.Combine(Root, "preview");

    private static readonly string LogPath = Path.Combine(Root, "preview.log");

    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static string[] GetFramePaths(string videoPath)
    {
        var dir = Path.Combine(Dir, Version, Hash(videoPath));
        var frames = new string[FrameCount];
        for (var i = 0; i < FrameCount; i++)
        {
            frames[i] = Path.Combine(dir, $"{i}.jpg");
        }
        return frames;
    }

    public static async Task EnsureFramesAsync(string videoPath, double durationSeconds, CancellationToken token)
    {
        if (durationSeconds <= 0 || !File.Exists(videoPath))
        {
            return;
        }

        var frames = GetFramePaths(videoPath);
        var dir = Path.GetDirectoryName(frames[0])!;
        var marker = Path.Combine(dir, "done");

        try
        {
            await Gate.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (!File.Exists(marker))
            {
                await GenerateAsync(videoPath, durationSeconds, frames, dir, marker, token);
            }
        }
        catch (OperationCanceledException)
        {
            Log("generate canceled");
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task GenerateAsync(
        string videoPath, double durationSeconds, string[] frames, string dir, string marker, CancellationToken token)
    {
        Log($"generate begin {Path.GetFileName(videoPath)} {durationSeconds:0.0}s");
        var failed = 0;
        try
        {
            Directory.CreateDirectory(dir);
            var grabber = await FrameGrabber.CreateFromFileAsync(videoPath).AsTask();
            try
            {
                for (var i = 0; i < FrameCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (File.Exists(frames[i]))
                    {
                        continue;
                    }
                    try
                    {
                        var position = TimeSpan.FromSeconds(durationSeconds * (i + 0.5) / FrameCount);
                        var frame = await grabber.ExtractVideoFrameAsync(position, true).AsTask();
                        token.ThrowIfCancellationRequested();
                        if (frame is null)
                        {
                            failed++;
                            Log($"frame {i} empty @{position.TotalSeconds:0.0}");
                            continue;
                        }
                        await SaveThumbnailAsync(frame, frames[i], token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Log($"frame {i} failed: {ex.GetType().Name} {ex.Message}");
                    }
                }
            }
            finally
            {
                grabber.Dispose();
            }

            if (failed == 0 && frames.All(File.Exists))
            {
                File.WriteAllText(marker, durationSeconds.ToString("0.0"));
                Log("generate done");
            }
            else
            {
                Log($"generate incomplete failed={failed}");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log($"generate aborted: {ex.GetType().Name} {ex.Message}");
        }
    }

    public static void ClearCache()
    {
        try
        {
            if (Directory.Exists(Dir))
            {
                Directory.Delete(Dir, true);
            }
        }
        catch
        {
        }
    }

    public static long CacheSize()
    {
        try
        {
            if (!Directory.Exists(Dir))
            {
                return 0;
            }
            return Directory.EnumerateFiles(Dir, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }

    public static void Log(string message)
    {
        try
        {
            var info = new FileInfo(LogPath);
            if (info.Exists && info.Length > 256 * 1024)
            {
                File.Delete(LogPath);
            }
            File.AppendAllText(LogPath, $"{DateTime.Now:MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    public static async Task<FrameGrabber> OpenGrabberAsync(string videoPath)
    {
        return await FrameGrabber.CreateFromFileAsync(videoPath).AsTask();
    }

    public static async Task<byte[]> ExtractExactFrameAsync(FrameGrabber grabber, double seconds, CancellationToken token)
    {
        using var frame = await grabber.ExtractVideoFrameAsync(TimeSpan.FromSeconds(seconds), true).AsTask();
        if (frame is null)
        {
            throw new InvalidOperationException("empty frame");
        }
        token.ThrowIfCancellationRequested();
        return await EncodeJpegAsync(frame, token);
    }

    private static async Task SaveThumbnailAsync(VideoFrame frame, string path, CancellationToken token)
    {
        var bytes = await EncodeJpegAsync(frame, token);
        await File.WriteAllBytesAsync(path, bytes, token);
    }

    private static async Task<byte[]> EncodeJpegAsync(VideoFrame frame, CancellationToken token)
    {
        using var raw = new InMemoryRandomAccessStream();
        await frame.EncodeAsJpegAsync(raw).AsTask(token);
        raw.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(raw);
        var scale = Math.Min(
            (double)FrameWidth / decoder.PixelWidth,
            (double)FrameHeight / decoder.PixelHeight);
        var width = Math.Max(1u, (uint)Math.Round(decoder.PixelWidth * scale));
        var height = Math.Max(1u, (uint)Math.Round(decoder.PixelHeight * scale));

        var transform = new BitmapTransform
        {
            ScaledWidth = width,
            ScaledHeight = height,
            InterpolationMode = BitmapInterpolationMode.Linear
        };
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform,
            ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);

        using var output = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();

        var bytes = new byte[output.Size];
        using var reader = new DataReader(output);
        await reader.LoadAsync((uint)output.Size).AsTask(token);
        reader.ReadBytes(bytes);
        return bytes;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}
