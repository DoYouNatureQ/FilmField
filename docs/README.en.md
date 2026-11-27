# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#installation)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | English | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Your personal cinema.

FilmField is a modern local video player that turns your video collection into a beautiful private movie library.
Browse your videos through an intuitive movie poster wall, manage your local media files with ease, and enjoy smooth playback. Everything in one place.

The interface recreates the Fluent design of the Windows 11 built-in media player — lightweight, fast, and native-feeling. It is built on WinUI 3 with FFmpeg, and supports Windows 10 (1809) and above.

The playback engine is powered by [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX), using system Media Foundation hardware decoding to play mainstream video and audio formats with low resource usage.

## Screenshots

<p align="center">
  <img src="promo/screenshots/player.png" alt="Player: Fluent control bar, seek bar thumbnail preview, and subtitles" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Windowed fullscreen: fullscreen playback with taskbar kept" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Home: poster grid and media library" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Movies library: poster wall with sort and manage toolbar" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="History: viewing heatmap and statistics" width="760">
</p>

Some features worth mentioning:

- Fluent Design interface, recreating the Windows 11 media player style
- FFmpeg engine + system hardware decoding, with broad video and audio format coverage
- Media library management: folder scanning, poster grids, drag-and-drop organizing, multiple libraries
- Online poster lookup (IMDb / TVmaze / Bangumi), with season posters and local image replacement
- Subtitle support: SRT / ASS / SSA / VTT / PGS (Blu-ray graphic subtitles), automatic external subtitle matching and memory
- Seek bar thumbnail preview: a hybrid of sampled frames + precise frame extraction — what you hover is what you see
- One-click screenshot, with optional subtitle overlay
- Playback history: GitHub-style viewing heatmap and statistics panel
- Two fullscreen modes: windowed fullscreen (keeps the taskbar) and true fullscreen
- Mica / Acrylic background materials, frosted-glass control bar
- Hold arrow keys for speed scanning, making seeking easier
- Playback queue, resume position, launch at startup

More features are on the way!

## Installation

Head to [Releases](https://github.com/DoYouNatureQ/FilmField/releases) to download the latest package. Extract it and run `FilmField.exe` directly — no installation needed. The app is self-contained, so no extra runtime is required.

> FilmField is a purely local player. It does not provide or bundle any video sources.

### Build from Source

1. **Prerequisites**: Windows 10 1809+, .NET 8 SDK (with the Windows desktop workload), Windows App SDK 1.8
2. **Clone**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Build**: `dotnet build -p:Platform=x64`
4. **Run**: build output goes to `publish\`; run `publish\FilmField.exe`

## Contributing

Issue reports, feature suggestions, and code contributions are all welcome.

If you find a bug or have any improvement ideas, feel free to open an Issue — Pull Requests are welcome too.

## Acknowledgements

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, decoding core
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, bridging FFmpeg and the Windows media pipeline
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, WinUI 3 app framework
- [HarmonyOS Sans](https://developer.huawei.com/consumer/en/design/) — UI font
- Poster data from [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for full copyright information of third-party components.

## License

This project is open source under the [GPL-3.0](../LICENSE) license.
