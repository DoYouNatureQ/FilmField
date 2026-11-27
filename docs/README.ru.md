# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#Установка)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | Русский | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Ваш личный кинотеатр.

FilmField — современный локальный видеоплеер, призванный превратить вашу коллекцию видео в красивую частную видеотеку.
Просматривайте видео через интуитивную стену киноафиш, легко управляйте локальными медиафайлами и наслаждайтесь плавным воспроизведением. Всё в одном месте.

Интерфейс воссоздаёт Fluent-дизайн встроенного медиапроигрывателя Windows 11 — легковесный, быстрый, с нативным ощущением. Построен на WinUI 3 и FFmpeg, поддерживает Windows 10 (1809) и новее.

Движок воспроизведения работает на [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) с аппаратным декодированием через системный Media Foundation — низкое потребление ресурсов при воспроизведении видео и аудио популярных форматов.

## Скриншоты

<p align="center">
  <img src="promo/screenshots/player.png" alt="Проигрыватель: панель управления Fluent, предпросмотр миниатюр на шкале прогресса и субтитры" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Оконный полноэкранный режим: воспроизведение во весь экран с сохранением панели задач" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Главная: сетка постеров и медиатека" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Фильмотека: стена постеров и панель сортировки и управления" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="История: тепловая карта просмотров и статистика" width="760">
</p>

Некоторые возможности, о которых стоит упомянуть:

- Интерфейс Fluent Design, воссоздающий стиль медиапроигрывателя Windows 11
- Движок FFmpeg + аппаратное декодирование системы, широкое покрытие видео- и аудиоформатов
- Управление медиатекой: сканирование папок, сетка постеров, сортировка перетаскиванием, несколько библиотек
- Поиск постеров в интернете (IMDb / TVmaze / Bangumi), постеры по сезонам сериалов и замена локальными изображениями
- Поддержка субтитров: SRT / ASS / SSA / VTT / PGS (графические субтитры Blu-ray), автоподбор и запоминание внешних субтитров
- Предпросмотр миниатюр на шкале прогресса: гибрид выборочных кадров и точного извлечения — что наведено, то и видно
- Снимок экрана в один клик, с возможностью сохранить вместе с субтитрами
- История просмотров: тепловая карта в стиле GitHub и панель статистики
- Два полноэкранных режима: оконный (с сохранением панели задач) и настоящий полный экран
- Материалы Mica / Acrylic, «морозное стекло» панели управления
- Удержание клавиш со стрелками для ускоренной перемотки — быстрее и удобнее
- Очередь воспроизведения, запоминание позиции, автозапуск

Ещё больше функций уже в пути!

## Установка

Скачайте последнюю версию архива со страницы [Releases](https://github.com/DoYouNatureQ/FilmField/releases), распакуйте и просто запустите `FilmField.exe` — установка не требуется. Приложение развёртывается автономно, дополнительная среда выполнения не нужна.

> FilmField — чисто локальный плеер. Он не предоставляет и не содержит никаких источников фильмов.

### Сборка из исходников

1. **Требования**: Windows 10 1809+, .NET 8 SDK (с рабочей нагрузкой Windows Desktop), Windows App SDK 1.8
2. **Клонирование**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Сборка**: `dotnet build -p:Platform=x64`
4. **Запуск**: результаты сборки выводятся в `publish\`, запустите `publish\FilmField.exe`

## Участие в разработке

Приветствуются сообщения об ошибках, предложения новых функций и вклад в код.

Если вы нашли баг или у вас есть идея по улучшению, напишите в Issues — Pull Request тоже приветствуются.

## Благодарности

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, ядро декодирования
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, мост между FFmpeg и медиаконвейером Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, фреймворк приложений WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — шрифт интерфейса
- Данные постеров: [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Полные сведения об авторских правах сторонних компонентов — в файле [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Лицензия

Проект распространяется как открытый код под лицензией [GPL-3.0](../LICENSE).
