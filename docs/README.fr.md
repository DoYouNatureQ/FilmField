# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#installation)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | Français | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Votre cinéma personnel.

FilmField est un lecteur vidéo local moderne qui transforme votre collection de vidéos en une magnifique vidéothèque personnelle.
Parcourez vos vidéos via un mur d'affiches de films intuitif, gérez facilement vos fichiers multimédias locaux et profitez d'une lecture fluide. Tout en un seul endroit.

L'interface recrée le design Fluent du lecteur multimédia intégré de Windows 11 — légère, rapide, avec un rendu natif. Elle est construite sur WinUI 3 et FFmpeg, et prend en charge Windows 10 (1809) et versions ultérieures.

Le moteur de lecture est propulsé par [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) et utilise le décodage matériel du système via Media Foundation pour lire les vidéos et l'audio des formats courants avec une faible consommation de ressources.

## Captures d'écran

<p align="center">
  <img src="promo/screenshots/player.png" alt="Lecteur : barre de contrôles Fluent, aperçu miniatures dans la barre de progression et sous-titres" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Plein écran fenêtré : lecture en plein écran avec la barre des tâches conservée" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Accueil : grille d'affiches et médiathèque" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Vidéothèque : mur d'affiches avec barre d'outils de tri et de gestion" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Historique : carte de chaleur des visionnages et statistiques" width="760">
</p>

Quelques fonctionnalités qui méritent d'être mentionnées :

- Interface en Fluent Design, recréant le style du lecteur multimédia de Windows 11
- Moteur FFmpeg + décodage matériel du système, large couverture des formats vidéo et audio
- Gestion de la médiathèque : analyse de dossiers, grilles d'affiches, organisation par glisser-déposer, plusieurs bibliothèques
- Recherche d'affiches en ligne (IMDb / TVmaze / Bangumi), affiches par saison de séries et remplacement par des images locales
- Prise en charge des sous-titres : SRT / ASS / SSA / VTT / PGS (sous-titres graphiques Blu-ray), correspondance automatique et mémorisation des sous-titres externes
- Aperçu miniatures dans la barre de progression : méthode hybride d'images échantillonnées + extraction précise des images — ce que vous survolez est ce que vous voyez
- Capture d'écran en un clic, avec possibilité d'enregistrer avec les sous-titres incrustés
- Historique de lecture : carte de chaleur des visionnages style GitHub et panneau de statistiques
- Deux modes plein écran : plein écran fenêtré (avec barre des tâches) et vrai plein écran
- Matériaux de fond Mica / Acrylique, barre de contrôles en verre dépoli
- Maintenez les flèches directionnelles enfoncées pour une navigation accélérée — avance et retour rapides plus pratiques
- File d'attente de lecture, mémorisation de la position de lecture, démarrage automatique

D'autres fonctionnalités sont en route !

## Installation

Rendez-vous sur [Releases](https://github.com/DoYouNatureQ/FilmField/releases) pour télécharger la dernière version. Extrayez l'archive et lancez simplement `FilmField.exe`, sans installation. L'application est autonome (self-contained), aucun runtime supplémentaire n'est nécessaire.

> FilmField est un lecteur purement local. Il ne fournit et n'intègre aucune source de contenu audiovisuel.

### Compiler depuis les sources

1. **Prérequis** : Windows 10 1809+, SDK .NET 8 (avec la charge de travail Windows Desktop), Windows App SDK 1.8
2. **Cloner** : `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Compiler** : `dotnet build -p:Platform=x64`
4. **Exécuter** : la sortie de compilation se trouve dans `publish\` ; lancez `publish\FilmField.exe`

## Contribuer

Les rapports de problèmes, suggestions de fonctionnalités et contributions de code sont tous les bienvenus.

Si vous trouvez un bug ou avez une idée d'amélioration, ouvrez une Issue ; les Pull Requests sont également les bienvenues.

## Remerciements

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, cœur de décodage
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, passerelle entre FFmpeg et le pipeline multimédia de Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, framework applicatif WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — police de l'interface
- Données d'affiches provenant de [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Consultez [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) pour les informations complètes de droits d'auteur des composants tiers.

## Licence

Ce projet est open source sous licence [GPL-3.0](../LICENSE).
