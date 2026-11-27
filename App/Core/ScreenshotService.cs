using FFmpegInteropX;
using Windows.Storage.Streams;

namespace FilmField.Services;

public static class ScreenshotService
{
    public static string DefaultDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FilmField");

    public static string TargetDir
    {
        get
        {
            var dir = SettingsService.Current.ScreenshotDir;
            return string.IsNullOrWhiteSpace(dir) ? DefaultDir : dir;
        }
    }

    public static async Task<byte[]?> RenderFrameAsync(string videoPath, TimeSpan position)
    {
        try
        {
            var grabber = await FrameGrabber.CreateFromFileAsync(videoPath);
            try
            {
                var frame = await grabber.ExtractVideoFrameAsync(position, true);
                if (frame is null)
                {
                    return null;
                }
                using var stream = new InMemoryRandomAccessStream();
                await frame.EncodeAsPngAsync(stream);
                return await ReadStreamAsync(stream);
            }
            finally
            {
                grabber.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> SaveAsync(string videoPath, byte[] png)
    {
        try
        {
            Directory.CreateDirectory(TargetDir);
            var path = BuildPath(videoPath);
            await File.WriteAllBytesAsync(path, png);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<byte[]> ReadStreamAsync(IRandomAccessStream stream)
    {
        stream.Seek(0);
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(bytes);
        return bytes;
    }

    private static string BuildPath(string videoPath)
    {
        var name = Path.GetFileNameWithoutExtension(videoPath);
        var stamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
        var path = Path.Combine(TargetDir, $"{name} {stamp}.png");
        for (var i = 2; File.Exists(path); i++)
        {
            path = Path.Combine(TargetDir, $"{name} {stamp} ({i}).png");
        }
        return path;
    }
}
