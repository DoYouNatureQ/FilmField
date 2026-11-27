# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#instalación)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | Español | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Tu cine personal.

FilmField es un reproductor de vídeo local moderno que convierte tu colección de vídeos en una hermosa biblioteca de cine privada.
Explora tus vídeos a través de un intuitivo muro de pósters de cine, administra fácilmente tus archivos multimedia locales y disfruta de una reproducción fluida. Todo en un solo lugar.

La interfaz recrea el diseño Fluent del reproductor multimedia de Windows 11: ligera, rápida y con sensación nativa. Está construido sobre WinUI 3 y FFmpeg, y es compatible con Windows 10 (1809) y versiones posteriores.

El motor de reproducción funciona con [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX), mediante la decodificación por hardware del sistema a través de Media Foundation, reproduciendo vídeo y audio de formatos habituales con un consumo bajo de recursos.

## Capturas de pantalla

<p align="center">
  <img src="promo/screenshots/player.png" alt="Reproductor: barra de controles Fluent, vista previa en miniatura de la barra de progreso y subtítulos" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Pantalla completa en ventana: reproducción a pantalla completa conservando la barra de tareas" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Inicio: cuadrícula de pósters y biblioteca multimedia" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Biblioteca de películas: muro de pósters con barra de ordenación y gestión" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Historial: mapa de calor de visualizaciones y estadísticas" width="760">
</p>

Algunas funciones que vale la pena mencionar:

- Interfaz Fluent Design, recreando el estilo del reproductor multimedia de Windows 11
- Motor FFmpeg + decodificación por hardware del sistema, amplia cobertura de formatos de vídeo y audio
- Gestión de la biblioteca multimedia: escaneo de carpetas, cuadrículas de pósters, organización por arrastrar y soltar, múltiples bibliotecas
- Búsqueda de pósters en línea (IMDb / TVmaze / Bangumi), pósters por temporada de series y reemplazo con imágenes locales
- Soporte de subtítulos: SRT / ASS / SSA / VTT / PGS (subtítulos gráficos de Blu-ray), coincidencia automática y memorización de subtítulos externos
- Vista previa en miniatura en la barra de progreso: esquema híbrido de fotogramas muestreados + extracción precisa; lo que apuntas es lo que ves
- Captura de pantalla con un clic, con opción de guardar con los subtítulos superpuestos
- Historial de reproducción: mapa de calor de visualizaciones estilo GitHub y panel de estadísticas
- Dos modos de pantalla completa: en ventana (conserva la barra de tareas) y pantalla completa real
- Materiales de fondo Mica / Acrílico, barra de controles con efecto vidrio esmerilado
- Mantén pulsadas las flechas del teclado para una exploración acelerada; avance y retroceso más cómodos
- Cola de reproducción, memoria de la posición de reproducción, inicio automático

¡Hay más funciones en camino!

## Instalación

Dirígete a [Releases](https://github.com/DoYouNatureQ/FilmField/releases) y descarga el paquete más reciente. Descomprímelo y ejecuta `FilmField.exe` directamente, sin necesidad de instalación. La aplicación se distribuye de forma autocontenida, por lo que no requiere ningún runtime adicional.

> FilmField es un reproductor puramente local. No proporciona ni incluye ninguna fuente de contenido audiovisual.

### Compilar desde el código fuente

1. **Requisitos**: Windows 10 1809+, .NET 8 SDK (con la carga de trabajo de escritorio de Windows), Windows App SDK 1.8
2. **Clonar**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Compilar**: `dotnet build -p:Platform=x64`
4. **Ejecutar**: el resultado de la compilación se genera en `publish\`; ejecuta `publish\FilmField.exe`

## Contribuir

Son bienvenidos los reportes de problemas, sugerencias de funciones y contribuciones de código.

Si encuentras un error o tienes alguna idea de mejora, abre un Issue; los Pull Request también son bienvenidos.

## Agradecimientos

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, núcleo de decodificación
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, puente entre FFmpeg y la canalización multimedia de Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, framework de aplicaciones WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — tipografía de la interfaz
- Datos de pósters de [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Consulta [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) para ver la información completa de derechos de autor de los componentes de terceros.

## Licencia

Este proyecto se publica como código abierto bajo la licencia [GPL-3.0](../LICENSE).
