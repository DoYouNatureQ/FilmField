using System.Globalization;

namespace FilmField.Services;

public static class LocalizationService
{
    public const int LanguageSystem = 0;
    public const int LanguageChinese = 1;
    public const int LanguageEnglish = 2;
    public const int LanguageTraditional = 3;
    public const int LanguageJapanese = 4;
    public const int LanguageKorean = 5;
    public const int LanguageRussian = 6;
    public const int LanguageSpanish = 7;
    public const int LanguageItalian = 8;
    public const int LanguagePortuguese = 9;
    public const int LanguageLatin = 10;
    public const int LanguageGerman = 11;
    public const int LanguageFrench = 12;
    public const int LanguageTurkish = 13;

    public static event Action? LanguageChanged;

    public static int CurrentLanguage =>
        SettingsService.Current.LanguageMode <= LanguageSystem
            ? DetectSystemLanguage()
            : Math.Min(SettingsService.Current.LanguageMode, LanguageTurkish);

    public static bool IsEnglish => CurrentLanguage == LanguageEnglish;

    public static string L(string key, params object?[] args)
    {
        var lang = CurrentLanguage;
        var text = lang switch
        {
            LanguageChinese => ChineseOverrides.GetValueOrDefault(key, key),
            LanguageEnglish => English.GetValueOrDefault(key, ChineseOverrides.GetValueOrDefault(key, key)),
            _ => Languages[lang].GetValueOrDefault(key)
                ?? English.GetValueOrDefault(key, ChineseOverrides.GetValueOrDefault(key, key))
        };
        return args.Length == 0 ? text : string.Format(text, args);
    }

    public static void NotifyChanged() => LanguageChanged?.Invoke();

