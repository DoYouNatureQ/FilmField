# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#instalação)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md) | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | Português | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

Seu cinema pessoal.

FilmField é um reprodutor de vídeo local moderno que transforma sua coleção de vídeos em uma bela biblioteca de cinema particular.
Navegue pelos seus vídeos por meio de um intuitivo mural de pôsteres de cinema, gerencie facilmente seus arquivos de mídia locais e desfrute de uma reprodução fluida. Tudo em um só lugar.

A interface recria o design Fluent do reprodutor de mídia nativo do Windows 11: leve, rápido e com aparência nativa. É construído sobre WinUI 3 e FFmpeg e é compatível com o Windows 10 (1809) e versões mais recentes.

O motor de reprodução é movido pelo [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX), com decodificação por hardware do sistema por meio do Media Foundation, reproduzindo vídeo e áudio dos formatos mais comuns com baixo consumo de recursos.

## Capturas de tela

<p align="center">
  <img src="promo/screenshots/player.png" alt="Reprodutor: barra de controles Fluent, pré-visualização em miniatura da barra de progresso e legendas" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="Tela cheia em janela: reprodução em tela cheia mantendo a barra de tarefas" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="Início: grade de pôsteres e biblioteca de mídia" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="Biblioteca de filmes: mural de pôsteres com barra de ordenação e gerenciamento" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="Histórico: mapa de calor de visualizações e estatísticas" width="760">
</p>

Alguns recursos que valem destaque:

- Interface em Fluent Design, recriando o estilo do reprodutor de mídia do Windows 11
- Motor FFmpeg + decodificação por hardware do sistema, ampla cobertura de formatos de vídeo e áudio
- Gerenciamento da biblioteca de mídia: varredura de pastas, grades de pôsteres, organização por arrastar e soltar, múltiplas bibliotecas
- Busca online de pôsteres (IMDb / TVmaze / Bangumi), pôsteres por temporada de séries e substituição por imagens locais
- Suporte a legendas: SRT / ASS / SSA / VTT / PGS (legendas gráficas de Blu-ray), correspondência e memorização automáticas de legendas externas
- Pré-visualização em miniatura na barra de progresso: esquema híbrido de quadros amostrados + extração precisa; o que você aponta é o que vê
- Captura de tela com um clique, com opção de salvar com as legendas sobrepostas
- Histórico de reprodução: mapa de calor de visualizações estilo GitHub e painel de estatísticas
- Dois modos de tela cheia: em janela (mantém a barra de tarefas) e tela cheia completa
- Materiais de fundo Mica / Acrílico, barra de controles com efeito vidro fosco
- Segure as teclas de seta para navegação acelerada; avanço e retrocesso mais práticos
- Fila de reprodução, memória da posição de reprodução, inicialização automática

Mais recursos estão a caminho!

## Instalação

Acesse [Releases](https://github.com/DoYouNatureQ/FilmField/releases) e baixe o pacote mais recente. Extraia o arquivo e execute `FilmField.exe` diretamente, sem necessidade de instalação. O aplicativo é distribuído de forma autocontida e não requer nenhum runtime adicional.

> FilmField é um reprodutor puramente local. Não fornece nem inclui nenhuma fonte de conteúdo audiovisual.

### Compilar do código-fonte

1. **Requisitos**: Windows 10 1809+, .NET 8 SDK (com a carga de trabalho Windows Desktop), Windows App SDK 1.8
2. **Clonar**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **Compilar**: `dotnet build -p:Platform=x64`
4. **Executar**: o resultado da compilação vai para `publish\`; execute `publish\FilmField.exe`

## Contribuir

Relatórios de problemas, sugestões de recursos e contribuições de código são todos bem-vindos.

Se encontrar um bug ou tiver alguma ideia de melhoria, abra uma Issue; Pull Requests também são bem-vindos.

## Agradecimentos

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, núcleo de decodificação
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, ponte entre o FFmpeg e o pipeline de mídia do Windows
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, framework de aplicativos WinUI 3
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — fonte da interface
- Dados de pôsteres de [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

Consulte [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) para ver as informações completas de direitos autorais dos componentes de terceiros.

## Licença

Este projeto é open source sob a licença [GPL-3.0](../LICENSE).
