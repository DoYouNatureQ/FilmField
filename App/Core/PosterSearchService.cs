using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FilmField.Services;

public sealed class PosterCandidate
{
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string Key { get; init; } = "";
    public int? Year { get; init; }
    public int? Season { get; init; }
}

public static class PosterSearchService
{
    private static readonly string PosterDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FilmField", "posters");

    private static readonly HttpClient Http = CreateHttp();

    private static readonly Regex YearRegex = new(@"(?<!\d)(19\d{2}|20\d{2})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex BracketRegex = new(@"[\[（【「〈].*?[\]）】」〉]", RegexOptions.Compiled);
    private static readonly Regex SeasonMarkRegex = new(
        @"(?i)(?<![a-z0-9])S(\d{1,2})(?:\s?E\d{1,3})?\b|\b(?:season|series)\s*(\d{1,2})\b|\b(\d{1,2})x\d{1,3}\b|第\s*(\d{1,2})\s*[季部]|第\s*([一二三四五六七八九十]{1,3})\s*[季部]",
        RegexOptions.Compiled);
    private static readonly Regex SeasonStripRegex = new(
        @"(?i)(?<![a-z0-9])S\d{1,2}(?:\s?E\d{1,3})?\b|\bseason\s*\d{1,2}\b|\bseries\s*\d{1,2}\b|\bep(?:isode)?\s*\d{1,4}\b|\b\d{1,2}x\d{1,3}\b|第\s*\d{1,3}\s*[季集话部期]|第[一二三四五六七八九十]{1,3}[季集话部期]",
        RegexOptions.Compiled);
    private static readonly Regex SpacesRegex = new(@"\s+", RegexOptions.Compiled);

    private static readonly HashSet<string> NoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "1080p", "1080i", "720p", "2160p", "480p", "360p", "4k", "8k", "uhd", "fhd", "hd",
        "hdr", "hdr10", "dv", "sdr", "dolby", "vision", "atmos",
        "x264", "x265", "h264", "h265", "h", "264", "265", "hevc", "avc", "av1", "xvid", "divx",
        "bluray", "blu-ray", "bdrip", "bd", "brrip", "dvdrip", "remux", "repack", "proper",
        "webdl", "web-dl", "webrip", "web", "hdtv", "amzn", "nf", "hmax", "atvp", "dsnp",
        "aac", "dts", "ac3", "eac3", "flac", "truehd", "ddp", "5.1", "7.1", "2.0",
        "10bit", "8bit", "60fps", "120fps", "3d", "ts", "cam", "hdts",
        "extended", "unrated", "remastered", "imax", "internal", "limited", "complete", "edition",
        "dual", "audio", "mandarin", "cantonese", "english", "chinese", "chs", "cht", "sc", "tc", "gb", "big5",
        "sub", "subs", "subbed", "embed", "embedded", "mkv", "mp4", "avi", "mov", "wmv", "m2ts",
        "rarbg", "yts", "yify", "eztv", "ettv",
        "国语", "粤语", "国粤双语", "国配", "台配", "中英双字", "双字", "双语", "简中", "繁中",
        "简体", "繁体", "中字", "内封", "内嵌", "高清", "蓝光", "原盘", "高码", "杜比", "杜比视界",
        "全景声", "重制版", "修复版", "加长版", "收藏版", "公映版", "未删减", "导演剪辑版", "解说", "抢先版"
    };

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(6)
        })
        {
            Timeout = TimeSpan.FromSeconds(12)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FilmField/0.10 (local video player)");
        return client;
    }

    public static string ParseTitle(string rawName, out int? year, out int? season)
    {
        year = null;
        season = null;
        var raw = rawName.Trim();
        var name = SpacesRegex.Replace(BracketRegex.Replace(raw, " ").Replace('.', ' ').Replace('_', ' '), " ").Trim();
        var match = YearRegex.Match(name);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
        {
            year = value;
            name = name.Remove(match.Index, match.Length).Trim();
        }
        season = MatchSeason(name);
        var title = Extract(name);
        if (title.Length == 0)
        {
            title = Extract(BracketRegex.Replace(raw, " "));
        }
        return title;

        string Extract(string source)
        {
            source = SpacesRegex.Replace(source.Replace('.', ' ').Replace('_', ' '), " ").Trim();
            source = SeasonStripRegex.Replace(source, " ");
            var words = source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => !NoiseTokens.Contains(w))
                .ToList();
            return SpacesRegex.Replace(string.Join(" ", words), " ").Trim();
        }
    }

    private static int? MatchSeason(string name)
    {
        var m = SeasonMarkRegex.Match(name);
        if (!m.Success)
        {
            return null;
        }
        for (var i = 1; i < m.Groups.Count; i++)
        {
            var group = m.Groups[i];
            if (!group.Success || group.Value.Length == 0)
            {
                continue;
            }
            if (i == 5)
            {
                var cn = CnNumber(group.Value);
                return cn > 0 ? cn : null;
            }
            if (int.TryParse(group.Value, out var value) && value > 0)
            {
                return value;
            }
            return null;
        }
        return null;
    }

    private static int CnNumber(string text)
    {
        const string Digits = "零一二三四五六七八九";
        if (text == "十")
        {
            return 10;
        }
        var tenIndex = text.IndexOf('十');
        if (tenIndex < 0)
        {
            return text.Length == 1 ? Digits.IndexOf(text[0]) : 0;
        }
        var tens = tenIndex == 0 ? 1 : Digits.IndexOf(text[0]);
        var units = tenIndex == text.Length - 1 ? 0 : Digits.IndexOf(text[^1]);
        return tens * 10 + units;
    }

    public static string NumToCn(int n)
    {
        const string Digits = "零一二三四五六七八九";
        if (n < 10)
        {
            return Digits[n].ToString();
        }
        if (n == 10)
        {
            return "十";
        }
        if (n < 20)
        {
            return "十" + Digits[n % 10];
        }
        if (n % 10 == 0)
        {
            return Digits[n / 10] + "十";
        }
        return Digits[n / 10] + "十" + Digits[n % 10];
    }

    public static async Task<List<PosterCandidate>> SearchAsync(string title, int? year, int? season = null)
    {
        var query = title.Trim();
        if (query.Length == 0)
        {
            return [];
        }
        var imdb = SafeAsync(() => ImdbAsync(query));
        var tvmaze = SafeAsync(() => TvmazeAsync(query, season));
        var bangumi = SafeAsync(() => BangumiAsync(query));
        var bangumiSeason = season is { } n && n is >= 1 and <= 99
            ? SafeAsync(() => BangumiAsync($"{query} 第{NumToCn(n)}季", n))
            : Task.FromResult<List<PosterCandidate>>([]);
        await Task.WhenAll(imdb, tvmaze, bangumi, bangumiSeason);
        var list = new List<PosterCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in new[] { imdb.Result, tvmaze.Result, bangumi.Result, bangumiSeason.Result })
        {
            foreach (var candidate in group)
            {
                if (seen.Add(candidate.Key))
                {
                    list.Add(candidate);
                }
            }
        }
        return list
            .OrderByDescending(c => season is { } s && c.Season == s)
            .ThenByDescending(c => year is { } y && c.Year == y)
            .ToList();
    }

    public static async Task<string?> DownloadAsync(string url)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            if (bytes.Length < 1024)
            {
                return null;
            }
            Directory.CreateDirectory(PosterDir);
            var file = Path.Combine(PosterDir, Hash("poster|" + url) + ".jpg");
            await File.WriteAllBytesAsync(file, bytes);
            return file;
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> SaveLocalPosterAsync(string sourcePath)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(sourcePath);
            if (bytes.Length < 1024)
            {
                return null;
            }
            Directory.CreateDirectory(PosterDir);
            var file = Path.Combine(
                PosterDir,
                Hash("poster|local|" + Convert.ToHexString(SHA256.HashData(bytes))) + Path.GetExtension(sourcePath));
            File.WriteAllBytes(file, bytes);
            return file;
        }
        catch
        {
            return null;
        }
    }

    private static int _autoRunning;

    public static async Task AutoFetchAsync(IReadOnlyList<LibraryEntry> targets, Action<LibraryEntry> applied)
    {
        if (targets.Count == 0 || Interlocked.Exchange(ref _autoRunning, 1) == 1)
        {
            return;
        }
        try
        {
            var library = LibraryService.Instance;
            foreach (var entry in targets)
            {
                var path = entry.Path;
                if (library.HasPosterOverride(path) || library.IsPosterMissed(path))
                {
                    continue;
                }
                if (await HasLocalPosterAsync(path, entry.IsFolder))
                {
                    continue;
                }
                var title = ParseTitle(entry.Name, out var year, out var season);
                var done = false;
                if (title.Length > 0)
                {
                    var results = await SearchAsync(title, year, season);
                    foreach (var candidate in results)
                    {
                        var file = await DownloadAsync(candidate.ImageUrl);
                        if (file is null)
                        {
                            continue;
                        }
                        library.SetPosterOverride(path, file);
                        applied(entry);
                        done = true;
                        break;
                    }
                }
                if (!done)
                {
                    library.RecordPosterMiss(path);
                }
                await Task.Delay(600);
            }
        }
        catch
        {
        }
        finally
        {
            _autoRunning = 0;
        }
    }

    private static async Task<bool> HasLocalPosterAsync(string path, bool isFolder)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (isFolder)
                {
                    return LibraryService.FolderHasPoster(path);
                }
                var dir = Path.GetDirectoryName(path) ?? "";
                var stem = Path.GetFileNameWithoutExtension(path);
                string[] names =
                [
                    stem + ".jpg", stem + ".jpeg", stem + ".png",
                    "poster.jpg", "poster.png", "cover.jpg", "cover.png", "folder.jpg", "folder.png"
                ];
                return names.Any(n => File.Exists(Path.Combine(dir, n)));
            }
            catch
            {
                return false;
            }
        });
    }

    private static async Task<List<PosterCandidate>> SafeAsync(Func<Task<List<PosterCandidate>>> source)
    {
        try
        {
            return await source();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<List<PosterCandidate>> TvmazeAsync(string title, int? season)
    {
        var url = $"https://api.tvmaze.com/search/shows?q={Uri.EscapeDataString(title)}";
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(await Http.GetByteArrayAsync(url)));
        var shows = new List<(int Id, string Name, int? Premiered, string Image)>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("show", out var show) || !TryImage(show, out var image))
            {
                continue;
            }
            var name = TextOf(show, "name");
            int? premiered = YearOf(show, "premiered");
            var id = show.TryGetProperty("id", out var sid) && sid.ValueKind == JsonValueKind.Number
                ? sid.GetInt32()
                : 0;
            shows.Add((id, name, premiered, image));
            if (shows.Count >= 8)
            {
                break;
            }
        }
        var list = new List<PosterCandidate>();
        if (season is { } want && shows.Count > 0 && shows[0].Id > 0)
        {
            list.AddRange(await TvmazeSeasonsAsync(shows[0].Id, shows[0].Name, want));
        }
        foreach (var show in shows)
        {
            list.Add(new PosterCandidate
            {
                Title = show.Name,
                Subtitle = show.Premiered is null
                    ? $"{LocalizationService.L("剧集")} · TVmaze"
                    : $"{LocalizationService.L("剧集")} · {show.Premiered} · TVmaze",
                ImageUrl = show.Image,
                Key = show.Id > 0 ? "tvmaze" + show.Id : "tvmaze" + show.Name,
                Year = show.Premiered
            });
        }
        return list;
    }

    private static async Task<List<PosterCandidate>> TvmazeSeasonsAsync(int showId, string showName, int want)
    {
        try
        {
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(
                await Http.GetByteArrayAsync($"https://api.tvmaze.com/shows/{showId}/seasons")));
            var list = new List<PosterCandidate>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("image", out var image) || image.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var link = TextOf(image, "original");
                if (link.Length == 0)
                {
                    link = TextOf(image, "medium");
                }
                if (link.Length == 0)
                {
                    continue;
                }
                var number = item.TryGetProperty("number", out var num) && num.ValueKind == JsonValueKind.Number
                    ? num.GetInt32()
                    : 0;
                if (number <= 0)
                {
                    continue;
                }
                var year = YearOf(item, "premiereDate");
                list.Add(new PosterCandidate
                {
                    Title = $"{showName} {LocalizationService.L("第{0}季", number)}",
                    Subtitle = year is null
                        ? $"{LocalizationService.L("第{0}季", number)} · TVmaze"
                        : $"{LocalizationService.L("第{0}季", number)} · {year} · TVmaze",
                    ImageUrl = link,
                    Key = $"tvmaze{showId}s{number}",
                    Year = year,
                    Season = number
                });
            }
            return list
                .OrderByDescending(s => s.Season == want)
                .ThenBy(s => s.Season)
                .Take(6)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<List<PosterCandidate>> ImdbAsync(string title)
    {
        var url = $"https://v3.sg.media-imdb.com/suggestion/x/{Uri.EscapeDataString(title)}.json?includeVideos=0";
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(await Http.GetByteArrayAsync(url)));
        var list = new List<PosterCandidate>();
        if (!doc.RootElement.TryGetProperty("d", out var results))
        {
            return list;
        }
        var portrait = new List<PosterCandidate>();
        var other = new List<PosterCandidate>();
        foreach (var item in results.EnumerateArray())
        {
            if (!item.TryGetProperty("qid", out var qid) || qid.ValueKind != JsonValueKind.String ||
                qid.GetString() is not { } kind ||
                kind is not ("movie" or "tvSeries" or "tvMiniSeries" or "tvMovie"))
            {
                continue;
            }
            if (!item.TryGetProperty("i", out var image) || image.ValueKind != JsonValueKind.Object ||
                !image.TryGetProperty("imageUrl", out var imageUrl) ||
                imageUrl.ValueKind != JsonValueKind.String ||
                imageUrl.GetString() is not { Length: > 0 } raw)
            {
                continue;
            }
            var name = TextOf(item, "l");
            if (name.Length == 0)
            {
                continue;
            }
            var year = item.TryGetProperty("y", out var y) && y.ValueKind == JsonValueKind.Number
                ? y.GetInt32()
                : null as int?;
            var wide = image.TryGetProperty("width", out var w) && image.TryGetProperty("height", out var h) &&
                w.ValueKind == JsonValueKind.Number && h.ValueKind == JsonValueKind.Number &&
                w.GetDouble() >= h.GetDouble();
            var candidate = new PosterCandidate
            {
                Title = name,
                Subtitle = KindLabel(kind) + (year is null ? "" : $" · {year}") + " · IMDb",
                ImageUrl = raw.Replace("._V1_.jpg", "._V1_UX500_.jpg"),
                Key = TextOf(item, "id").Length > 0 ? "imdb" + TextOf(item, "id") : "imdb" + name,
                Year = year
            };
            (wide ? other : portrait).Add(candidate);
            if (portrait.Count + other.Count >= 12)
            {
                break;
            }
        }
        list.AddRange(portrait);
        list.AddRange(other);
        return list;
    }

    private static string KindLabel(string kind) => kind switch
    {
        "tvSeries" => LocalizationService.L("剧集"),
        "tvMiniSeries" => LocalizationService.L("迷你剧"),
        "tvMovie" => LocalizationService.L("电视电影"),
        _ => LocalizationService.L("电影")
    };

    private static async Task<List<PosterCandidate>> BangumiAsync(string title, int? seasonMark = null)
    {
        var url = $"https://api.bgm.tv/search/subject/{Uri.EscapeDataString(title)}?type=2&max_results=10&responseGroup=large";
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(await Http.GetByteArrayAsync(url)));
        var list = new List<PosterCandidate>();
        if (!doc.RootElement.TryGetProperty("results", out var results))
        {
            return list;
        }
        foreach (var subject in results.EnumerateArray())
        {
            if (!TryImage(subject, out var image))
            {
                continue;
            }
            var nameCn = TextOf(subject, "name_cn");
            var name = TextOf(subject, "name");
            if (nameCn.Length == 0 && name.Length == 0)
            {
                continue;
            }
            var year = YearOf(subject, "date");
            list.Add(new PosterCandidate
            {
                Title = nameCn.Length > 0 ? nameCn : name,
                Subtitle = year is null ? $"{LocalizationService.L("番剧")} · Bangumi" : $"{year} · Bangumi",
                ImageUrl = image,
                Key = subject.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number
                    ? "bgm" + id.GetInt32()
                    : "bgm" + name,
                Year = year,
                Season = seasonMark ?? MatchSeason(nameCn.Length > 0 ? nameCn : name)
            });
            if (list.Count >= 8)
            {
                break;
            }
        }
        return list;
    }

    private static string TextOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static int? YearOf(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        value.GetString() is { Length: >= 4 } text &&
        int.TryParse(text[..4], out var year)
            ? year
            : null;

    private static bool TryImage(JsonElement element, out string url)
    {
        url = "";
        foreach (var container in new[] { "image", "images" })
        {
            if (!element.TryGetProperty(container, out var images) || images.ValueKind != JsonValueKind.Object)
            {
                continue;
            }
            foreach (var key in new[] { "original", "common", "large", "medium", "grid" })
            {
                if (images.TryGetProperty(key, out var value) &&
                    value.ValueKind == JsonValueKind.String &&
                    value.GetString() is { Length: > 0 } link)
                {
                    url = link;
                    return true;
                }
            }
        }
        return false;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
}
