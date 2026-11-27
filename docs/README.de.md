# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#installation)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | Deutsch | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Dein privates Kino.

FilmField ist ein moderner lokaler Video-Player, der deine Videosammlung in eine schöne private Filmbibliothek verwandelt.
Durchstöbere deine Videos über eine intuitive Filmposter-Galerie, verwalte deine lokalen Mediendateien mühelos und genieße eine flüssige Wiedergabe. Alles an einem Ort.

Die Oberfläche bildet das Fluent-Design des in Windows 11 integrierten Media-Players nach – leicht, schnell und mit nativem Gefühl. Es basiert auf WinUI 3 und FFmpeg und unterstützt Windows 10 (1809) und höher.

Die Wiedergabe-Engine wird von [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) angetrieben und nutzt die Hardware-Dekodierung des Systems über Media Foundation, um Videos und Audio gängiger Formate mit geringer Ressourcenlast wiederzugeben.

## Screenshots

<p align="center">
  <img src="promo/screenshots/player.png" alt="Player: Fluent-Steuerelementeleiste, Thumbnail-Vorschau in der Fortschrittsleiste und Untertitel" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Vollbild im Fenster: Vollbild-Wiedergabe mit sichtbarer Taskleiste" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Startseite: Poster-Raster und Mediathek" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Filmbibliothek: Poster-Wand mit Symbolleiste zum Sortieren und Verwalten" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Verlauf: Heatmap der Wiedergaben und Statistiken" width="760">
</p>

Einige Funktionen, die es wert sind, erwähnt zu werden:

- Fluent-Design-Oberfläche im Stil des Windows 11 Media-Players
- FFmpeg-Engine + Hardware-Dekodierung des Systems, breite Abdeckung von Video- und Audioformaten
- Mediathek-Verwaltung: Ordnerscan, Poster-Raster, Organisation per Drag & Drop, mehrere Bibliotheken
- Online-Postersuche (IMDb / TVmaze / Bangumi), Staffel-Poster von Serien und Ersatz durch lokale Bilder
- Untertitelunterstützung: SRT / ASS / SSA / VTT / PGS (Blu-ray-Grafikuntertitel), automatische Zuordnung und Merken externer Untertitel
- Thumbnail-Vorschau in der Fortschrittsleiste: Hybrides Verfahren aus Sample-Frames + präziser Frame-Extraktion – was du antippst, siehst du
- Screenshot mit einem Klick, optional mit eingeblendeten Untertiteln speichern
- Wiedergabeverlauf: GitHub-artige Heatmap und Statistikpanel
- Zwei Vollbild-Modi: Fenster-Vollbild (mit Taskleiste) und echtes Vollbild
- Mica- / Acrylic-Hintergrundmaterialien, frostglasartige Steuerelementeleiste
- Pfeiltasten gedrückt halten für beschleunigtes Suchen – schneller vor- und zurückspulen
- Wiedergabeliste, Merken der Wiedergabeposition, Autostart

Weitere Funktionen sind auf dem Weg!

## Installation

Lade das neueste Paket von [Releases](https://github.com/DoYouNatureQ/FilmField/releases) herunter, entpacke es und starte einfach `FilmField.exe` – keine Installation nötig. Die App ist selbstständig lauffähig (self-contained), ein zusätzliches Runtime wird nicht benötigt.

> FilmField ist ein rein lokaler Player. Er stellt keinerlei Filmquellen bereit oder enthält sie.

### Aus dem Quellcode erstellen

1. **Voraussetzungen**: Windows 10 1809+, .NET 8 SDK (mit Windows-Desktop-Workload), Windows App SDK 1.8
2. **Klonen**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Erstellen**: `dotnet build -p:Platform=x64`
4. **Ausführen**: Die Build-Ausgabe landet in `publish\`; starte `publish\FilmField.exe`

## Mitwirken

Fehlerberichte, Funktionsvorschläge und Code-Beiträge sind jederzeit willkommen.

Wenn du einen Bug findest oder eine Verbesserungsidee hast, erstelle ein Issue – Pull Requests sind ebenfalls willkommen.

## Danksagungen

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, Dekodierungskern
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, Brücke zwischen FFmpeg und der Windows-Medienpipeline
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, WinUI-3-App-Framework
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — UI-Schriftart
- Poster-Daten von [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Vollständige Urheberrechtsinformationen der Drittkomponenten findest du in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Lizenz

Dieses Projekt wird als Open Source unter der [GPL-3.0](../LICENSE)-Lizenz veröffentlicht.