    private static int DetectSystemLanguage()
    {
        var culture = CultureInfo.CurrentUICulture;
        var name = culture.Name;
        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            if (name.Contains("Hant") || name.EndsWith("TW") || name.EndsWith("HK") || name.EndsWith("MO"))
            {
                return LanguageTraditional;
            }
            return LanguageChinese;
        }
        return culture.TwoLetterISOLanguageName.ToLowerInvariant() switch
        {
            "en" => LanguageEnglish,
            "ja" => LanguageJapanese,
            "ko" => LanguageKorean,
            "ru" => LanguageRussian,
            "es" => LanguageSpanish,
            "it" => LanguageItalian,
            "pt" => LanguagePortuguese,
            "de" => LanguageGerman,
            "fr" => LanguageFrench,
            "tr" => LanguageTurkish,
            _ => LanguageEnglish
        };
    }

    private static readonly Dictionary<int, Dictionary<string, string>> Languages = new()
    {
        [LanguageTraditional] = LocalizationZhHant.Map,
        [LanguageJapanese] = LocalizationJa.Map,
        [LanguageKorean] = LocalizationKo.Map,
        [LanguageRussian] = LocalizationRu.Map,
        [LanguageSpanish] = LocalizationEs.Map,
        [LanguageItalian] = LocalizationIt.Map,
        [LanguagePortuguese] = LocalizationPt.Map,
        [LanguageLatin] = LocalizationLa.Map,
        [LanguageGerman] = LocalizationDe.Map,
        [LanguageFrench] = LocalizationFr.Map,
        [LanguageTurkish] = LocalizationTr.Map
    };

    private static readonly Dictionary<string, string> ChineseOverrides = new()
    {
        ["nav.播放"] = "播放",
        ["section.播放"] = "播放",
        ["section.视频"] = "视频",
        ["kind.视频"] = "视频",
        ["section.文件夹"] = "文件夹",
        ["kind.文件夹"] = "文件夹",
        ["kind.集合"] = "文件夹",
        ["section.库"] = "库",
        ["kind.库"] = "库"
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["nav.播放"] = "Player",
        ["播放队列"] = "Play Queue",
        ["主页"] = "Home",
        ["新建库"] = "New Library",
        ["历史记录"] = "History",
        ["设置"] = "Settings",
        ["重命名"] = "Rename",
        ["删除"] = "Delete",
        ["库名称"] = "Library name",
        ["重命名库"] = "Rename library",
        ["确定"] = "OK",
        ["取消"] = "Cancel",
        ["删除「{0}」？"] = "Delete \"{0}\"?",
        ["只删除这个库与其中的观看记录，磁盘上的视频文件不会被删除。"] = "This only removes the library and its watch history; video files on disk will not be deleted.",
        ["删除库"] = "Delete library",
        ["创建"] = "Create",
        ["复制到「{0}」"] = "Copy to \"{0}\"",
        ["移动到「{0}」"] = "Move to \"{0}\"",
        ["已复制到「{0}」"] = "Copied to \"{0}\"",
        ["已移动到「{0}」"] = "Moved to \"{0}\"",

        ["拖放视频文件到窗口，或点击下方的打开按钮"] = "Drop video files into the window, or use the open button below",
        ["上一个"] = "Previous",
        ["播放/暂停"] = "Play/Pause",
        ["下一个"] = "Next",
        ["循环模式"] = "Repeat mode",
        ["随机播放"] = "Shuffle",
        ["选集"] = "Episodes",
        ["播放列表"] = "Playlist",
        ["播放列表 · {0} 集"] = "Playlist · {0} episodes",
        ["静音"] = "Mute",
        ["播放速度"] = "Playback speed",
        ["字幕"] = "Subtitles",
        ["加载字幕文件..."] = "Load subtitle file...",
        ["关闭字幕"] = "Close subtitles",
        ["内封轨道 {0}"] = "Embedded track {0}",
        ["截图"] = "Screenshot",
        ["打开文件"] = "Open file",
        ["窗口全屏"] = "Window fullscreen",
        ["退出窗口全屏"] = "Exit window fullscreen",
        ["全屏"] = "Fullscreen",
        ["退出全屏"] = "Exit fullscreen",
        ["循环全部"] = "Repeat all",
        ["单曲循环"] = "Repeat one",
        ["循环关闭"] = "Repeat off",
        ["无法创建播放项"] = "Failed to create playback item",
        ["无法打开：{0}"] = "Cannot open: {0}",
        ["解码失败：{0}"] = "Decoding failed: {0}",
        ["字幕加载失败：{0}"] = "Failed to load subtitles: {0}",
        ["截图失败"] = "Screenshot failed",
        ["已保存截图：{0}"] = "Screenshot saved: {0}",

        ["{0} 个文件夹"] = "{0} folders",
        ["{0} 个视频"] = "{0} videos",
        ["{0} 个库"] = "{0} libraries",
        ["{0} 个条目"] = "{0} items",
        ["{0} 个文件夹 · {1} 个视频"] = "{0} folders · {1} videos",
        ["返回上一级"] = "Go up one level",
        ["正在扫描…"] = "Scanning…",
        ["最近播放"] = "Recently played",
        ["名称"] = "Name",
        ["大小"] = "Size",
        ["刷新"] = "Refresh",
        ["管理"] = "Manage",
        ["添加"] = "Add",
        ["文件夹"] = "Folder",
        ["视频文件"] = "Video files",
        ["section.库"] = "Libraries",
        ["section.视频"] = "Videos",
        ["section.播放"] = "Playback",
        ["section.文件夹"] = "Folders",
        ["kind.文件夹"] = "Folder",
        ["kind.集合"] = "Collection",
        ["kind.视频"] = "Video",
        ["kind.库"] = "Library",
        ["上一页"] = "Previous page",
        ["下一页"] = "Next page",
        ["第 {0} / {1} 页"] = "Page {0} / {1}",
        ["这里还没有视频"] = "Nothing here yet",
        ["添加文件夹或视频文件，自动生成海报墙与观看记录"] = "Add folders or video files to build a poster wall and watch history",
        ["添加文件夹"] = "Add folder",
        ["添加文件"] = "Add files",
        ["这个文件夹里还没有视频"] = "No videos in this collection yet",
        ["这个文件夹里没有视频"] = "No videos in this folder",
        ["没有找到视频文件"] = "No video files found",
        ["把海报拖进来就能放进文件夹"] = "Drag posters here to add them to the collection",
        ["当前文件夹内没有视频文件或子文件夹"] = "No video files or subfolders in this folder",
        ["确认文件夹里是否有视频，或再添加其他文件夹"] = "Check whether the folder contains videos, or add another folder",
        ["添加文件夹或视频文件，自动生成封面网格与观看记录"] = "Add folders or video files to build the cover grid and watch history",
        ["{0}的来源"] = "Sources of {0}",
        ["关闭"] = "Close",
        ["还没有添加任何来源"] = "No sources added yet",
        ["文件"] = "Files",
        ["移除"] = "Remove",
        ["详细"] = "Details",
        ["打开"] = "Open",
        ["解散文件夹"] = "Dissolve collection",
        ["从库中移除"] = "Remove from library",
        ["联网查找海报"] = "Find poster online",
        ["选择本地图片"] = "Choose local image",
        ["从文件夹移除"] = "Remove from collection",
        ["新建文件夹"] = "New collection",
        ["重命名文件夹"] = "Rename collection",
        ["文件夹名称"] = "Collection name",
        ["重命名视频"] = "Rename video",
        ["视频名称"] = "Video name",
        ["重命名{0}"] = "Rename {0}",
        ["搜索关键词"] = "Search keywords",
        ["搜索"] = "Search",
        ["使用选中海报"] = "Use selected poster",
        ["恢复默认"] = "Reset to default",
        ["正在搜索…"] = "Searching…",
        ["没有找到候选，试试修改关键词"] = "No candidates found; try different keywords",
        ["找到 {0} 个候选"] = "Found {0} candidates",
        ["· 已识别第{0}季，对应海报排前"] = " · Season {0} detected; matching posters ranked first",
        ["· 数据来自 IMDb / TVmaze / Bangumi"] = " · Data from IMDb / TVmaze / Bangumi",
        ["搜索失败，请检查网络连接"] = "Search failed; check your network connection",
        ["海报下载失败，请重试"] = "Poster download failed; please retry",
        ["已更新海报"] = "Poster updated",
        ["已恢复默认封面"] = "Poster reset to default",
        ["移动至其他库"] = "Move to another library",
        ["复制至其他库"] = "Copy to another library",
        ["已加入「{0}」"] = "Added to \"{0}\"",
        ["已组成文件夹"] = "Grouped into a collection",
        ["已移出文件夹"] = "Removed from the collection",
        ["已解散文件夹"] = "Collection dissolved",
        ["已重命名"] = "Renamed",
        ["重命名失败"] = "Rename failed",
        ["组成文件夹"] = "Group into a collection",
        ["加入「{0}」"] = "Add to \"{0}\"",
        ["移出文件夹"] = "Remove from collection",
        ["库 · {0} 个视频"] = "Library · {0} videos",
        ["文件夹 · {0} 个视频"] = "Collection · {0} videos",
        ["视频 · {0}"] = "Video · {0}",
        ["类型"] = "Type",
        ["数量"] = "Count",
        ["位置"] = "Location",
        ["时长"] = "Duration",
        ["观看"] = "Views",
        ["上次"] = "Last played",
        ["进度"] = "Progress",
        ["{0} 次"] = "{0} times",
        ["未播放"] = "Not played",
        ["未开始"] = "Not started",
        ["详细信息"] = "Details",
        ["保存名称"] = "Save name",
        ["从未播放"] = "Never played",
        ["今天 {0}"] = "Today {0}",
        ["昨天 {0}"] = "Yesterday {0}",
        ["M月d日 HH:mm"] = "MMM d, HH:mm",
        ["yyyy年M月d日 HH:mm"] = "MMM d, yyyy, HH:mm",
        ["M月d日"] = "MMM d",
        ["yyyy年M月d日"] = "MMM d, yyyy",
        ["文件将移入回收站。"] = "The file will be moved to the Recycle Bin.",
        ["删除视频"] = "Delete video",
        ["无法删除"] = "Cannot delete",
        ["文件可能正在被使用，或当前账户没有权限。"] = "The file may be in use, or the current account lacks permission.",
        ["知道了"] = "Got it",
        ["只从当前库移除这个来源，不会删除磁盘上的文件。"] = "This removes the source from the current library only; files on disk are not deleted.",
        ["图片文件无效"] = "Invalid image file",
        ["未知"] = "Unknown",

        ["队列为空"] = "Queue is empty",
        ["拖放视频文件到窗口，或用控制条上的打开按钮加入队列"] = "Drop video files into the window, or use the open button on the control bar to add them",

        ["观看活动"] = "Watch activity",
        ["每格代表一天"] = "Each cell represents one day",
        ["少"] = "Less",
        ["多"] = "More",
        ["近一年播放"] = "Plays in the past year",
        ["活跃天数"] = "Active days",
        ["最长连续"] = "Longest streak",
        ["最近观看"] = "Last watched",
        ["最近看过"] = "Recently watched",
        ["还没有观看记录"] = "No watch history yet",
        ["播放过的视频会按时间出现在这里"] = "Videos you play will show up here over time",
        ["一"] = "Mon",
        ["三"] = "Wed",
        ["五"] = "Fri",
        ["日"] = "Sun",
        ["{0}月"] = "{0}",
        ["播放 {0} 次"] = "{0} plays",
        ["{0} 天"] = "{0} days",

        ["通用"] = "General",
        ["开机自动启动"] = "Launch at startup",
        ["外观"] = "Appearance",
        ["主题"] = "Theme",
        ["语言"] = "Language",
        ["跟随系统"] = "Follow system",
        ["浅色"] = "Light",
        ["深色"] = "Dark",
        ["记住播放位置"] = "Remember playback position",
        ["点击画面播放/暂停"] = "Click video to play/pause",
        ["切换面板时暂停播放"] = "Pause when switching panels",
        ["←/→ 按住倍速"] = "Hold ←/→ to scan",
        ["轻按跳转"] = "Tap skip",
        ["3 秒"] = "3 s",
        ["5 秒"] = "5 s",
        ["10 秒"] = "10 s",
        ["15 秒"] = "15 s",
        ["30 秒"] = "30 s",
        ["按住慢放"] = "Hold slow",
        ["按住快放"] = "Hold fast",
        ["{0} 秒"] = "{0} s",
        ["解码"] = "Decoding",
        ["FFmpeg 优先（支持 HEVC / DTS 等更多格式）"] = "Prefer FFmpeg (more formats such as HEVC / DTS)",
        ["仅系统解码（兼容性保守）"] = "System codecs only (conservative compatibility)",
        ["截图包含字幕"] = "Include subtitles in screenshots",
        ["更改..."] = "Change...",
        ["媒体库"] = "Media library",
        ["自动联网查找海报"] = "Auto-fetch posters online",
        ["缓存"] = "Cache",
        ["清理缩略图与预览缓存"] = "Clear thumbnail and preview cache",
        ["暂无缓存"] = "No cache",
        ["当前占用 {0} MB"] = "Currently using {0} MB",
        ["清理后各库封面与进度条预览会在需要时重新生成"] = "Covers and progress previews regenerate as needed after clearing",
        ["关于"] = "About",
        ["© 2026 NatureQ（DoYouNatureQ），保留所有权利"] = "© 2026 NatureQ (DoYouNatureQ). All rights reserved.",
        ["FilmField 是纯本地播放器，不内置、不提供、不指向任何影片资源；播放内容均来自用户自己的本地文件，其著作权归原权利人所有"] = "FilmField is a purely local player that does not bundle, provide, or point to any video content; playback comes entirely from the user's own local files, whose copyright belongs to the original rights holders.",
        ["界面字体：HarmonyOS Sans SC © Huawei Device Co., Ltd.，依鸿蒙字体许可协议免费商用，原样打包未做修改"] = "UI font: HarmonyOS Sans SC © Huawei Device Co., Ltd., free for commercial use under the HarmonyOS font license, packaged unmodified",
        ["解码内核：FFmpeg（LGPL-2.1）· FFmpegInteropX（Apache 2.0）· Windows App SDK（MIT）"] = "Decoding: FFmpeg (LGPL-2.1) · FFmpegInteropX (Apache 2.0) · Windows App SDK (MIT)",
        ["海报数据：IMDb / TVmaze / Bangumi，仅按用户请求临时获取缓存用于本地展示，版权归对应平台与权利人所有"] = "Poster data: IMDb / TVmaze / Bangumi, temporarily fetched on request and cached for local display; copyright belongs to the respective platforms and rights holders.",
        ["界面符号图标使用系统自带 Segoe Fluent Icons 字体，版权 Microsoft Corporation"] = "UI symbols use the built-in Segoe Fluent Icons font, © Microsoft Corporation",
        ["查看完整版权声明"] = "View full copyright notices",
        ["清理完成"] = "Cleanup complete",
        ["缩略图与预览缓存已删除，将在需要时重新生成。"] = "Thumbnail and preview caches were deleted and will regenerate when needed.",

        ["剧集"] = "TV series",
        ["迷你剧"] = "Miniseries",
        ["电视电影"] = "TV movie",
        ["电影"] = "Movie",
        ["番剧"] = "Anime",
        ["第{0}季"] = "Season {0}",

        ["新建库 {0}"] = "New Library {0}",
        ["未命名库"] = "Unnamed library",
        ["新建文件夹 {0}"] = "Collection {0}"
    };
}
