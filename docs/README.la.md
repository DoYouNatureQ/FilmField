# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#installatio)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | Lingua Latina

Cinematographum tuum privatum.

FilmField est lector pellicularum localis hodiernus, qui collectionem tuam pellicularum in bibliothecam cinematographicam pulchram privata convertit.
Pelliculas per parietem posterorum cinematographicorum intuitivos perlustra, tabellas mediorum localium facile cura, lusionemque lenem fruito. Omnia in uno loco.

Interfacies formam "Fluent" lusoris mediorum Windows 11 imitatur — levis, celer, sensu nativo. Super WinUI 3 cum FFmpeg aedificatur, Windows 10 (1809) et posteriorem sustinens.

Motor lusionis ab [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) agitur, per "Media Foundation" systematis duro decodificans, pelliculas et sonos formae vulgaris parvo usu opum reproducens.

## Imagines

<p align="center">
  <img src="promo/screenshots/player.png" alt="Lector: barra imperandi Fluent, praevisiones minutae in barra progressionis et subtitula" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Modus toti faciei fenestralis: lusio toti faciei cum barra operum servata" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Domum: reticulum posterorum et bibliotheca mediorum" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Bibliotheca pellicularum: paries posterorum cum barra ordinandi et administrandi" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Historia: mapa caloris visionum et statistica" width="760">
</p>

Nonnullae functiones memorabiles:

- Interfacies "Fluent Design", formam lusoris mediorum Windows 11 imitans
- Motor FFmpeg + dura decodificatio systematis, multas formas pellicularum sonorumque tegens
- Administratio bibliothecae mediorum: nomenclatura laorum, reticula posterorum, ordinatio trahendo, plures bibliothecae
- Quaestio posterorum in interrete (IMDb / TVmaze / Bangumi), posteri temporum serierum et substitutio imaginum localium
- Subtitula sustentata: SRT / ASS / SSA / VTT / PGS (subtitula graphica Blu-ray), convenientia externorum subtitulorum automata et memoria
- Praevisiones minutae in barra progressionis: consilium hybridum frammatum exemplarium + extractionis accuratae — quod tangis, id videtur
- Screenshot uno tactu, etiam cum subtitulis superpositis servabile
- Historia lusionis: heatmap more GitHub et tabula statistica
- Duo modi toti faciei: modus fenestralis (cum barra operum servata) et modus vere plenus
- Materiae fundi Mica / Acrylic, barra imperandi vitreo froso
- Sagittis diu pressis celerius praevolve; praevolutio et retrolusio leniores
- Series lusionis, memoria loci lusionis, initium automaticum

Plures functiones adhuc in itinere sunt!

## Installatio

I ad [Releases](https://github.com/DoYouNatureQ/FilmField/releases) et arcam compressam novissimam deprome. Eam effinge ac `FilmField.exe` simpliciter cura — installatione non opus est. Programma se ipsum continet, ulla instrumenta runtime extra non requiritur.

> FilmField lector mere localis est. Nullas fontes pellicularum praebet vel continet.

### Ex fontibus aedificare

1. **Postulata**: Windows 10 1809+, .NET 8 SDK (cum onere "Windows Desktop"), Windows App SDK 1.8
2. **Clonare**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Aedificare**: `dotnet build -p:Platform=x64`
4. **Currere**: exitus aedificationis in `publish\` ponitur; cura `publish\FilmField.exe`

## Contribuere

Relationes problematum, suggestiones functionum, contributiones codicis gratissime accipiuntur.

Si errorem invenis vel meliorationem proponis, per Issues nuntia — Pull Request etiam gratae sunt.

## Gratiae

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, cor decodificationis
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, pons inter FFmpeg et viam mediorum Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, compages programmatum WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — littera interfaciei
- Data posterorum ex [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

In [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) integra informationes de iuribus componentium tertiorum invenies.

## Licentia

Hoc programma sub licentia [GPL-3.0](../LICENSE) codice aperto divulgatur.
