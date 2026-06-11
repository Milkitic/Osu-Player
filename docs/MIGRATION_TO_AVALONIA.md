# Osu-Player WPF → Avalonia 迁移手册

> 目标:将 `OsuPlayer` (WPF, net10.0-windows) 迁移到原生 Avalonia 12(.NET 10, Windows 优先)
>
> 业务层(`OsuPlayer.Core / Data / Media.Audio / Media.Lyric / Playback / Shared`)保留不动,仅重写 `OsuPlayer` + `OsuPlayer.Presentation`。

---

## 目录

1. [总体架构与项目结构](#1-总体架构与项目结构)
2. [依赖映射](#2-依赖映射)
3. [命名空间/语法映射](#3-命名空间语法映射)
4. [资源与样式迁移](#4-资源与样式迁移)
5. [窗口/页面/UserControl 映射表](#5-窗口页面usercontrol-映射表)
6. [动画 (Storyboard) 迁移](#6-动画-storyboard-迁移)
7. [控件模板 (ControlTemplate) 迁移](#7-控件模板-controltemplate-迁移)
8. [行为 (Behaviors) 迁移](#8-行为-behaviors-迁移)
9. [转换器 (IValueConverter) 迁移](#9-转换器-ivalueconverter-迁移)
10. [特殊功能:着色器/视频/托盘/Markdown](#10-特殊功能着色器视频托盘markdown)
11. [DI 与启动流程改造](#11-di-与启动流程改造)
12. [P/Invoke 与平台代码](#12-pinvoke-与平台代码)
13. [Phase 1.5 基线建立步骤](#13-phase-15-基线建立步骤)
14. [逐步执行清单](#14-逐步执行清单)
15. [回退与风险控制](#15-回退与风险控制)

---

## 1. 总体架构与项目结构

### 1.1 当前 WPF 结构

```
src/
├── OsuPlayer.Core/                  # 业务(无 WPF 依赖,保留)
├── OsuPlayer.Data/                  # 数据访问(无 WPF 依赖,保留)
├── OsuPlayer.Media.Audio/           # 音频(NAudio,无 WPF 依赖,保留)
├── OsuPlayer.Media.Lyric/           # 歌词解析(无 WPF 依赖,保留)
├── OsuPlayer.Playback/              # 播放(无 WPF 依赖,保留)
├── OsuPlayer.Shared/                # 共享(无 WPF 依赖,保留)
├── OsuPlayer.Abstractions/          # 接口(无 WPF 依赖,保留)
├── OsuPlayer.Presentation/          # WPF MVVM 辅助(FrameNavigationService/Interaction)
│                                    #   → 重写或重定向到 Avalonia 命名空间
└── OsuPlayer/                       # WPF UI 入口(完整重写)
    ├── App.xaml(.cs)                # 入口
    ├── Windows/                     # 7 个 Window
    ├── Pages/                       # 7 个 Page
    ├── UserControls/                # 12 个 UserControl
    ├── Styles/                      # 20 个 ResourceDictionary/Style
    ├── Converters/                  # 22 个 IValueConverter
    ├── Services/                    # 5 个服务(部分 WPF 耦合)
    ├── UiComponents/                # 自定义控件 + Storyboard 动画
    ├── ViewModels/                  # 13 个 ViewModel(可复用)
    ├── lang/                        # 国际化
    ├── Resources/                   # 资源(图片/字体/着色器DLL)
    └── extensions/                  # 插件(ffmpeg/oppai/ShaderEffects)
```

### 1.2 目标 Avalonia 结构

```
src/
├── OsuPlayer.Core/                  # 保留
├── OsuPlayer.Data/                  # 保留
├── OsuPlayer.Media.Audio/           # 保留
├── OsuPlayer.Media.Lyric/           # 保留
├── OsuPlayer.Playback/              # 保留
├── OsuPlayer.Shared/                # 保留
├── OsuPlayer.Abstractions/          # 保留
├── OsuPlayer.Presentation/          # 保留 ViewModelBase/Interaction 抽象
│                                    #   移除 WPF FrameNavigationService 改为 Avalonia 版
├── OsuPlayer/                       # WPF 旧实现(保留,作为对照)
└── OsuPlayer.Avalonia/              # 新建: Avalonia 12 + MVVM
    ├── App.axaml(.cs)               # 入口
    ├── Program.cs                   # 启动
    ├── ViewLocator.cs               # 视图定位器
    ├── Windows/                     # Avalonia Window(原 Windows/)
    ├── Views/Pages/                 # UserControl 化(原 Pages/)
    ├── Views/UserControls/          # UserControl(原 UserControls/)
    ├── Controls/                    # 自定义 TemplatedControl(原 UiComponents/)
    ├── Styles/                      # ResourceDictionary(原 Styles/)
    ├── Converters/                  # IValueConverter(原 Converters/)
    ├── Services/                    # 服务(原 Services/,无 UI 依赖的服务复用)
    ├── Skia/                        # 自定义着色器(DrawOperation)
    ├── Tray/                        # 托盘包装
    ├── Video/                       # FFmpegVideoPlayer 集成
    ├── ViewModels/                  # ViewModel(原 ViewModels/,可复用)
    ├── lang/                        # lang/default.axaml
    ├── Resources/                   # 图片/字体
    └── Assets/                      # 图标
```

### 1.3 命名空间映射

| WPF | Avalonia |
|---|---|
| `OsuPlayer` | `OsuPlayer.Avalonia` |
| `OsuPlayer.Pages` | `OsuPlayer.Avalonia.Views.Pages` |
| `OsuPlayer.Pages.Settings` | `OsuPlayer.Avalonia.Views.Pages.Settings` |
| `OsuPlayer.Windows` | `OsuPlayer.Avalonia.Windows` |
| `OsuPlayer.UserControls` | `OsuPlayer.Avalonia.Views.UserControls` |
| `OsuPlayer.UiComponents` | `OsuPlayer.Avalonia.Controls` |
| `OsuPlayer.Styles` | `OsuPlayer.Avalonia.Styles` |
| `OsuPlayer.Converters` | `OsuPlayer.Avalonia.Converters` |
| `OsuPlayer.Services` | `OsuPlayer.Avalonia.Services` (或直接 `OsuPlayer.Services` 复用) |
| `OsuPlayer.ViewModels` | `OsuPlayer.Avalonia.ViewModels` |

> **ViewModel 复用策略**:`OsuPlayer.Avalonia.ViewModels` 直接 `using OsuPlayer.ViewModels;` 或文件级包含,优先保留原 ViewModel,避免大规模改写。

---

## 2. 依赖映射

### 2.1 包引用映射(OsuPlayer.Avalonia.csproj)

| 原 WPF 包 | Avalonia 替代 | 备注 |
|---|---|---|
| `<UseWPF>true</UseWPF>` | `Avalonia 12.0.4` + `Avalonia.Desktop 12.0.4` | 框架核心 |
| `CommunityToolkit.Mvvm 8.4.2` | `CommunityToolkit.Mvvm 8.4.1+` | 沿用 |
| `Microsoft.Xaml.Behaviors.Wpf 1.1.142` | `Avalonia.Xaml.Behaviors 11.x`(适配 Avalonia 12) | 命令/事件触发 |
| `Hardcodet.NotifyIcon.Wpf 2.0.1` | 内置 `Avalonia.Controls.TrayIcon` (11.1+) | 跨平台托盘 |
| `FFME.Windows 7.0.361-beta` | `FFmpegVideoPlayer.Avalonia 0.x` | 视频播放(支持 mp4/webm) |
| `Markdig.Wpf 0.5.0.1` | **暂不实现**(用 TextBlock 显示纯文本) | 用户确认 |
| `FFmpeg.AutoGen 7.0.0` | 保留(`net10.0` 自动兼容) | 原生库绑定 |
| `NAudio 2.3.0` | 保留 | Windows only,但目标就是 Windows |
| `Microsoft.Extensions.DependencyInjection 10.0.8` | 保留 | DI 容器 |
| `NLog 6.1.3` | 保留 | 日志 |
| `Microsoft-WindowsAPICodePack-Shell 1.1.5` | 保留(Windows only) | Shell API |
| `Dapper 2.1.79` / `Coosu.Beatmap 2.5.1` | 保留 | 业务层 |

### 2.2 原生 DLL 处理

| WPF 端 | Avalonia 端 |
|---|---|
| `extensions\plugins\ShaderEffects.dll` (WPF `ShaderEffect`) | **删除**;改用 Skia 自定义 DrawOperation |
| `extensions\plugins\ffmpeg\win-x64/*.dll` | **保留**;由 `FFmpegVideoPlayer.Avalonia` 加载 |
| `extensions\plugins\oppai-ng/oppai.dll` | **保留**;P/Invoke 直接调用 |

### 2.3 业务层 ProjectReference

OsuPlayer.Avalonia.csproj 添加:
```xml
<ItemGroup>
  <ProjectReference Include="..\OsuPlayer.Core\OsuPlayer.Core.csproj" />
  <ProjectReference Include="..\OsuPlayer.Data\OsuPlayer.Data.csproj" />
  <ProjectReference Include="..\OsuPlayer.Media.Audio\OsuPlayer.Media.Audio.csproj" />
  <ProjectReference Include="..\OsuPlayer.Media.Lyric\OsuPlayer.Media.Lyric.csproj" />
  <ProjectReference Include="..\OsuPlayer.Playback\OsuPlayer.Playback.csproj" />
  <ProjectReference Include="..\OsuPlayer.Presentation\OsuPlayer.Presentation.csproj" />
  <ProjectReference Include="..\OsuPlayer.Shared\OsuPlayer.Shared.csproj" />
</ItemGroup>
```

> 业务层项目目标框架是 `net10.0`(非 `-windows`),可直接被 Avalonia 项目引用。`OsuPlayer.Media.Audio` 引用了 `Microsoft.WindowsAPICodePack-*` 的 `net10.0-windows` 行为,需检查:若需保持跨层兼容,可能需保留 `net10.0-windows` 或抽离到 Windows-only 子项目。

---

## 3. 命名空间/语法映射

### 3.1 根命名空间

```xml
<!-- WPF -->
xmlns="http://schemas.microsoft.com/winfx/2006/xaml"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"

<!-- Avalonia -->
xmlns="https://github.com/avaloniaui"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"   <!-- 仍用 x: 前缀 -->
```

### 3.2 本地命名空间

```xml
<!-- WPF -->
xmlns:local="clr-namespace:OsuPlayer.UserControls"
xmlns:conv="clr-namespace:OsuPlayer.Converters"
xmlns:cfg="clr-namespace:Milki.Extensions.Configuration"

<!-- Avalonia -->
xmlns:local="using:OsuPlayer.Avalonia.Views.UserControls"
xmlns:conv="using:OsuPlayer.Avalonia.Converters"
<!-- 或保留对原项目的引用: -->
xmlns:cfg="clr-namespace:Milki.Extensions.Configuration"
```

> `clr-namespace:` 在 Avalonia 12 中仍受支持(向 WPF 兼容),但官方推荐 `using:`。

### 3.3 x:DataType 编译绑定

```xml
<!-- WPF 无需 -->
<Window x:Class="...">

<!-- Avalonia 强制推荐 -->
<Window x:Class="..." x:DataType="vm:MainWindowViewModel">
```

在 `OsuPlayer.Avalonia.csproj` 中已启用 `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`,需为每个绑定根添加 `x:DataType`。

### 3.5 标题栏 / WindowChrome 迁移

`OsuPlayer` 的主窗口和设置窗口在 WPF 中都不是原生标题栏,而是 `WindowChrome + CaptionHeight + UseAeroCaptionButtons=False` 配合页面内自绘按钮实现。迁移到 Avalonia 时,这里必须保持“继续使用自绘标题栏”,不要退回系统标题栏,否则视觉和交互都会偏离原项目。

**最低改动迁移规则**

1. WPF 的 `WindowChrome` 迁移到 Avalonia 时,窗口级别统一使用:
```xml
WindowDecorations="None"
ExtendClientAreaToDecorationsHint="True"
```

> 当前仓库锁定的是 Avalonia `12.0.4`,`ExtendClientAreaChromeHints` 并不可用;这里不要为了“概念完整”引入不存在的属性。对本项目而言,`WindowDecorations="None"` 已负责隐藏原生标题栏,`ExtendClientAreaToDecorationsHint="True"` 负责把自定义标题栏接入窗口拖拽/系统按钮语义。

2. WPF 的 `CaptionHeight` 直接映射到 Avalonia 的:
```xml
ExtendClientAreaTitleBarHeightHint="40"   <!-- MainWindow -->
ExtendClientAreaTitleBarHeightHint="32"   <!-- ConfigWindow / BeatmapInfoWindow -->
```

3. 自定义标题栏根节点必须标记:
```xml
WindowDecorationProperties.ElementRole="TitleBar"
```
否则拖拽、双击最大化、系统吸附(snap)等行为可能不完整。

4. 最小化 / 最大化 / 关闭按钮必须分别标记:
```xml
WindowDecorationProperties.ElementRole="MinimizeButton"
WindowDecorationProperties.ElementRole="MaximizeButton"
WindowDecorationProperties.ElementRole="CloseButton"
```
这样可最大程度复用系统行为,避免自己重写整套非客户区逻辑。

5. WPF 旧实现里最大化后内容区会留出 7px 安全边距;Avalonia 端优先直接绑定 `Window.OffScreenMargin`,不要继续写死魔法数,这样能更接近系统实际边界。

6. 标题栏视觉尽量保留原资源:
   - `MainWindow` 左上角继续使用 `title.png / title_sm.png`
   - `ConfigWindow` 继续使用纯文本标题 + 关闭按钮
   - 额外功能按钮(设置 / mini)继续放在系统按钮左侧,不要拆到内容区

> 结论:对 `OsuPlayer` 这种原本就有自绘标题栏的 WPF 项目,`Avalonia Window + 自定义 WindowTitleBar 控件` 是最低风险、最低改动的迁移路径;重点不是“重设计”,而是把 WPF `WindowChrome` 的职责逐项映射完整。

### 3.4 常用代码命名空间

| WPF | Avalonia |
|---|---|
| `using System.Windows;` | `using Avalonia;` |
| `using System.Windows.Controls;` | `using Avalonia.Controls;` |
| `using System.Windows.Data;` | `using Avalonia.Data;` |
| `using System.Windows.Input;` | `using Avalonia.Input;` |
| `using System.Windows.Media;` | `using Avalonia.Media;` |
| `using System.Windows.Media.Animation;` | `using Avalonia.Animation;` |
| `using System.Windows.Shapes;` | `using Avalonia.Controls.Shapes;` |
| `using System.Windows.Threading;` | `using Avalonia.Threading;` |
| `using Microsoft.Xaml.Behaviors;` | `using Avalonia.Xaml.Behaviors;` |
| `using Hardcodet.Wpf.TaskbarNotification;` | `using Avalonia.Controls;` (TrayIcon) |

---

## 4. 资源与样式迁移

### 4.1 资源字典

| WPF 文件 | Avalonia 文件 | 改动 |
|---|---|---|
| `Styles/BrushDictionary.xaml` | `Styles/BrushDictionary.axaml` | 见 4.2 |
| `Styles/ButtonDictionary.xaml` | `Styles/ButtonDictionary.axaml` | 移除 implicit Style 改 Selector |
| `Styles/ButtonStyle.xaml` | `Styles/ButtonStyle.axaml` | ControlTemplate → ControlTheme |
| `Styles/Components.xaml` | `Styles/Components.axaml` | 通用 |
| `Styles/ConfigStyle.xaml` | `Styles/ConfigStyle.axaml` | 通用 |
| `Styles/ContextMenuStyle.xaml` | `Styles/ContextMenuStyle.axaml` | 同上 |
| `Styles/ConverterDictionary.xaml` | `Styles/ConverterDictionary.axaml` | 转换器,语法不变 |
| `Styles/EasingFunction.xaml` | `Styles/EasingFunction.axaml` | EasingFunctionKeyFrames → Easing |
| `Styles/FontDictionary.xaml` | `Styles/FontDictionary.axaml` | 字体注册方式改变 |
| `Styles/GeneralStyle.xaml` | `Styles/GeneralStyle.axaml` | 通用 |
| `Styles/i18n.xaml` | `Styles/i18n.axaml` | 几乎不变(动态资源) |
| `Styles/ListViewStyle.xaml` | `Styles/ListViewStyle.axaml` | ListView → ListBox |
| `Styles/NavigationStyle.xaml` | `Styles/NavigationStyle.axaml` | RadioButton 模板 |
| `Styles/Radios.xaml` | `Styles/Radios.axaml` | RadioButton 模板 |
| `Styles/ScrollBarStyle.xaml` | `Styles/ScrollBarStyle.axaml` | ScrollBar 模板 |
| `Styles/SliderStyle.xaml` | `Styles/SliderStyle.axaml` | Slider 模板 |
| `Styles/StatusIconStyle.xaml` | `Styles/StatusIconStyle.axaml` | 通用 |
| `Styles/SvgDictionary.xaml` | `Styles/SvgDictionary.axaml` | SVG 处理见 4.3 |
| `Styles/TabStyle.xaml` | `Styles/TabStyle.axaml` | TabControl 模板 |
| `lang/default.xaml` | `lang/default.axaml` | 几乎不变 |
| `lang/en-US.xaml`(运行时文件) | `lang/en-US.axaml` | 几乎不变 |

### 4.2 Brush/Color 资源

```xml
<!-- WPF -->
<SolidColorBrush x:Key="UiForegroundColor" Color="#FFCCCCCC" />

<!-- Avalonia -->
<SolidColorBrush x:Key="UiForegroundColor" Color="#CCCCCC" />
<!-- 或直接 Color -->
<Color x:Key="UiForegroundColor">#CCCCCC</Color>
```

> Avalonia 的 `Color` 是 `#AARRGGBB` 或 `#RRGGBB`(可选 alpha),与 WPF 相同。

### 4.3 SVG 资源(关键)

WPF 端用 `StreamGeometry` 在 XAML 中定义 SVG path。Avalonia 11+ 支持 `<StreamGeometry>` 但语法略不同:

```xml
<!-- WPF (SvgDictionary.xaml 中) -->
<StreamGeometry x:Key="IconPlay">M0,0 L10,5 L0,10 Z</StreamGeometry>

<!-- Avalonia (一致) -->
<StreamGeometry x:Key="IconPlay">M0,0 L10,5 L0,10 Z</StreamGeometry>
```

引用方式:
```xml
<!-- WPF -->
<Path Data="{StaticResource IconPlay}" Fill="..." />

<!-- Avalonia -->
<Path Data="{StaticResource IconPlay}" Fill="..." />
```

> 部分 SVG path 在 Avalonia 中解析略有差异,需逐个验证。

### 4.4 SnapsToDevicePixels 移除

Avalonia 渲染默认就考虑设备像素,删除所有 `SnapsToDevicePixels="True"`,无替代。

### 4.5 RenderOptions.BitmapScalingMode

Avalonia 等价:`RenderOptions` 附加属性(API 完整):
```xml
<Border RenderOptions.BitmapScalingMode="HighQuality">
```

行为相同(HighQuality / LowQuality / Linear / NearestNeighbor / MediumQuality / Unspecified)。

### 4.6 RenderOptions.ClearTypeHint

Avalonia 无对应概念,**删除**。

### 4.7 字体加载

```xml
<!-- WPF (FontDictionary.xaml) -->
<FontFamily x:Key="MainFont">pack://application:,,,/Resources/Fonts/#Source Sans Pro</FontFamily>

<!-- Avalonia: 不在 XAML 中加载,改在 App.axaml.cs 中 -->
```

```csharp
// OsuPlayer.Avalonia/App.axaml.cs
public override void Initialize()
{
    AvaloniaXamlLoader.Load(this);
    // 加载自定义字体
    Resources["MainFont"] = new FontFamily("avares://OsuPlayer.Avalonia/Resources/Fonts#Source Sans Pro");
    // 或
    var assets = AssetLoader.Open(new Uri("avares://OsuPlayer.Avalonia/Resources/Fonts/SourceSansPro-Regular.ttf"));
    // 注册到 FontManager.Manager
}
```

更现代的写法 — 在 App.axaml 中:
```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml" />
</Application.Styles>

<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://OsuPlayer.Avalonia/Resources/Fonts.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## 5. 窗口/页面/UserControl 映射表

### 5.1 Windows(7 个)

| WPF | Avalonia | 工作量 | 备注 |
|---|---|---|---|
| `Windows/MainWindow.xaml(.cs)` | `Windows/MainWindow.axaml(.cs)` | 高 | 主窗口,含导航/播放控制/侧栏 |
| `Windows/MiniWindow.xaml(.cs)` | `Windows/MiniWindow.axaml(.cs)` | 中 | 迷你模式,桌面边栏吸附(用 `Window.Position` + `Animation` 替代 Storyboard) |
| `Windows/ConfigWindow.xaml(.cs)` | `Windows/ConfigWindow.axaml(.cs)` | 中 | 设置窗口,Frame 导航 |
| `Windows/BeatmapInfoWindow.xaml(.cs)` | `Windows/BeatmapInfoWindow.axaml(.cs)` | 中 | 谱面信息弹窗 |
| `Windows/LyricWindow.xaml(.cs)` | `Windows/LyricWindow.axaml(.cs)` | 高 | 歌词窗口,含着色器特效(灰度+模糊) + Storyboard 动画 + Always-on-top |
| `Windows/ExceptionWindow.xaml(.aml.cs)` | `Windows/ExceptionWindow.axaml(.cs)` | 低 | 异常弹窗 |
| `Windows/NewVersionWindow.xaml(.cs)` | `Windows/NewVersionWindow.axaml(.cs)` | 低 | 升级弹窗(Markdown 改为纯文本) |
| `UpdateWindow.xaml(.cs)`(根) | `Windows/UpdateWindow.axaml(.cs)` | 低 | 更新窗口 |

### 5.2 Pages(7 个)

WPF 的 `Page` 全部改为 Avalonia `UserControl`,配合 `Frame`/`ContentControl` 切换:

| WPF Page | Avalonia UserControl | 备注 |
|---|---|---|
| `Pages/CollectionPage.xaml` | `Views/Pages/CollectionPage.axaml` | 收藏夹,含列表+自定义 Panel |
| `Pages/FindPage.xaml` | `Views/Pages/FindPage.axaml` | 发现 |
| `Pages/RecentPlayPage.xaml` | `Views/Pages/RecentPlayPage.axaml` | 最近播放 |
| `Pages/SearchPage.xaml` | `Views/Pages/SearchPage.axaml` | 搜索 |
| `Pages/ExportPage.xaml` | `Views/Pages/ExportPage.axaml` | 导出 |
| `Pages/StoryboardPage.xaml` | **跳过**(用户确认) | — |
| `Pages/Settings/AboutPage.xaml` | `Views/Pages/Settings/AboutPage.axaml` | 关于 |
| `Pages/Settings/ExportPage.xaml` | `Views/Pages/Settings/ExportPage.axaml` | 导出设置 |
| `Pages/Settings/GeneralPage.xaml` | `Views/Pages/Settings/GeneralPage.axaml` | 通用设置 |
| `Pages/Settings/HotKeyPage.xaml` | `Views/Pages/Settings/HotKeyPage.axaml` | 热键设置 |
| `Pages/Settings/InterfacePage.xaml` | `Views/Pages/Settings/InterfacePage.axaml` | 界面设置 |
| `Pages/Settings/LyricPage.xaml` | `Views/Pages/Settings/LyricPage.axaml` | 歌词设置 |
| `Pages/Settings/PlayPage.xaml` | `Views/Pages/Settings/PlayPage.axaml` | 播放设置 |

> `Page` 改 `UserControl`:`xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` 根元素 `Page` → `UserControl`(Avalonia 命名空间)。

### 5.3 UserControls(12 个)

| WPF UserControl | Avalonia UserControl | 备注 |
|---|---|---|
| `UserControls/AddCollectionControl.xaml` | `Views/UserControls/AddCollectionControl.axaml` | 简单 |
| `UserControls/AnimationControl.xaml` | `Views/UserControls/AnimationControl.axaml` | 含 FFME,改 FFmpegVideoPlayer |
| `UserControls/ClosingControl.xaml` | `Views/UserControls/ClosingControl.axaml` | 简单 |
| `UserControls/DifficultyBadge.xaml` | `Views/UserControls/DifficultyBadge.axaml` | 简单徽章 |
| `UserControls/DiffSelectControl.xaml` | `Views/UserControls/DiffSelectControl.axaml` | 难度选择 |
| `UserControls/EditCollectionControl.xaml` | `Views/UserControls/EditCollectionControl.axaml` | 简单 |
| `UserControls/MiniPlayController.xaml` | `Views/UserControls/MiniPlayController.axaml` | 迷你播放控制 |
| `UserControls/PlayController.xaml` | `Views/UserControls/PlayController.axaml` | 主播放控制(复杂) |
| `UserControls/PlayListControl.xaml` | `Views/UserControls/PlayListControl.axaml` | 播放列表 |
| `UserControls/PlayModeControl.xaml` | `Views/UserControls/PlayModeControl.axaml` | 播放模式 |
| `UserControls/SelectCollectionControl.xaml` | `Views/UserControls/SelectCollectionControl.axaml` | 选收藏夹 |
| `UserControls/VolumeControl.xaml` | `Views/UserControls/VolumeControl.axaml` | 音量控制 |
| `UserControls/WelcomeControl.xaml` | `Views/UserControls/WelcomeControl.axaml` | 欢迎页 |

### 5.4 UiComponents(自定义控件)

| WPF | Avalonia | 改造重点 |
|---|---|---|
| `UiComponents/ButtonComponent/CommonButton` | `Controls/ButtonComponent/CommonButton` | `Style` → `ControlTheme` |
| `UiComponents/ButtonComponent/CloseButton` | `Controls/ButtonComponent/CloseButton` | 通用 |
| `UiComponents/ButtonComponent/MaxButton` | `Controls/ButtonComponent/MaxButton` | 通用 |
| `UiComponents/ButtonComponent/MinButton` | `Controls/ButtonComponent/MinButton` | 通用 |
| `UiComponents/ButtonComponent/PlayerControlButton` | `Controls/ButtonComponent/PlayerControlButton` | 通用 |
| `UiComponents/ButtonComponent/SystemButton` | `Controls/ButtonComponent/SystemButton` | 通用 |
| `UiComponents/FrontDialogComponent/FrontDialogOverlay` | `Controls/FrontDialogComponent/FrontDialogOverlay` | Storyboard → Animation |
| `UiComponents/LoaderComponent/Loader` | `Controls/LoaderComponent/Loader` | 旋转动画 |
| `UiComponents/NotificationComponent/NotifyControl` | `Controls/NotificationComponent/NotifyControl` | Storyboard → Animation |
| `UiComponents/NotificationComponent/NotifyOverlay` | `Controls/NotificationComponent/NotifyOverlay` | 通用 |
| `UiComponents/PanelComponent/VirtualizingGalleryWrapPanel` | `Controls/PanelComponent/VirtualizingGalleryWrapPanel` | 自定义 Panel(重写) |
| `UiComponents/RadioButtonComponent/SwitchRadio` | `Controls/RadioButtonComponent/SwitchRadio` | `ControlTemplate` → `ControlTheme` + PseudoClass |
| `UiComponents/TextBlockComponent/OutlinedTextBlock` | `Controls/TextBlockComponent/OutlinedTextBlock` | 自定义 `TemplatedControl`,Skia 描边 |
| `UiComponents/TextBoxComponent/CommonTextBox` | `Controls/TextBoxComponent/CommonTextBox` | `ControlTemplate` → `ControlTheme` |

---

## 6. 动画 (Storyboard) 迁移

WPF `System.Windows.Media.Animation.Storyboard` 大量使用(`LyricWindow`、`MiniWindow`、`NotifyControl`、`FrontDialogOverlay`、`FrameNavigationService`、`SwitchRadio`、`VirtualizingGalleryWrapPanel`)。Avalonia 等价:

### 6.1 简单属性动画 (DoubleAnimation/ColorAnimation)

```csharp
// WPF
var da = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(0.3) };
Storyboard.SetTarget(da, this);
Storyboard.SetTargetProperty(da, new PropertyPath(OpacityProperty));
var sb = new Storyboard();
sb.Children.Add(da);
sb.Begin();

// Avalonia
var animation = new Animation.Animation
{
    Duration = TimeSpan.FromSeconds(0.3),
    FillMode = FillMode.Forward,
    Children =
    {
        new KeyFrame
        {
            Setters = { new Setter(OpacityProperty, 0d) },
            Cue = new Cue(0d)
        },
        new KeyFrame
        {
            Setters = { new Setter(OpacityProperty, 1d) },
            Cue = new Cue(1d)
        }
    }
};
await animation.RunAsync(this);
```

### 6.2 EasingFunction

```xml
<!-- WPF -->
<DoubleAnimation ...>
  <DoubleAnimation.EasingFunction>
    <CubicEase EasingMode="EaseInOut" />
  </DoubleAnimation.EasingFunction>
</DoubleAnimation>
```

```csharp
// Avalonia
new KeyFrame
{
    Setters = { new Setter(OpacityProperty, 0d) },
    Cue = new Cue(0d),
    KeySpline = new KeySpline(0.4, 0, 0.2, 1) // CubicEase.EaseInOut 等价
}
```

或使用内置 `Easing`:
```csharp
Easing = new CubicEaseInOut()
```

### 6.3 简单过渡(替代 Storyboard.Begin/Stop)

```xml
<!-- Avalonia: 直接在控件上声明 Transitions -->
<Border Opacity="0">
    <Border.Transitions>
        <Transitions>
            <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseInOut" />
        </Transitions>
    </Border.Transitions>
</Border>
<!-- 修改 Opacity 自动过渡 -->
```

### 6.4 各处动画改造点

| 文件 | 动画类型 | Avalonia 方案 |
|---|---|---|
| `LyricWindow.xaml.cs` | `DoubleAnimation` × 2 (Border.MarginProperty) | `Animation` |
| `MiniWindow.xaml.cs` | `DoubleAnimation` (LeftProperty, 自定义边栏吸附) | `Animation` + Window 位置 |
| `NotifyControl.xaml.cs` | `DoubleAnimation` + `VectorAnimation` | `Animation` |
| `FrontDialogOverlay.xaml.cs` | `DoubleAnimation` × 4 (ScaleX/ScaleY/Opacity) | `Animation` |
| `FrameNavigationService.cs` | `DoubleAnimation` (页面淡入 ScaleX/Y) | `Animation` |
| `SwitchRadio.cs` | `DoubleAnimation` (页面切换 + Radio 状态) | `Animation` + `Transitions` |
| `VirtualizingGalleryWrapPanel.cs` | `DoubleAnimation` (滚动到指定位置) | `Animation` |

---

## 7. 控件模板 (ControlTemplate) 迁移

### 7.1 通用改造

```xml
<!-- WPF Style (implicit, x:Key 不必有) -->
<Style TargetType="{x:Type Button}">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type Button}">
                <Border ...>
                    <ContentPresenter />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="..." />
        </Trigger>
    </Style.Triggers>
</Style>
```

```xml
<!-- Avalonia -->
<ControlTheme x:Key="{x:Type Button}" TargetType="Button">
    <Setter Property="Template">
        <ControlTemplate>
            <Border ...>
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </Setter>
    <Style Selector="^:pointerover">
        <Setter Property="Background" Value="..." />
    </Style>
</ControlTheme>
```

### 7.2 触发器 → 伪类

| WPF Trigger | Avalonia 伪类 |
|---|---|
| `Trigger Property="IsMouseOver" Value="True"` | `:pointerover` |
| `Trigger Property="IsPressed" Value="True"` | `:pressed` |
| `Trigger Property="IsEnabled" Value="False"` | `:disabled` |
| `Trigger Property="IsFocused" Value="True"` | `:focus` |
| `Trigger Property="IsChecked" Value="True"` (Radio/CheckBox) | `:checked` |
| `Trigger Property="IsSelected" Value="True"` (ListBoxItem) | `:selected` |
| `DataTrigger` | 用绑定 + 自定义伪类,或直接 `Style Selector` 配合 `Classes` 切换 |
| `EventTrigger` | 用 `Transitions` + `Animation` 配合 `:pointerover`/`:pressed` 等 |

### 7.3 TemplateBinding

WPF 和 Avalonia 都支持 `TemplateBinding`,语法相同。

### 7.4 x:Type 静态引用

```xml
<!-- WPF -->
TargetType="{x:Type Button}"
BasedOn="{StaticResource {x:Type Control}}"

<!-- Avalonia -->
TargetType="Button"
<!-- BasedOn 用 x:Key 引用 -->
BasedOn="{StaticResource ButtonControlTheme}"
```

---

## 8. 行为 (Behaviors) 迁移

### 8.1 包引用

```xml
<PackageReference Include="Avalonia.Xaml.Behaviors" Version="11.2.0.6" />
<!-- 适配 Avalonia 12;或最新版 -->
```

### 8.2 命名空间

```xml
<!-- WPF -->
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"
xmlns:ei="http://schemas.microsoft.com/expression/2010/interactions"

<!-- Avalonia -->
xmlns:behaviors="using:Avalonia.Xaml.Behaviors"
xmlns:behaviors="clr-namespace:Avalonia.Xaml.Behaviors;assembly=Avalonia.Xaml.Behaviors"
```

### 8.3 触发器映射

| WPF | Avalonia |
|---|---|
| `<i:Interaction.Behaviors>` | `<Interaction.Behaviors>` (同) |
| `<i:EventTrigger EventName="Loaded">` | `<EventTriggerBehavior EventName="Loaded">` |
| `<i:InvokeCommandAction Command="..." />` | `<InvokeCommandAction Command="..." />` |
| `<ei:CallMethodAction MethodName="..." />` | `<CallMethodAction MethodName="..." />` |
| `<ei:ChangePropertyAction PropertyName="..." Value="..." />` | `<ChangePropertyAction PropertyName="..." Value="..." />` |

### 8.4 实际触发现状

WPF 项目中,Behaviors 使用极少(`ExportPage.xaml` 中只有被注释的 `Interaction.Triggers`),实际无需在 Avalonia 中大量使用。但 ViewModel 中通过 `OnNavigatedTo`/`Loaded` 触发的逻辑需迁移到 Avalonia 对应生命周期事件。

---

## 9. 转换器 (IValueConverter) 迁移

### 9.1 接口

```csharp
// WPF
public class MyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { ... }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { ... }
}

// Avalonia (几乎相同)
public class MyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) { ... }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) { ... }
}
```

### 9.2 命名空间

```csharp
// WPF
using System.Windows.Data;

// Avalonia
using Avalonia.Data.Converters;
```

### 9.3 XAML 注册

```xml
<!-- WPF -->
<local:BooleanToCursorConverter x:Key="BooleanToCursorConverter" />

<!-- Avalonia (相同) -->
<local:BooleanToCursorConverter x:Key="BooleanToCursorConverter" />
```

### 9.4 ConvertBack 警告

Avalonia 12 编译绑定下 `ConvertBack` 不常调用,需标注 `[Obsolete]` 或返回 `AvaloniaProperty.UnsetValue`。

### 9.5 转换器清单(直接复用)

22 个 `Converters/*.cs` 中,**纯逻辑转换器**(不依赖 WPF API)可直接复制到 `OsuPlayer.Avalonia/Converters/`,改 using 即可:
- `BooleanToCursorConverter`(需评估是否还需要 WPF Cursor)
- `ButtonColorConverter`
- `DateTimeConverter`
- `DeviceInfoToStringConverter`
- `ExceptionToStringConverter`
- `IconColorConverter`
- `IndexToStringConverter`
- `MsToStringConverter`
- `NegativeBooleanConverter`
- `NullToHiddenConverter`(改为 `NullToIsVisibleConverter` 用 `IsVisible`)
- `PlayingConverter`
- `RoundedNumberConverter`
- `StarRating2ColorConverter` / `StarRating2ForeColorConverter`
- `TabConverter`
- `TrueToVisibleConverter`(改为 `BoolToIsVisibleConverter`)
- `MiniWindowConverter`
- `MainWindowConverters`(多值转换器,使用 `IMultiValueConverter` → Avalonia 同样支持)
- `LocalizedFontFamilyConverter`(依赖 `FontFamily`,Avalonia 同样支持)
- `GetOutlinedTextConverter`(用 `OutlinedTextBlock`)
- `MarkdownConverter`(**删除**,用纯文本)
- `NotificationComponent/Converters/*`(4 个)
- `EmptyToVisibleConverter` / `FontColorConverter` / `MixColorConverter` / `NotificationTypeConverter` / `NotificationTypeToCursorConverter`

---

## 10. 特殊功能:着色器/视频/托盘/Markdown

### 10.1 着色器 (ShaderEffects)

**WPF 实现**:`extensions\plugins\ShaderEffects.dll` 中包含 `GrayscaleEffect` / `ColorToneEffect` / `InvertColorEffect` / `BloomEffect` / `MagnifierEffect` / `MonochromeEffect` / `PixelateEffect` / `ShaderEffectBase` / `SmoothMagnifierEffect` / `SwirlEffect` / `ZoomBlurEffect` 等。WPF `ShaderEffect` 通过 `PixelShader` 加载 `.ps` 文件。

**Avalonia 方案:Skia 自定义 DrawOperation**(参考 KeyASIO 项目的 `SkiaColorMatrixUtils` + `HueRotationDrawOperation` 思路)

```csharp
// Skia/HueRotationDrawOperation.cs
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace OsuPlayer.Avalonia.Skia;

public class HueRotationDrawOperation : ICustomDrawOperation
{
    public float HueDegrees { get; set; } = 0f;
    public float Saturation { get; set; } = 1f;
    public Rect Bounds { get; set; }
    public IBitmap? Source { get; set; }

    public void Render(ImmediateDrawingContext context)
    {
        if (Source is null) return;
        var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (lease is null) return;
        using var skLease = lease.Lease();
        var canvas = skLease.SkCanvas;
        canvas.Save();
        // 1. 通过 SkColorFilter.CreateColorMatrix 实现 HueRotation
        var hueCos = MathF.Cos(HueDegrees * MathF.PI / 180f);
        var hueSin = MathF.Sin(HueDegrees * MathF.PI / 180f);
        // 参考 SkColorMatrixHelpers (类似 KeyASIO 项目)
        var matrix = new[]
        {
            hueCos + (1 - hueCos) / 3,           1f / 3 * (1 - hueCos) - hueSin / Sqrt3, 1f / 3 * (1 - hueCos) + hueSin / Sqrt3, 0, 0,
            1f / 3 * (1 - hueCos) + hueSin / Sqrt3, hueCos + (1 - hueCos) / 3,        1f / 3 * (1 - hueCos) - hueSin / Sqrt3, 0, 0,
            1f / 3 * (1 - hueCos) - hueSin / Sqrt3, 1f / 3 * (1 - hueCos) + hueSin / Sqrt3, hueCos + (1 - hueCos) / 3,           0, 0,
            0, 0, 0, 1, 0
        };
        var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint
        {
            ColorFilter = colorFilter,
            IsAntialias = true
        };
        // 绘制 Source bitmap
        // ...
        canvas.Restore();
    }
    
    public void Dispose() { }
    public bool HitTest(Point p) => false;
    public bool Equals(ICustomDrawOperation? other) => false;
}
```

引用方式:`<Image Source="..."><Image.Effect><hue:HueRotationEffect HueDegrees="180" /></Image.Effect></Image>`,需自定义 `Effect` 附加属性 + `RenderTargetBitmap` 重绘。

**简化方案**:
1. **直接用 SkColorFilter** 渲染到位图后显示
2. **或** 使用 `Avalonia.Skia` 提供的 `Effect` 抽象 + `Shader`
3. **LyricWindow 灰度**:用 `OpacityMask` + `LinearGradientBrush`,纯色不需要 Skia
4. **LyricWindow 模糊**:用 `RenderTargetBitmap` + `SKImageFilter.CreateBlur`

**建议**:先实现灰度(用 `OpacityMask`),后用 Skia 实现 HueRotation,模糊跳过(歌词窗口通常不需要)。

### 10.2 视频 (FFME → FFmpegVideoPlayer)

```xml
<!-- WPF (AnimationControl.xaml) -->
xmlns:ffme="clr-namespace:Unosquare.FFME;assembly=ffme.win"
<ffme:MediaElement Source="..." />

<!-- Avalonia -->
xmlns:ffmpeg="clr-namespace:Avalonia.FFmpegVideoPlayer;assembly=Avalonia.FFmpegVideoPlayer"
<ffmpeg:VideoPlayerControl Source="..." ShowControls="True" />
```

需要在 `Program.cs` 初始化:
```csharp
using FFmpegVideoPlayer.Core;
FFmpegInitializer.Initialize();  // 加载 FFmpeg 原生库
```

并保留 `extensions\plugins\ffmpeg\win-x64\*.dll` 复制到输出。

### 10.3 托盘 (Hardcodet.NotifyIcon → Avalonia.TrayIcon)

```xml
<!-- WPF (MainWindow.xaml) -->
<tb:TaskbarIcon x:Name="NotifyIcon"
                IconSource="..."
                ToolTipText="Osu Player"
                TrayMouseDoubleClick="...">
    <tb:TaskbarIcon.ContextMenu>
        <ContextMenu>...</ContextMenu>
    </tb:TaskbarIcon.ContextMenu>
</tb:TaskbarIcon>

<!-- Avalonia (TrayIcon 不是 Window 子控件,而是 Application 顶级) -->
<!-- 放在 App.axaml 中: -->
<Application>
    <Application.Styles>...</Application.Styles>
    <TrayIcon.Icons>
        <TrayIcons>
            <TrayIcon Icon="avares://OsuPlayer.Avalonia/Resources/osuPlayer.ico"
                      ToolTipText="Osu Player"
                      Clicked="OnTrayClick">
                <TrayIcon.Menu>
                    <NativeMenu>
                        <NativeMenuItem Header="显示" Click="OnShowClick" />
                        <NativeMenuItem Header="退出" Click="OnExitClick" />
                    </NativeMenu>
                </TrayIcon.Menu>
            </TrayIcon>
        </TrayIcons>
    </TrayIcon.Icons>
</Application>
```

事件:
- WPF `TrayMouseDoubleClick` → Avalonia `Clicked`(双击也触发,需判断 `ClickCount`)
- WPF `TrayRightMouseDown` → Avalonia 没有 `RightClicked`,改用 `MenuFlyout`(右键弹出)

### 10.4 Markdown

`Markdig.Wpf` 的 `MarkdownViewer` 替换:**不使用**。`NewVersionWindow.xaml` 的内容改为 `TextBlock` + 纯文本(或后续接入 `Markdown.Avalonia` 社区包,目前用户确认跳过)。

---

## 11. DI 与启动流程改造

### 11.1 WPF 启动 (App.xaml.cs)

```csharp
// WPF
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        EntryStartup.Initialize();
        var services = EntrySetup.Build();
        var mainWindow = services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

### 11.2 Avalonia 启动 (App.axaml.cs)

```csharp
// Avalonia
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            EntryStartup.Initialize();
            Services = EntrySetup.Build();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
            
            desktop.ShutdownRequested += OnShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### 11.3 EntrySetup 改造

`EntrySetup.cs` 中注册的服务大部分可复用(无 UI 依赖),需调整:
- `services.AddTransient<Pages.CollectionPage>();` → `services.AddTransient<OsuPlayer.Avalonia.Views.Pages.CollectionPage>();`
- `services.AddTransient<MainWindow>();` → `services.AddTransient<OsuPlayer.Avalonia.Windows.MainWindow>();`
- `services.AddTransient<Windows.LyricWindow>();` → `services.AddTransient<OsuPlayer.Avalonia.Windows.LyricWindow>();`
- `services.AddTransient<Pages.Settings.HotKeyPage>();` → 同样重命名
- ...

**建议**:在 `OsuPlayer.Avalonia` 项目下重新创建 `EntrySetup.cs`,引用 `OsuPlayer.Avalonia` 的命名空间。

### 11.4 FrameNavigationService 改造

WPF `Frame` + `Page` 导航改为 Avalonia `ContentControl` + `UserControl`:

```csharp
// OsuPlayer.Presentation.Avalonia/FrameNavigationService.cs
public class FrameNavigationService
{
    private readonly ContentControl _frame;
    
    public void NavigateTo<T>() where T : UserControl
    {
        _frame.Content = App.Services.GetRequiredService<T>();
    }
}
```

可选方案:用 `Avalonia.Navigation`(导航库),但社区常用 `ContentControl` + Service。

---

## 12. P/Invoke 与平台代码

### 12.1 审计清单

| 文件 | 调用 | 平台 |
|---|---|---|
| `OverallKeyHook.cs` | `Milki.Extensions.MouseKeyHook.IKeyboardHook` | 跨平台抽象(OK) |
| `EntrySetup.cs` | `Milki.Extensions.MouseKeyHook.KeyboardHookFactory.CreateGlobal()` | 跨平台抽象(OK) |
| `FFmpegWindowsFunctionResolver.cs` | `FFmpeg.AutoGen` 库加载 | Windows (OK) |
| `Windows/LyricWindow.xaml.WinApi.cs` | Win32 `SetWindowLong` / `GetWindowLong` / `WS_EX_TRANSPARENT` 等 | Windows only (Avalonia 仍可用 `Win32Interop` 调) |
| `OsuPlayer.Data` | `oppai.dll` P/Invoke (Coosu.Beatmap 包装) | Windows (OK,目标就是 Windows) |
| `OsuPlayer.Media.Audio` | `NAudio` 音频输出 | Windows (OK) |
| `OsuPlayer.Media.Audio` | `Microsoft.WindowsAPICodePack` Shell | Windows (OK) |

### 12.2 Win32 调用 (LyricWindow 透明穿透)

WPF 用 `HwndSource.FromHwnd` + `SetWindowLong(hwnd, GWL_EXSTYLE, WS_EX_TRANSPARENT)`。

Avalonia 等价:
```csharp
// Avalonia 11+
var hwnd = WindowNative.GetWindowHandle(window);
var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT);
```

需引用:
```xml
<PackageReference Include="Avalonia.Win32" Version="12.0.4" />
```

```csharp
using Avalonia.Platform;
```

---

## 13. Phase 1.5 基线建立步骤

已完成:
- [x] 创建 `OsuPlayer.Avalonia` 项目(模板 `avalonia.mvvm`)
- [x] 验证构建成功(0 错误 0 警告)
- [x] 启用 `AvaloniaUseCompiledBindingsByDefault`
- [x] 添加 `AvaloniaUI.DiagnosticsSupport`

下一步:
1. 扩展 `OsuPlayer.Avalonia.csproj`:
   - 添加 `OsuPlayer.Shared` / `OsuPlayer.Core` / `OsuPlayer.Data` / `OsuPlayer.Media.Audio` / `OsuPlayer.Media.Lyric` / `OsuPlayer.Playback` / `OsuPlayer.Presentation` 引用
   - 添加 `CommunityToolkit.Mvvm 8.4.2`
   - 添加 `Avalonia.Xaml.Behaviors`
   - 添加 `Avalonia.TrayIcon`(若 Avalonia 12 包含则无需)
   - 添加 `FFmpegVideoPlayer.Avalonia`
   - 添加 `NAudio` / `FFmpeg.AutoGen` / `Dapper` / `Coosu.Beatmap` / `NLog`
2. 复制 `OsuPlayer/Resources/Fonts/*` → `OsuPlayer.Avalonia/Resources/Fonts/`,并配置为 `<AvaloniaResource>`
3. 复制 `OsuPlayer/Resources/*.png` `OsuPlayer/Resources/official/*` `OsuPlayer/Resources/*.jpg` → Avalonia
4. 复制 `OsuPlayer/Resources/xaml/*.xaml`(SVG 资源)
5. 创建 `OsuPlayer.Avalonia/Styles/FontDictionary.axaml` 注册字体
6. 创建 `OsuPlayer.Avalonia/Converters/*`(直接复制 + 改 using)
7. 创建 `OsuPlayer.Avalonia/EntrySetup.cs`(服务注册)
8. 创建 `OsuPlayer.Avalonia/EntryStartup.cs`(初始化逻辑)
9. 改造 `App.axaml.cs` 使用 DI
10. 验证空窗口 + DI 运行

---

## 14. 逐步执行清单

### Phase A:基线(0.5 天)

- [ ] 步骤 A1:扩展 csproj 引用业务层
- [ ] 步骤 A2:复制资源(字体/图片/SVG)
- [ ] 步骤 A3:复制 ViewModel 到 Avalonia(暂时原样,改命名空间)
- [ ] 步骤 A4:复制 Converters 改 using
- [ ] 步骤 A5:App.axaml.cs 接入 DI + EntrySetup
- [ ] 步骤 A6:`dotnet build` 验证

### Phase B:样式 + 主窗口(1.5 天)

- [ ] 步骤 B1:迁移 `Styles/BrushDictionary` + `FontDictionary` + `GeneralStyle` + `i18n` + `ConverterDictionary`
- [ ] 步骤 B2:迁移 `Styles/ButtonStyle` (WPF Style → ControlTheme)
- [ ] 步骤 B3:迁移 `Styles/SliderStyle` / `ScrollBarStyle` / `TabStyle` / `ListViewStyle`(ListView → ListBox)
- [ ] 步骤 B4:迁移 `Styles/ContextMenuStyle` / `StatusIconStyle` / `ConfigStyle` / `NavigationStyle` / `Radios`
- [ ] 步骤 B5:迁移 `Styles/SvgDictionary` (StreamGeometry)
- [ ] 步骤 B6:迁移 `Styles/Components` / `EasingFunction`
- [ ] 步骤 B7:`MainWindow.axaml` 顶部 + 导航骨架
- [ ] 步骤 B8:`MainWindow.axaml.cs` 接入 ViewModel + 托盘(暂时跳过托盘)
- [ ] 步骤 B9:验证窗口能打开

### Phase C:子页面 + 控件(2 天)

- [ ] 步骤 C1:`Pages/Settings/*` 7 个页面
- [ ] 步骤 C2:`Pages/CollectionPage` / `FindPage` / `RecentPlayPage` / `SearchPage` / `ExportPage`
- [ ] 步骤 C3:`FrameNavigationService` 改造为 ContentControl
- [ ] 步骤 C4:`UserControls/*` 12 个控件
- [ ] 步骤 C5:`UiComponents/*` 7 个自定义控件(SwitchRadio 涉及动画,优先级靠后)

### Phase D:播放 + 歌词 + 特殊(2 天)

- [ ] 步骤 D1:迁移 `OsuPlayer/Windows/LyricWindow` + Skia 着色器
- [ ] 步骤 D2:迁移 `OsuPlayer/Windows/MiniWindow`(边栏吸附)
- [ ] 步骤 D3:迁移 `OsuPlayer/UserControls/AnimationControl`(FFME → FFmpegVideoPlayer)
- [ ] 步骤 D4:Storyboard → Avalonia Animation(覆盖所有 6 处)
- [ ] 步骤 D5:`OverallKeyHook` 验证(跨平台抽象直接复用)

### Phase E:配置 + 升级(0.5 天)

- [ ] 步骤 E1:`Windows/ConfigWindow` + Frame 导航
- [ ] 步骤 E2:`Windows/BeatmapInfoWindow` + `Windows/ExceptionWindow` + `Windows/NewVersionWindow` + `UpdateWindow`

### Phase F:集成验证(0.5 天)

- [ ] 步骤 F1:`dotnet build` 修复所有警告/错误
- [ ] 步骤 F2:启动应用,验证每个窗口/页面
- [ ] 步骤 F3:异常处理 + 日志验证
- [ ] 步骤 F4:更新 `OsuPlayer/EntrySetup` 引用(可选,旧项目继续保留)

**总估时**:7 个工作日(单人)

---

## 15. 回退与风险控制

### 15.1 风险点

| 风险 | 等级 | 缓解 |
|---|---|---|
| 着色器效果无法 1:1 复刻 | 中 | 优先保证灰度,HueRotation 后置 |
| Storyboard 动画时序差异 | 低 | 关键路径逐个验证 |
| ControlTemplate 大量重写工作 | 高 | 优先实现核心 4 个样式(Button/Slider/ListBox/ContextMenu),其余用 FluentTheme 默认 |
| FFME 视频格式兼容 | 中 | 测试 osu! 故事板实际格式(主要 mp4),FFmpegVideoPlayer.Avalonia 基于 FFmpeg 覆盖更广 |
| Avalonia 12 vs 11 API 差异 | 中 | 锁定 `Avalonia 12.0.4` 版本,所有 API 参考 12 文档 |
| SkColorMatrix 数学 | 低 | 参考 KeyASIO 现有实现 |
| WPF Storyboard 引用 DependencyProperty | 中 | 替换为 AvaloniaProperty |

### 15.2 回退策略

- WPF 项目(`OsuPlayer`)原样保留,不删除
- Avalonia 项目作为新入口并行存在
- 业务层完全共享,新 UI 出现问题时不影响数据/播放
- Phase 1.5 提交点为 `port-baseline` tag,任何时候可回退

### 15.3 关键 commit 节点

1. `feat: scaffold Avalonia project` — 项目脚手架
2. `feat: wire DI with business layer` — DI 接入
3. `feat: migrate brushes and fonts` — 资源
4. `feat: migrate MainWindow skeleton` — 主窗口骨架
5. `feat: migrate pages` — 子页面
6. `feat: migrate user controls` — 用户控件
7. `feat: add Skia shader` — 着色器
8. `feat: integrate FFmpegVideoPlayer` — 视频
9. `feat: integrate TrayIcon` — 托盘
10. `fix: port all Storyboards` — 动画
11. `chore: clean up` — 清理

---

## 附录 A:版本对齐

| 组件 | 版本 | 说明 |
|---|---|---|
| .NET SDK | 10.0.300 | 已安装 |
| Avalonia | 12.0.4 | 模板默认 |
| CommunityToolkit.Mvvm | 8.4.1+ | 模板默认 |
| Avalonia.Xaml.Behaviors | 11.x | 适配 Avalonia 12 |
| FFmpegVideoPlayer.Avalonia | latest | NuGet |
| NAudio | 2.3.0 | 与 WPF 一致 |
| FFmpeg.AutoGen | 7.0.0 | 与 WPF 一致 |
| Dapper | 2.1.79 | 与 WPF 一致 |
| Coosu.Beatmap | 2.5.1 | 与 WPF 一致 |
| NLog | 6.1.3 | 与 WPF 一致 |
| SkiaSharp | (Avalonia 传递依赖) | — |

## 附录 B:参考资源

- [Avalonia WPF 迁移总览](https://docs.avaloniaui.net/docs/migration/wpf/)
- [Avalonia WPF Cheat Sheet](https://docs.avaloniaui.net/docs/migration/wpf/cheat-sheet)
- [Avalonia 控件映射](https://docs.avaloniaui.net/docs/migration/wpf/controls)
- [Avalonia 数据模板](https://docs.avaloniaui.net/docs/migration/wpf/data-templates)
- [Avalonia 事件](https://docs.avaloniaui.net/docs/migration/wpf/events)
- [Avalonia 布局](https://docs.avaloniaui.net/docs/migration/wpf/layout)
- [Avalonia 属性](https://docs.avaloniaui.net/docs/migration/wpf/properties)
- [Avalonia 样式](https://docs.avaloniaui.net/docs/migration/wpf/styling)
- [CTO 移植指南](https://avaloniaui.net/blog/the-expert-guide-to-porting-wpf-applications-to-avalonia)
- [FFmpegVideoPlayer.Avalonia](https://github.com/jojomondag/FFmpegVideoPlayer.Avalonia)

---

*文档生成于 Phase 0 完成时;执行过程中遇到具体 API 差异时,优先调用 `lookup_wpf_to_avalonia_mapping` 获取聚焦映射表。*
