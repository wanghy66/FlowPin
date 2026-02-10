# FlowPin

[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-2ea043?style=flat-square)](#)
[![Framework](https://img.shields.io/badge/.NET-8-512bd4?style=flat-square)](#)
[![UI](https://img.shields.io/badge/UI-WPF-0a66c2?style=flat-square)](#)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](./LICENSE)

FlowPin is a lightweight Windows tray app that brings browser-like middle-click auto-scroll to desktop apps (editor, terminal, file explorer, etc.).

FlowPin 是一个轻量级 Windows 托盘工具，把浏览器“中键自动滚动”的体验扩展到桌面应用（编辑器、终端、资源管理器等）。

---

## Highlights / 核心特性

- Middle-click auto-scroll with anchor-based acceleration  
  中键触发自动滚动，基于锚点距离加速
- Hold middle button + move: release to stop; click mode also supported  
  支持“按住拖动松手退出”与“点按常驻”两种模式
- Process/Class filtering (`All Apps`, `Exclude List`, `Only List`)  
  支持进程/窗口类名过滤（全部应用、排除名单、仅名单）
- Smooth overlay indicator with customizable size and color  
  指示器支持颜色、大小自定义
- Chinese/English UI  
  中英双语界面
- Portable self-contained build available  
  支持自包含单文件发布

---

## Screenshots / 截图

> Add your screenshots here  
> 这里可放设置界面和滚动指示器截图

---

## Quick Start / 快速开始

### Run directly / 直接运行

Use the published self-contained executable:

使用发布后的自包含可执行文件：

`publish/single-file-self-contained/FlowPin.App.exe`

### Build from source / 从源码构建

```powershell
dotnet build .\FlowPin.sln
```

### Publish (self-contained) / 发布（自包含）

```powershell
dotnet publish .\FlowPin.App\FlowPin.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o .\publish\single-file-self-contained
```

---

## Usage / 使用方法

1. Launch app (runs in tray).  
   启动程序（常驻托盘）。
2. Middle-click in target window to start auto-scroll.  
   在目标窗口按中键开始自动滚动。
3. Move mouse farther from anchor for faster scrolling.  
   鼠标离锚点越远，滚动越快。
4. Press middle-click again or `Esc` to stop.  
   再按中键或 `Esc` 退出。

Settings are opened from tray menu (or left-click tray icon).  
设置可通过托盘菜单（或左键托盘图标）打开。

---

## Configuration / 配置说明

- `Sensitivity` / 灵敏度
- `Dead Zone` / 死区
- `Range` / 距离尺度（影响加速曲线）
- `Gamma` / 曲线形状
- `Middle Click Debounce` / 中键防抖
- Filter mode and lists / 过滤模式与名单
- Indicator color/size / 指示器颜色与大小
- Language / 语言
- Restore defaults / 恢复默认设置

Config and log files:

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

## Roadmap / 路线图

- [ ] More robust compatibility presets for major apps  
      更多主流应用兼容预设
- [ ] Optional speed profile presets  
      可选速度档位预设
- [ ] Better diagnostics panel in UI  
      在 UI 中加入诊断面板

---

## Contributing / 贡献

Issues and pull requests are welcome.  
欢迎提交 Issue 和 PR。

Please include:

- Repro steps / 复现步骤
- Environment / 环境信息（Windows version, app version）
- Relevant logs / 相关日志（`%AppData%\FlowPin\app.log`）

---

## License / 许可证

MIT (recommended).  
建议使用 MIT 许可证（可按需调整）。
