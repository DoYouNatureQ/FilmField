# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#kurulum)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | Türkçe | [Lingua Latina](README.la.md)

Kişisel sinemanız.

FilmField, video koleksiyonunuzu güzel bir kişisel film arşivine dönüştürmeyi amaçlayan modern bir yerel video oynatıcısıdır.
Sezgisel bir film posteri duvarı üzerinden videolarınıza göz atın, yerel medya dosyalarınızı kolayca yönetin ve akıcı bir oynatma deneyiminin tadını çıkarın. Her şey tek bir yerde.

Arayüz, Windows 11'in yerleşik medya oynatıcısının Fluent tasarımını yeniden üretir — hafif, hızlı ve yerel hissiyatlı. WinUI 3 ve FFmpeg üzerine inşa edilmiştir; Windows 10 (1809) ve üzerini destekler.

Oynatma motoru [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) tarafından çalıştırılır ve sistemdeki Media Foundation üzerinden donanım hızlandırmalı kod çözme ile yaygın formatlardaki video ve sesleri düşük kaynak kullanımıyla oynatır.

## Ekran Görüntüleri

<p align="center">
  <img src="promo/screenshots/player.png" alt="Oynatıcı: Fluent kontrol çubuğu, ilerleme çubuğunda küçük resim önizleme ve altyazılar" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Pencereli tam ekran: görev çubuğunun korunduğu tam ekran oynatma" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Giriş sayfası: poster ızgarası ve medya arşivi" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Film arşivi: poster duvarı ile sıralama ve yönetim araç çubuğu" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Geçmiş: izleme ısı haritası ve istatistikler" width="760">
</p>

Anlamaya değer bazı özellikler:

- Fluent Design arayüz, Windows 11 medya oynatıcı stilini yeniden üretir
- FFmpeg motoru + sistem donanım kod çözme, geniş video ve ses formatı desteği
- Medya arşivi yönetimi: klasör taraması, poster ızgaraları, sürükle-bırak düzenleme, çoklu arşiv
- Çevrimiçi poster arama (IMDb / TVmaze / Bangumi), dizi sezon posterleri ve yerel görselle değiştirme desteği
- Altyazı desteği: SRT / ASS / SSA / VTT / PGS (Blu-ray grafik altyazıları), harici altyazıların otomatik eşleşmesi ve hatırlanması
- İlerleme çubuğunda küçük resim önizleme: örneklenmiş kareler + hassas kare çıkarmanın melez yöntemi — üzerine geldiğiniz an ne görüyorsanız odur
- Tek tıkla ekran görüntüsü, altyazı üstte olacak şekilde kaydetme seçeneğiyle
- İzleme geçmişi: GitHub tarzı izleme ısı haritası ve istatistik paneli
- İki tam ekran modu: pencereli tam ekran (görev çubuğu korunur) ve gerçek tam ekran
- Mica / Acrylic arka plan malzemeleri, buzlu cam efektli kontrol çubuğu
- Yön tuşlarını basılı tutarak hızlandırılmış tarama; ileri ve geri sarma daha pratik
- Oynatma kuyruğu, oynatma konumunun hatırlanması, başlangıçta otomatik açılma

Daha fazlası yolda!

## Kurulum

Son sürümü [Releases](https://github.com/DoYouNatureQ/FilmField/releases) sayfasından indirin, arşivi çıkarın ve `FilmField.exe` dosyasını doğrudan çalıştırın — kurulum gerekmez. Uygulama kendi içinde tamdır (self-contained), ek bir çalışma zamanı gerekmez.

> FilmField tamamen yerel bir oynatıcıdır. Hiçbir film kaynağı sağlamaz veya içermez.

### Kaynak koddan derleme

1. **Gereksinimler**: Windows 10 1809+, .NET 8 SDK (Windows masaüstü iş yüküyle birlikte), Windows App SDK 1.8
2. **Klonlama**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Derleme**: `dotnet build -p:Platform=x64`
4. **Çalıştırma**: derleme çıktısı `publish\` klasörüne gider; `publish\FilmField.exe` dosyasını çalıştırın

## Katkıda Bulunma

Hata bildirimleri, özellik önerileri ve kod katkıları memnuniyetle karşılanır.

Bir hata bulursanız ya da geliştirme fikriniz varsa bir Issue açın; Pull Request'ler de memnuniyetle karşılanır.

## Teşekkürler

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, kod çözme çekirdeği
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, FFmpeg ile Windows medya hattı arasındaki köprü
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, WinUI 3 uygulama çerçevesi
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — arayüz yazı tipi
- Poster verileri: [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Üçüncü taraf bileşenlerin tam telif hakkı bilgileri için [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) dosyasına bakın.

## Lisans

Bu proje [GPL-3.0](../LICENSE) lisansı altında açık kaynak olarak yayımlanmaktadır.
