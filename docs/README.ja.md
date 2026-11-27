# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#インストール)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | 日本語 | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

あなたのプライベートシネマ。

FilmField はモダンなローカル動画プレイヤーです。動画コレクションを美しいプライベートな映画ライブラリに変えることを目指しています。
直感的なポスターウォールで動画を閲覧し、ローカルのメディアファイルを簡単に管理しながら、滑らかな再生体験を楽しめます。すべてが一つの場所に。

インターフェースは Windows 11 標準のメディアプレイヤーの Fluent デザインを再現しており、軽量・高速・ネイティブな操作感。WinUI 3 と FFmpeg を基盤とし、Windows 10（1809）以降に対応しています。

再生エンジンは [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) によって駆動され、システムの Media Foundation ハードウェアデコードを経由して、主流フォーマットの動画と音声を低負荷で再生します。

## スクリーンショット

<p align="center">
  <img src="promo/screenshots/player.png" alt="プレイヤー画面：Fluent コントロールバー、シークバーのサムネイルプレビューと字幕" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="ウィンドウ全画面：タスクバーを残す全画面再生" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="ホーム：ポスターグリッドとメディアライブラリ" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="映画ライブラリ：ポスターウォールと並べ替え・管理ツールバー" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="再生履歴：視聴ヒートマップと統計" width="760">
</p>

紹介したい機能の一部：

- Fluent Design インターフェース、Windows 11 メディアプレイヤーのスタイルを再現
- FFmpeg エンジン + システムハードウェアデコード、幅広い動画・音声フォーマットに対応
- メディアライブラリ管理：フォルダスキャン、ポスターグリッド、ドラッグ＆ドロップ整理、複数ライブラリ分類
- ネットワークからのポスター取得（IMDb / TVmaze / Bangumi）、ドラマ・アニメのシーズン別ポスターとローカル画像への差し替えに対応
- 字幕対応：SRT / ASS / SSA / VTT / PGS（Blu-ray グラフィック字幕）、外部字幕の自動マッチングと記憶
- シークバーのサムネイルプレビュー：サンプリングフレーム + 高精度フレーム抽出のハイブリッド方式、ホバーした位置がそのまま見える
- ワンクリックスクリーンショット、字幕を重ねての保存にも対応
- 再生履歴：GitHub 風の視聴ヒートマップと統計パネル
- 2 つの全画面モード：ウィンドウ全画面（タスクバーを維持）とフルスクリーン
- Mica / Acrylic 背景素材、すりガラス風コントロールバー
- 方向キーの長押しで倍速シーク、早送り・巻き戻しが快適
- 再生キュー、再生位置の記憶、スタートアップ起動

さらに多くの機能が開発中です！

## インストール

[Releases](https://github.com/DoYouNatureQ/FilmField/releases) から最新版のパッケージをダウンロードし、解凍して `FilmField.exe` を実行するだけです。インストールは不要。アプリは自己完結型デプロイのため、追加のランタイムも必要ありません。

> FilmField は純粋なローカルプレイヤーであり、映像ソースは一切提供・同梱していません。

### ソースからビルド

1. **必要環境**：Windows 10 1809+、.NET 8 SDK（Windows デスクトップワークロード付き）、Windows App SDK 1.8
2. **クローン**：`git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **ビルド**：`dotnet build -p:Platform=x64`
4. **実行**：ビルド成果物は `publish\` に出力されます。`publish\FilmField.exe` を実行してください

## コントリビューション

バグ報告、機能提案、コード貢献を歓迎します。

バグを発見した場合や改善提案がある場合は、Issues でお知らせください。Pull Request も歓迎です。

## 謝辞

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+、デコードコア
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0、FFmpeg と Windows メディアパイプラインのブリッジ
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT、WinUI 3 アプリフレームワーク
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — UI フォント
- ポスターデータ：[IMDb](https://www.imdb.com)、[TVmaze](https://www.tvmaze.com)、[Bangumi](https://bgm.tv)

サードパーティコンポーネントの完全な著作権情報は [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) をご覧ください。

## ライセンス

本プロジェクトは [GPL-3.0](../LICENSE) ライセンスの下でオープンソースとして公開されています。
