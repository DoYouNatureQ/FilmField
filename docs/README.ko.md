# FilmField

[![Release](https://img.shields.io/github/v/release/DoYouNatureQ/FilmField?style=flat-square&color=0078D4&label=release)](https://github.com/DoYouNatureQ/FilmField/releases)
[![License](https://img.shields.io/badge/license-GPL--3.0-blue?style=flat-square)](../LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat-square)](#설치)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/download)

[简体中文](../README.md) | [English](README.en.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | 한국어 | [Русский](README.ru.md) | [Español](README.es.md) | [Italiano](README.it.md) | [Português](README.pt.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Türkçe](README.tr.md) | [Lingua Latina](README.la.md)

당신만의 개인 영화관.

FilmField는 현대적인 로컬 동영상 플레이어로, 영상 컬렉션을 아름다운 개인 영화 라이브러리로 바꾸는 것을 목표로 합니다.
직관적인 영화 포스터 월에서 영상을 탐색하고, 로컬 미디어 파일을 손쉽게 관리하며, 부드러운 재생 경험을 누려보세요. 모든 것을 한곳에서.

인터페이스는 Windows 11 기본 미디어 플레이어의 Fluent 디자인을 재현하여 가볍고 빠르며 네이티브 느낌을 줍니다. WinUI 3와 FFmpeg 기반으로 만들어졌으며, Windows 10(1809) 이상을 지원합니다.

재생 엔진은 [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX)로 구동되며, 시스템 Media Foundation 하드웨어 디코딩을 통해 주요 포맷의 영상과 오디오를 낮은 자원 사용량으로 재생합니다.

## 스크린샷

<p align="center">
  <img src="promo/screenshots/player.png" alt="플레이어 화면: Fluent 컨트롤 바, 시크 바 썸네일 미리보기 및 자막" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/fullscreen.png" alt="창 전체 화면: 작업 표시줄을 유지하는 전체 화면 재생" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/library.png" alt="홈: 포스터 그리드와 미디어 라이브러리" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/movies.jpg" alt="영화 라이브러리: 포스터 월과 정렬·관리 도구 모음" width="760">
</p>
<p align="center">
  <img src="promo/screenshots/history.png" alt="시청 기록: 시청 히트맵과 통계" width="760">
</p>

소개할 만한 기능들:

- Fluent Design 인터페이스, Windows 11 미디어 플레이어 스타일 재현
- FFmpeg 엔진 + 시스템 하드웨어 디코딩, 폭넓은 영상·오디오 포맷 지원
- 미디어 라이브러리 관리: 폴더 스캔, 포스터 그리드, 드래그 앤 드롭 정리, 다중 라이브러리 분류
- 온라인 포스터 검색(IMDb / TVmaze / Bangumi), 시즌별 포스터 및 로컬 이미지 교체 지원
- 자막 지원: SRT / ASS / SSA / VTT / PGS(Blu-ray 그래픽 자막), 외부 자막 자동 매칭 및 기억
- 시크 바 썸네일 미리보기: 샘플링 프레임 + 정밀 프레임 추출의 하이브리드 방식, 가리킨 곳이 곧 보이는 화면
- 원클릭 스크린샷, 자막을 겹쳐 저장 가능
- 시청 기록: GitHub 스타일 시청 히트맵과 통계 패널
- 두 가지 전체 화면 모드: 창 전체 화면(작업 표시줄 유지)과 완전 전체 화면
- Mica / Acrylic 배경 소재, 컨트롤 바 서리 유리 효과
- 방향키 길게 눌러 배속 탐색, 빠른 되감기·빨리 감기
- 재생 대기열, 재생 위치 기억, 시작 프로그램 등록

더 많은 기능이 준비 중입니다!

## 설치

[Releases](https://github.com/DoYouNatureQ/FilmField/releases)에서 최신 버전 패키지를 다운로드하고, 압축을 푼 뒤 `FilmField.exe`를 실행하기만 하면 됩니다. 설치가 필요 없습니다. 앱은 자체 포함 배포 방식이라 별도의 런타임도 필요하지 않습니다.

> FilmField는 순수 로컬 플레이어이며, 어떠한 영상 소스도 제공하거나 내장하지 않습니다.

### 소스에서 빌드

1. **필요 환경**: Windows 10 1809+, .NET 8 SDK(Windows 데스크톱 워크로드 포함), Windows App SDK 1.8
2. **클론**: `git clone https://github.com/DoYouNatureQ/FilmField.git`
3. **빌드**: `dotnet build -p:Platform=x64`
4. **실행**: 빌드 결과물은 `publish\`에 출력됩니다. `publish\FilmField.exe`를 실행하세요

## 기여하기

버그 보고, 기능 제안, 코드 기여를 모두 환영합니다.

버그를 발견했거나 개선 제안이 있다면 Issues로 알려주세요. Pull Request도 환영합니다.

## 감사의 말

- [FFmpeg](https://ffmpeg.org) — LGPL-2.1+, 디코딩 코어
- [FFmpegInteropX](https://github.com/ffmpeginteropx/FFmpegInteropX) — Apache-2.0, FFmpeg와 Windows 미디어 파이프라인의 브리지
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) — MIT, WinUI 3 앱 프레임워크
- [HarmonyOS Sans](https://developer.huawei.com/consumer/cn/design/) — UI 글꼴
- 포스터 데이터: [IMDb](https://www.imdb.com), [TVmaze](https://www.tvmaze.com), [Bangumi](https://bgm.tv)

타사 구성 요소의 전체 저작권 정보는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)를 참고하세요.

## 라이선스

이 프로젝트는 [GPL-3.0](../LICENSE) 라이선스로 오픈소스로 공개되어 있습니다.
