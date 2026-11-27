# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#installazione)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | Italiano | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Il tuo cinema personale.

FilmField è un lettore video locale moderno che trasforma la tua collezione di video in una splendida biblioteca cinematografica privata.
Sfoglia i tuoi video attraverso un intuitivo muro di poster di film, gestisci facilmente i tuoi file multimediali locali e goditi una riproduzione fluida. Tutto in un unico posto.

L'interfaccia ricrea il design Fluent del lettore multimediale di Windows 11: leggera, veloce, dall'aspetto nativo. È costruito su WinUI 3 e FFmpeg e supporta Windows 10 (1809) e versioni successive.

Il motore di riproduzione è basato su [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) e, tramite la decodifica hardware del sistema con Media Foundation, riproduce video e audio dei formati più diffusi con un consumo minimo di risorse.

## Screenshot

<p align="center">
  <img src="promo/screenshots/player.png" alt="Lettore: barra dei controlli Fluent, anteprima miniature sulla barra di avanzamento e sottotitoli" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Schermo intero a finestra: riproduzione a schermo intero con barra delle applicazioni visibile" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Home: griglia di poster e libreria multimediale" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Libreria film: muro di poster con barra di ordinamento e gestione" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Cronologia: mappa di calore delle visualizzazioni e statistiche" width="760">
</p>

Alcune funzionalità che vale la pena menzionare:

- Interfaccia in Fluent Design, che ricrea lo stile del lettore multimediale di Windows 11
- Motore FFmpeg + decodifica hardware di sistema, ampia copertura di formati video e audio
- Gestione della libreria multimediale: scansione delle cartelle, griglie di poster, organizzazione tramite trascinamento, più librerie
- Ricerca poster online (IMDb / TVmaze / Bangumi), poster per stagione delle serie e sostituzione con immagini locali
- Supporto dei sottotitoli: SRT / ASS / SSA / VTT / PGS (sottotitoli grafici Blu-ray), abbinamento e memorizzazione automatici dei sottotitoli esterni
- Anteprima miniature sulla barra di avanzamento: schema ibrido di fotogrammi campionati + estrazione precisa; ciò che indichi è ciò che vedi
- Screenshot con un clic, salvabile anche con i sottotitoli sovrapposti
- Cronologia delle visualizzazioni: mappa di calore in stile GitHub e pannello di statistiche
- Due modalità a schermo intero: a finestra (con barra delle applicazioni) e schermo intero completo
- Materiali di sfondo Mica / Acrilico, barra dei controlli con effetto vetro smerigliato
- Tieni premuti i tasti freccia per lo scorrimento accelerato; avanzamento e riavvolgimento più comodi
- Coda di riproduzione, memorizzazione della posizione, avvio automatico

Altre funzionalità sono in arrivo!

## Installazione

Vai su [Releases](https://github.com/DoYouNatureQ/FilmField/releases) e scarica l'ultimo pacchetto. Estrai l'archivio ed esegui direttamente `FilmField.exe`, senza bisogno di installazione. L'app è distribuita in forma autonoma (self-contained), quindi non richiede alcun runtime aggiuntivo.

> FilmField è un lettore puramente locale. Non fornisce né include alcuna fonte di contenuti audiovisivi.

### Compilare dai sorgenti

1. **Requisiti**: Windows 10 1809+, .NET 8 SDK (con il carico di lavoro Windows Desktop), Windows App SDK 1.8
2. **Clonare**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Compilare**: `dotnet build -p:Platform=x64`
4. **Eseguire**: l'output della compilazione va in `publish\`; esegui `publish\FilmField.exe`

## Contribuire

Segnalazioni di problemi, suggerimenti di funzionalità e contributi di codice sono tutti benvenuti.

Se trovi un bug o hai un'idea di miglioramento, apri una Issue; anche i Pull Request sono benvenuti.

## Ringraziamenti

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, motore di decodifica
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, ponte tra FFmpeg e la pipeline multimediale di Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, framework applicativo WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — carattere dell'interfaccia
- Dati dei poster da [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Per le informazioni complete sul copyright dei componenti di terze parti, consulta [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Licenza

Questo progetto è open source sotto licenza [GPL-3.0](../LICENSE).
