# FilmField 版权与致谢声明

发布日期：2026-11-26

## FilmField 本体

Copyright (c) 2026 NatureQ（GitHub: DoYouNatureQ）。保留所有权利。

FilmField 是一款纯本地视频播放器：不内置、不提供、不指向任何影片资源（片源），
也不参与任何影视内容的分发。应用播放的一切内容均来自用户自己的本地媒体文件，
相关内容的著作权归原权利人所有，与 FilmField 无关。

## 第三方组件

### FFmpeg（avcodec / avformat / avutil / swresample / swscale / swscale / avdevice / avfilter）

- 版权：Copyright (c) FFmpeg contributors 及各组件原作者
- 许可：LGPL-2.1-or-later（随 FFmpegInteropX 官方构建，另静态编入 zlib 与 MIT 许可组件）
- 主页：https://ffmpeg.org
- 许可文本：https://www.gnu.org/licenses/old-licenses/lgpl-2.1.html
- 源代码：FFmpeg 为自由软件，任何人可依 LGPL-2.1 在 https://ffmpeg.org/download.html
  获取源代码。本应用以动态链接方式使用，未对 FFmpeg 做任何修改。

### FFmpegInteropX

- 版权：Copyright (c) FFmpegInteropX contributors（fork 自 Microsoft/FFmpegInterop）
- 许可：Apache License 2.0
- 仓库：https://github.com/ffmpeginteropx/FFmpegInteropX
- 许可文本：https://www.apache.org/licenses/LICENSE-2.0

### Windows App SDK / WinUI 3

- 版权：Copyright (c) Microsoft Corporation
- 许可：MIT License
- 仓库：https://github.com/microsoft/WindowsAppSDK

### HarmonyOS Sans SC 字体（Medium / Bold / Regular）

- 版权：Copyright 2021 Huawei Device Co., Ltd.（由华为与汉仪字库联合设计）
- 许可：HarmonyOS Sans Fonts License Agreement（鸿蒙字体许可协议）
- 条款要点：个人与商业用途均可免费使用、嵌入应用随软件分发；
  不得将字体文件单独出售或变相收费；不得对字体文件本身进行修改、
  拆解或反向工程后发布衍生字体。FilmField 仅原样打包、未做任何改动。
- 官方页面：https://developer.huawei.com/consumer/cn/design/

### 系统图标

界面符号图标（FontIcon）使用 Windows 系统自带的 Segoe Fluent Icons 字体，
版权 Microsoft Corporation，随操作系统提供，本应用未另行分发。

## 海报元数据来源（媒体库联网找海报功能）

- IMDb：https://www.imdb.com —— 数据版权归 IMDb.com, Inc. 所有
- TVmaze：https://www.tvmaze.com —— 依其 API 条款免费使用并注明出处
- Bangumi：https://bgm.tv —— 依 Bangumi API 使用条款使用

上述服务返回的海报图片与元数据，其著作权归各自权利人所有，
FilmField 仅按用户请求临时获取并缓存用于本地展示。

---

以上第三方组件各自依其许可证使用。完整许可证文本可在上述链接获取；
如需随包分发的许可证副本，请查看各组件官方仓库。
