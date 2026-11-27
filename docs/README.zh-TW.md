# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#安裝)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | 繁體中文 | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

你的私人影院。

FilmField 是一款現代化的本機影片播放器，旨在將你的影片收藏變成一個精美的私人電影庫。
透過直觀的電影海報牆瀏覽影片，輕鬆管理本機媒體檔案，並享受流暢的播放體驗。一切盡在一處。

介面復刻 Windows 11 內建媒體播放器的 Fluent 設計風格，輕量、快速、原生觀感。它基於 WinUI 3 與 FFmpeg 構建，支援 Windows 10（1809）及以上版本。

播放核心由 [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) 驅動，經由系統 Media Foundation 硬體解碼，低佔用播放主流格式的影片與音訊。

## 截圖

<p align="center">
  <img src="promo/screenshots/player.png" alt="播放頁：Fluent 控制列、進度列縮圖預覽與字幕" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="視窗全螢幕：保留工作列的全螢幕播放" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="主頁：海報網格與媒體庫" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="電影庫：海報牆與排序、管理工具列" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="播放記錄：觀看熱度圖與統計" width="760">
</p>

一些值得一提的功能：

- Fluent Design 介面，復刻 Windows 11 媒體播放器風格
- FFmpeg 核心 + 系統硬解，廣泛涵蓋影片與音訊格式
- 媒體庫管理：資料夾掃描、封面/海報網格、拖曳整理、多庫分類
- 網路擷取海報（IMDb / TVmaze / Bangumi），支援影集分季海報與本機圖片替換
- 字幕支援：SRT / ASS / SSA / VTT / PGS（藍光圖形字幕），外部字幕自動匹配與記憶
- 進度列縮圖預覽：取樣影格 + 精確取影格混合方案，滑鼠懸停即可預覽
- 一鍵截圖，可疊加字幕儲存
- 播放記錄：GitHub 式觀看熱度圖與統計面板
- 兩種全螢幕模式：視窗全螢幕（保留工作列）與完全全螢幕
- 雲母 / 壓克力背景材質，控制列毛玻璃
- 長按方向鍵倍速掃描，快轉快退更順手
- 播放佇列、記憶播放位置、開機自動啟動

更多功能還在路上！

## 安裝

前往 [Releases](https://github.com/DoYouNatureQ/FilmField/releases) 下載最新版本的壓縮檔，解壓縮後直接執行 `FilmField.exe` 即可，無需安裝。應用為自包含部署，不需要額外安裝執行環境。

> FilmField 是純本機播放器，不提供、不內建任何影視片源。

### 從原始碼建置

1. **環境需求**：Windows 10 1809+，.NET 8 SDK（含 Windows 桌面工作負載），Windows App SDK 1.8
2. **複製儲存庫**：`git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **建置**：`dotnet build -p:Platform=x64`
4. **執行**：建置產物輸出至 `publish\`，執行 `publish\FilmField.exe`

## 參與貢獻

歡迎提交問題回報、功能建議以及程式碼貢獻。

如果你發現了 Bug，或有任何改進建議，歡迎透過 Issues 提交回報，也歡迎提交 Pull Request。

## 銘謝

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+，解碼核心
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0，FFmpeg 與 Windows 媒體管線的橋接
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT，WinUI 3 應用程式框架
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — 介面字型
- 海報資料來自 [IMDb](https://www.imdb.com)、[TVmaze](https://www.tvmaze.com)、[Bangumi](https://bgm.tv)

完整的第三方元件版權資訊見 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

## 授權

本專案基於 [GPL-3.0](../LICENSE) 授權條款開源。
