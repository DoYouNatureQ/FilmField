using System.Text;
using System.Text.RegularExpressions;

namespace FilmField.Services;

public sealed record PgsImage(
    int VideoWidth,
    int VideoHeight,
    int OffsetX,
    int OffsetY,
    int Width,
    int Height,
    byte[] Indices,
    byte[][] Palette);

public sealed record SubtitleCue(TimeSpan Start, TimeSpan End, string Text, PgsImage? Image = null);

public static class SubtitleService
{
    public static readonly string[] SubtitleExtensions = [".srt", ".ass", ".ssa", ".vtt", ".sup"];

    public static string? FindMatchedPath(string videoPath)
    {
        try
        {
            foreach (var ext in SubtitleExtensions)
            {
                var subtitlePath = Path.ChangeExtension(videoPath, ext);
                if (File.Exists(subtitlePath))
                {
                    return subtitlePath;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static List<SubtitleCue>? TryLoadMatched(string videoPath)
    {
        try
        {
            foreach (var ext in SubtitleExtensions)
            {
                var subtitlePath = Path.ChangeExtension(videoPath, ext);
                if (!File.Exists(subtitlePath))
                {
                    continue;
                }
                var cues = LoadFromFile(subtitlePath);
                if (cues.Count > 0)
                {
                    return cues;
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static List<SubtitleCue> LoadFromFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() == ".sup"
            ? ParseSup(File.ReadAllBytes(path))
            : Parse(ReadText(path));
    }

    public static List<SubtitleCue> Parse(string content)
    {
        var cues = content.Contains("[Events]", StringComparison.OrdinalIgnoreCase)
            ? ParseAss(content)
            : ParseSrt(content);
        cues.Sort((a, b) => a.Start.CompareTo(b.Start));
        return cues;
    }

    private static List<SubtitleCue> ParseSrt(string content)
    {
        var cues = new List<SubtitleCue>();
        foreach (var block in Regex.Split(content.Replace("\r\n", "\n"), "\n\n+"))
        {
            var cue = ParseBlock(block);
            if (cue is not null)
            {
                cues.Add(cue);
            }
        }
        return cues;
    }

    private static List<SubtitleCue> ParseAss(string content)
    {
        var cues = new List<SubtitleCue>();
        var fields = new[] { "Layer", "Start", "End", "Style", "Name", "MarginL", "MarginR", "MarginV", "Effect", "Text" };
        var inEvents = false;
        foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('['))
            {
                inEvents = line.Equals("[Events]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            var separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key == "Format" && inEvents)
            {
                fields = value.Split(',').Select(f => f.Trim()).ToArray();
            }
            else if (key == "Dialogue")
            {
                var startIdx = Array.IndexOf(fields, "Start");
                var endIdx = Array.IndexOf(fields, "End");
                var textIdx = Array.IndexOf(fields, "Text");
                var parts = value.Split(',');
                if (startIdx < 0 || endIdx < 0 || textIdx < 0 || parts.Length <= textIdx ||
                    !TryParseTimestamp(parts[startIdx], out var start) ||
                    !TryParseTimestamp(parts[endIdx], out var end))
                {
                    continue;
                }
                var text = CleanAssText(string.Join(',', parts[textIdx..]));
                if (text.Length > 0)
                {
                    cues.Add(new SubtitleCue(start, end, text));
                }
            }
        }
        return cues;
    }

    private static string CleanAssText(string text)
    {
        text = Regex.Replace(text, "\\{[^}]*\\}", "");
        text = text.Replace("\\N", "\n").Replace("\\n", "\n").Replace("\\h", " ");
        return text.Trim();
    }

    private static SubtitleCue? ParseBlock(string block)
    {
        var lines = block.Split('\n');
        var timeIndex = Array.FindIndex(lines, l => l.Contains("-->"));
        if (timeIndex < 0 || timeIndex + 1 >= lines.Length)
        {
            return null;
        }
        var parts = lines[timeIndex].Split("-->", 2);
        if (!TryParseTimestamp(parts.ElementAtOrDefault(0), out var start) ||
            !TryParseTimestamp(parts.ElementAtOrDefault(1), out var end))
        {
            return null;
        }
        var text = string.Join('\n', lines[(timeIndex + 1)..]).Trim();
        return text.Length == 0 ? null : new SubtitleCue(start, end, text);
    }

    private static bool TryParseTimestamp(string? value, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (value is null)
        {
            return false;
        }
        var segments = value.Trim().Replace(',', '.').Split(':');
        if (segments.Length != 3 && segments.Length != 2)
        {
            return false;
        }
        var hours = 0;
        var minuteIndex = 0;
        if (segments.Length == 3)
        {
            if (!int.TryParse(segments[0], out hours))
            {
                return false;
            }
            minuteIndex = 1;
        }
        if (!int.TryParse(segments[minuteIndex], out var minutes))
        {
            return false;
        }
        var secondParts = segments[^1].Split('.');
        if (!int.TryParse(secondParts[0], out var seconds))
        {
            return false;
        }
        var milliseconds = 0;
        if (secondParts.Length > 1)
        {
            if (!int.TryParse(secondParts[1].PadRight(3, '0')[..3], out milliseconds))
            {
                return false;
            }
        }
        time = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return true;
    }

    private static string ReadText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch
        {
            return Encoding.GetEncoding("GB18030").GetString(bytes);
        }
    }

    public static List<SubtitleCue> ParseSup(byte[] data)
    {
        var cues = new List<SubtitleCue>();
        var reader = new SupReader();
        TimeSpan? openStart = null;
        List<(int Id, int X, int Y)>? openComposition = null;
        var pos = 0;
        while (pos + 13 <= data.Length && data[pos] == 0x50 && data[pos + 1] == 0x47)
        {
            var pts = TimeSpan.FromMilliseconds(Be32(data, pos + 2) * 10.0 / 900);
            var type = data[pos + 10];
            var size = Be16(data, pos + 11);
            var bodyPos = pos + 13;
            pos = bodyPos + size;
            if (pos > data.Length)
            {
                break;
            }
            var body = data[bodyPos..pos];
            switch (type)
            {
                case 0x14:
                    ParsePalette(reader, body);
                    break;
                case 0x15:
                    ParseObject(reader, body);
                    break;
                case 0x16:
                    ParseComposition(reader, body);
                    break;
                case 0x80:
                    if (openStart is { } start && openComposition is { } objects)
                    {
                        AddPgsCue(cues, start, pts, objects, reader, SnapshotPalette(reader));
                    }
                    openStart = reader.Composition.Count > 0 ? pts : null;
                    openComposition = reader.Composition.Count > 0 ? reader.Composition : null;
                    break;
            }
        }
        if (openStart is { } lastStart && openComposition is { } lastObjects)
        {
            AddPgsCue(cues, lastStart, lastStart + TimeSpan.FromSeconds(8), lastObjects, reader, SnapshotPalette(reader));
        }
        return cues;
    }

    private static byte[][] SnapshotPalette(SupReader reader)
    {
        var palette = reader.Palette ?? new byte[256][];
        var snapshot = new byte[256][];
        Array.Copy(palette, snapshot, 256);
        return snapshot;
    }

    private sealed class SupReader
    {
        public byte[][]? Palette;
        public Dictionary<int, (byte[] Rle, int Width, int Height)> Objects = new();
        public Dictionary<int, MemoryStream> Fragments = new();
        public Dictionary<int, (int Width, int Height)> FragmentSizes = new();
        public List<(int Id, int X, int Y)> Composition = [];
        public int VideoWidth;
        public int VideoHeight;
    }

    private static void ParseComposition(SupReader reader, byte[] body)
    {
        if (body.Length < 11)
        {
            return;
        }
        reader.VideoWidth = Be16(body, 0);
        reader.VideoHeight = Be16(body, 2);
        var count = body[10];
        var pos = 11;
        var list = new List<(int Id, int X, int Y)>();
        for (var i = 0; i < count && pos + 8 <= body.Length; i++)
        {
            var flags = body[pos + 3];
            var x = Be16(body, pos + 4);
            var y = Be16(body, pos + 6);
            list.Add((Be16(body, pos), x, y));
            pos += 8;
            if ((flags & 0x80) != 0)
            {
                pos += 8;
            }
        }
        reader.Composition = list;
    }

    private static void ParseObject(SupReader reader, byte[] body)
    {
        if (body.Length < 4)
        {
            return;
        }
        var id = Be16(body, 0);
        var sequence = body[3];
        if ((sequence & 0x80) != 0)
        {
            if (body.Length < 11)
            {
                return;
            }
            var width = Be16(body, 7);
            var height = Be16(body, 9);
            var data = body[11..];
            if ((sequence & 0x40) != 0)
            {
                reader.Fragments.Remove(id);
                reader.FragmentSizes.Remove(id);
                reader.Objects[id] = (data, width, height);
            }
            else
            {
                var stream = new MemoryStream();
                stream.Write(data, 0, data.Length);
                reader.Fragments[id] = stream;
                reader.FragmentSizes[id] = (width, height);
            }
        }
        else
        {
            if (!reader.Fragments.TryGetValue(id, out var stream))
            {
                return;
            }
            stream.Write(body, 4, body.Length - 4);
            if ((sequence & 0x40) != 0 && reader.FragmentSizes.TryGetValue(id, out var size))
            {
                reader.Objects[id] = (stream.ToArray(), size.Width, size.Height);
                reader.Fragments.Remove(id);
                reader.FragmentSizes.Remove(id);
            }
        }
    }

    private static void ParsePalette(SupReader reader, byte[] body)
    {
        reader.Palette ??= new byte[256][];
        var hd = reader.VideoWidth >= 720;
        var pos = 2;
        while (pos + 5 <= body.Length)
        {
            var id = body[pos];
            reader.Palette[id] = ConvertColor(
                body[pos + 1], body[pos + 3], body[pos + 2], body[pos + 4], hd);
            pos += 5;
        }
    }

    private static byte[] ConvertColor(byte y, byte cb, byte cr, byte alpha, bool hd)
    {
        var crToR = hd ? 1.793 : 1.596;
        var cbToG = hd ? 0.213 : 0.391;
        var crToG = hd ? 0.533 : 0.813;
        var cbToB = hd ? 2.112 : 2.018;
        var luma = 1.164 * (y - 16);
        var r = (int)Math.Round(luma + crToR * (cr - 128));
        var g = (int)Math.Round(luma - cbToG * (cb - 128) - crToG * (cr - 128));
        var b = (int)Math.Round(luma + cbToB * (cb - 128));
        return [(byte)Math.Clamp(b, 0, 255), (byte)Math.Clamp(g, 0, 255), (byte)Math.Clamp(r, 0, 255), alpha];
    }

    private static void AddPgsCue(
        List<SubtitleCue> cues,
        TimeSpan start,
        TimeSpan end,
        List<(int Id, int X, int Y)> composition,
        SupReader reader,
        byte[][] palette)
    {
        var present = composition.Where(o => reader.Objects.ContainsKey(o.Id)).ToList();
        if (present.Count == 0)
        {
            return;
        }
        var minX = Math.Max(0, present.Min(o => o.X));
        var minY = Math.Max(0, present.Min(o => o.Y));
        var maxX = present.Max(o => o.X + reader.Objects[o.Id].Width);
        var maxY = present.Max(o => o.Y + reader.Objects[o.Id].Height);
        var width = maxX - minX;
        var height = maxY - minY;
        if (width <= 0 || height <= 0)
        {
            return;
        }
        var indices = new byte[width * height];
        foreach (var o in present)
        {
            var obj = reader.Objects[o.Id];
            var pixels = DecodeRle(obj.Rle, obj.Width, obj.Height);
            var clipLeft = Math.Max(minX, o.X);
            var clipTop = Math.Max(minY, o.Y);
            var clipRight = Math.Min(minX + width, o.X + obj.Width);
            var clipBottom = Math.Min(minY + height, o.Y + obj.Height);
            for (var y = clipTop; y < clipBottom; y++)
            {
                var src = (y - o.Y) * obj.Width + (clipLeft - o.X);
                var dst = (y - minY) * width + (clipLeft - minX);
                Array.Copy(pixels, src, indices, dst, clipRight - clipLeft);
            }
        }
        var videoWidth = reader.VideoWidth > 0 ? reader.VideoWidth : width;
        var videoHeight = reader.VideoHeight > 0 ? reader.VideoHeight : height;
        cues.Add(new SubtitleCue(
            start, end, "",
            new PgsImage(videoWidth, videoHeight, minX, minY, width, height, indices, palette)));
    }

    private static byte[] DecodeRle(byte[] rle, int width, int height)
    {
        var pixels = new byte[width * height];
        var pos = 0;
        var pixel = 0;
        while (pos < rle.Length && pixel < pixels.Length)
        {
            var color = rle[pos++];
            var run = 1;
            if (color == 0 && pos < rle.Length)
            {
                var flags = rle[pos++];
                run = flags & 0x3F;
                if ((flags & 0x40) != 0 && pos < rle.Length)
                {
                    run = (run << 8) + rle[pos++];
                }
                color = 0;
                if ((flags & 0x80) != 0 && pos < rle.Length)
                {
                    color = rle[pos++];
                }
            }
            if (run == 0)
            {
                if (pixel % width != 0)
                {
                    pixel = (pixel / width + 1) * width;
                }
            }
            else
            {
                if (pixel + run > pixels.Length)
                {
                    run = pixels.Length - pixel;
                }
                if (run > 0)
                {
                    Array.Fill(pixels, color, pixel, run);
                    pixel += run;
                }
            }
        }
        return pixels;
    }

    private static int Be16(byte[] data, int offset)
    {
        return (data[offset] << 8) | data[offset + 1];
    }

    private static uint Be32(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
