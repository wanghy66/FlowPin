# FlowPin

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-2ea043?style=flat-square)](#)
[![Framework](https://img.shields.io/badge/.NET-8-512bd4?style=flat-square)](#)
[![UI](https://img.shields.io/badge/UI-WPF-0a66c2?style=flat-square)](#)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](./LICENSE)

FlowPin is a lightweight Windows tray utility that brings browser-like middle-click auto-scroll to desktop apps.

FlowPin 是一款轻量级 Windows 托盘工具，把浏览器中键自动滚动体验扩展到更多桌面应用。

**Keywords / 关键词**: middle-click-auto-scroll, windows-autoscroll, tray-utility, productivity-tool, 中键滚动, 自动滚动, Windows效率工具, 终端滚动, 资源管理器滚动

---

## Highlights / 核心特性

- Middle-click auto-scroll with anchor-based acceleration  
  中键自动滚动，基于锚点距离加速
- Hold-to-scroll and click-to-toggle modes  
  支持按住拖动松手退出与点按常驻两种模式
- Scope modes: All Apps / Exclude List / Only List  
  应用范围模式：全部应用 / 排除名单 / 仅名单
- Process list + window class list filtering  
  支持进程名单和窗口类名名单过滤
- Customizable indicator (size/color)  
  指示器可自定义（大小/颜色）
- Chinese/English settings UI  
  中英双语设置界面

---


> **AI Notice / AI 说明**  
> This project was primarily generated and iterated with AI assistance (GPT-5.3).  
> 本项目主要由 AI（GPT-5.3）辅助生成与迭代完成。

---

## Quick Start / 快速开始

### Run directly / 直接运行

`publish/single-file-self-contained-v3/FlowPin.App.exe`

### Build / 构建

```powershell
dotnet build .\FlowPin.sln
```

### Publish (self-contained) / 发布（自包含）

```powershell
dotnet publish .\FlowPin.App\FlowPin.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o .\publish\single-file-self-contained-v3
```

### Publish (framework-dependent) / 发布（框架依赖）

```powershell
dotnet publish .\FlowPin.App\FlowPin.App.csproj -c Release -r win-x64 --self-contained false -o .\publish\framework-dependent-win-x64
```

---

## Usage / 使用方法

1. Launch the app (runs in tray).  
   启动程序（常驻托盘）。
2. Middle-click in a target window to start scrolling.  
   在目标窗口按中键开始滚动。
3. Move farther from anchor for faster scroll.  
   鼠标离锚点越远，滚动越快。
4. Middle-click again or press `Esc` to stop.  
   再按中键或按 `Esc` 退出。
5. Open settings from tray and click **Save** to apply changes.  
   从托盘打开设置，修改后点击“保存”生效。

---

## Config / 配置文件

- `%AppData%\FlowPin\settings.json`
- `%AppData%\FlowPin\app.log`

---

## Project Structure / 项目结构

```text
FlowPin.sln
FlowPin.App/
  Core/
  Interop/
  Models/
  Services/
  UI/
```

---

## Contributing / 贡献

Issues and pull requests are welcome.  
欢迎提交 Issue 和 PR。

---

## License / 许可证

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.  
本项目采用 MIT 许可证，详见 [LICENSE](./LICENSE)。
