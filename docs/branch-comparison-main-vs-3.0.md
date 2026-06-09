# main (3d81e5de) vs experimental/3.0 全面差异分析报告

> 报告生成时间：2026-06-09
> 基础数据：`main` 128 个独立提交，`experimental/3.0` 172 个独立提交；`git diff --stat` 统计 838 个文件变更。Merge-base 为 313c0f46（已远离两个分支的 HEAD）。`main` 基于 .NET 10 + Windows，`3.0` 仍在 .NET 6。

---

## 一、main 分支做了什么

`main` 的演进路径非常清晰：**先把 2.x 的"上帝文件"逐步解耦成一个干净的九项目分层架构，再用 CommunityToolkit.Mvvm + 强类型 DI + 事件总线 + EF Core 全面替换自研实现**。**几乎没有新功能，全部是架构改造**。

### 1. 架构重构：拆项目、去上帝类

| 主题 | 关键提交 | 改造点 |
|---|---|---|
| 拆分 `OsuPlayer.Wpf` | `e2158d5c`、`3fab9b92` | 把 `OsuPlayer.Wpf`（791 行 csproj，数十个混杂文件夹）拆为 4 个项目：`OsuPlayer.Core`（领域）、`OsuPlayer.Playback`（播放门面）、`OsuPlayer.Presentation`（WPF 接口/导航抽象）、`OsuPlayer.Media.Audio`（音频引擎） |
| 抽出 `OsuPlayer.Data` | `f292b802` 起 | 数据库层从 `OsuPlayer.Data` 改造为**纯 EF Core + SQLite**，引入 `OsuPlayerDbContext`、Migrations、列名 snake_case、`Func<OsuPlayerDbContext>` DI 工厂 |
| 解耦路径 | `c5759a3d`、`b9e176aa` | 干掉静态 `Domain` 类，引入 `IAppPaths` / `AppPaths` 抽象 + DI 注册 `IUserPreferences=> AppSettings.Default` |
| 共享层 | `bb2401f5`、`e2158d5c` | `IAppNotificationService` 等接口下沉到 `OsuPlayer.Shared`，移除 WPF 耦合 |

**最终九项目结构**（`src/`）：`OsuPlayer`（WPF 入口）、`OsuPlayer.Core`（领域/VM-独立服务）、`OsuPlayer.Data`（EF Core）、`OsuPlayer.Media.Audio`（音频引擎）、`OsuPlayer.Media.Lyric`（歌词）、`OsuPlayer.Playback`（协调门面）、`OsuPlayer.Presentation`（UI 接口）、`OsuPlayer.Shared`（跨项目）、`OsuPlayer.Abstractions`（空壳，未来预留）。

### 2. 数据访问：Dapper → EF Core 全量替换

| 提交 | 改动 |
|---|---|
| `8d979643`、`ea60b1f5` | 删除旧版 Dapper 数据层（`AppDbOperator.cs 555 行`、`BeatmapDbOperator.cs 314 行`、`Dapper/Provider/...` 全部 975 行 `DbProviderBase`），EF Core 全面接管 |
| `9dcc50da` | 搜索由客户端 LINQ 改 EF Core（`Use EF Core for beatmap search`） |
| `0f41b686`、`692bdaa2` | 加索引 + 表/列重命名 snake_case 化 |
| `e98c053b`、`2fbddbc3` | 引入 EF Core 迁移 + 遗留数据库迁移器 `LegacyPlayerDatabaseMigrator`（用 `ATTACH DATABASE` 导入旧 `player.db`，表重命名：`beatmap→beatmaps`、`map_info→beatmap_play_settings`、`collection→collections`、`collection_relation→collection_beatmaps`） |
| `0a2b3d5f`、`7706f5f1` | 统一 `Directory.Build.props` 管理程序集信息 + 项目迁到 SDK 风格 |
| `cf1b6127` | 修 GUID 映射错（自定义 `GuidTypeHandler`） |

DbContext 采用 **Transient** 生命周期，DI 中额外注册 `Func<OsuPlayerDbContext>` 单例。Repository 走 `IPlayerDataStore` 接口 + `PlayerDataService` 实现 + `NotifyingPlayerDataService` 装饰器。

### 3. 播放架构：拆 + 异步 + 状态机

| 提交 | 改动 |
|---|---|
| `9bc1d327`、`d1ffc211` | 把 `OsuPlayback` 重构为 `OsuAudio` 模块，独立 `IPlaybackController`、`OsuMixPlayer`、`OsuBeatmapAudioSession`、`OsuEffectPlaybackBus`、`OsuPlaybackEventAudioCache`、夜核规则 `NightcoreRules` |
| `e3f03726`、`6f618dc2` | 旧的全局键盘钩子替换为 **`Milki.Extensions.MouseKeyHook`** + KeyASIO 子模块 |
| `f292b802` | 全面迁移音频到 **KeyASIO + SoundTouch**（`SoundTouchPlaybackRateProcessorFactory.IsSupported`、`KeyASIO.Net` 子模块等） |
| `84af94b2` | 合并 `PlayerStatePump` + `OsuPlaybackEventDispatcher` → `PlayerEventBus`；内联 `BeatmapLoadService` |
| `c762847b`、`c5fed966` | 播放控制从同步改异步，UI 线程调度统一化 |
| `0e0a90a8`、`e80d7eb0` 等 | 多次 `Update KeyASIO.Net` |
| `9075a57a`、`4c6c5f35`、`e856de3e`、`eb575281` | 拆播放控制协调状态机、提取 `IUiThreadDispatcher` / `IPlaybackController`、谱面加载抽成独立 `BeatmapLoadService`、音频会话资源和播放规则提取 |
| `039eb739`、`31377edc`、`196ce3e9`、`0f6d28c2` | 修复全局快捷键 UI 线程执行、播放会话释放时未等待进行中操作、并发缓存数据竞争、预缓存任务管理 |
| `06417358` | 固定音频延迟为 1ms，简化设备选择策略 |
| `e7e0d74f`、`36d691b8`、`3aa61f93` | 视频 seek 前先暂停；默认 `GeneralOffset = -23`；新增 VBRI MP3 handling 与 `GeneralOffset` 绑定 |
| `df4832cb` | 砍掉动态音频校准代码 |
| `c2656cae` | **新增** balance mode + limiter settings |

`Playback` 项目提供 `ObservablePlayController`（UI 绑定的 `ObservableObject`）、`PlayerEventBus`（事件总线，所有事件经 `IUiThreadDispatcher` marshal）、`PlayerSessionService`（编解码/换曲/前后一首）、`SessionOperationManager`（**区分 `BeginCurrentOperation` 自动续播 vs `BeginInterruptingOperation` 手动操作**的取消令牌管理）。`OsuBeatmapAudioSession` 实现 12 秒预缓存窗口 + 8 秒前进。

### 4. MVVM + DI 全面化

| 提交 | 改动 |
|---|---|
| `2c5ee4c4`、`b78cbcd8` | 用 `CommunityToolkit.Mvvm` 替换自定义 `VmBase`，全面 `[ObservableProperty] public partial` 源生成器 |
| `7962d607` | 引入 DI 容器，全面重构服务注册与页面导航 |
| `878af05e` | 移除遗留 Service Locator 模式 |
| `a66dbf53`、`7b173a9d` | 用 CommunityToolkit.Mvvm 重构命令，全面异步化数据访问；`HttpClient` 替换 `HttpWebRequest` |
| `65481524` | `WeakReferenceMessenger` 解耦跨页面搜索导航 |
| `c5fed966` | 迁移播放器控件与页面为 MVVM，统一导航服务 `FrameNavigationService` |
| `28c3439e` | 移除自定义路由事件，改用标准 .NET 事件 |
| `e9a9ce42` | DI 注入 ViewModel，从 XAML 移除 `DataContext` 声明 |
| `c47f62d2` | 页面事件处理迁到 ViewModel + 命令 + 消息 |
| `3eb2cdb5` | 搜索改服务端分页 + 防抖取消（`CancellationTokenSource _queryCancellation` + `_queryVersion`） |
| `a7ab32b1` | **设置页从代码后台迁 MVVM** |

### 5. UI 解耦与样式化

| 提交 | 改动 |
|---|---|
| `87bc1040` | 导航折叠动画从转换器迁到 XAML 样式 |
| `a3285167` | 字体选择从 MVVM 绑定改为代码后台事件处理（特定场景反模式） |
| `8018a928` | 修解决方案文件夹名不匹配 |
| `f9dab90a` | 加 `.editorconfig` 统一文件末尾换行符 |
| `b3042471` | 统一文件作用域命名空间格式 |
| `68d0e1ae` | 删除 `Milky` 前缀，统一命名空间为 `OsuPlayer` |
| `2fdbd7cc` | **新增** 星级评分转换器 + 难度徽章组件 |
| `68240289` | 删除过时 helpers/converters |
| `883e06a8` | 清理 Windows Desktop SDK 依赖 |

### 6. Bug 修复

`eeb2011e`（播放器偏移未含固定偏移）、`10e739b0`（`MapIdentity` 反序列化失败）、`7ba79fe0`（安全停止辅助方法）、`792e8016`（播放操作异常处理）、`1d06b2ee`（移除播放控制结果类型，改显式方法）、`b0e41340`（重构导出服务）、`3b760282`（简化播放列表身份集合生成）、`db09b689`（抽取重复谱面操作到统一服务）、`4a19defa`（NAudio.Wave AsioOut 引用）、`c72e0d88`（重构播放完成处理与并发）、`73b2ad31`（重构谱面加载）、`6ef7f39e`（重组到 `src/`、`tests/` 目录）、`3fdb4e28`（`VirtualizingGalleryWrapPanel` 空引用）、`ad52ec74`（导航服务 + 收藏/搜索按钮状态）、`205cbac5`（FFmpeg native 依赖加载）、`da9d164e`（`TryAddCollectionAsync` 加 `isLocked`）、`e2bb77f8`（全面参数化 SQL）、`1f5631c9`（清调试日志）。

### 7. 现代基础设施

`66be450b` GitHub Actions 发布工作流；`8dac728e` SLNX 解决方案文件；`d87fde28` `Coosu.Beatmap` 2.5.1 升级 + 打击音逻辑重写；`2bca315e` 升级 FFME.Windows 支持架构分离；`ef33611c` NLog 6.x 升级。

---

## 二、experimental/3.0 分支做了什么

3.0 走的是**完全不同的方向**：重写为干净的六项目结构 + 自研完整的 WPF 控件库 + 全新的视觉/动效系统。**保留大量自研实现**，而 main 已用第三方替换。它**没有** EF Core 装饰器模式、**没有** CommunityToolkit.Mvvm，**没有**多项目分层。核心贡献是**UI 控件库**和**视觉系统**。

### 1. 项目结构重塑

| 提交 | 改动 |
|---|---|
| `e21b2ca3` | 从 WinUI 分支迁回 |
| `5fce1ba2`、`4c9dab0c` | 命名空间调整到 `Milki.OsuPlayer.*` |
| `729771dd` | 版本 bump 至 `3.0.0-a.0` |
| `ae7affa4` | 升级依赖库 |
| `b0d2e160` | `OsuPlayer.csproj` 更新（`net6.0-windows`，移除 `ApplicationDefinition`，手写 `Main`） |

**最终六项目结构**：`OsuPlayer`（WPF 入口）+ `OsuPlayer.Audio`（混音引擎）+ `OsuPlayer.Data`（EF Core）+ `OsuPlayer.Shared`（共享）+ `OsuPlayer.Wpf`（WPF 基础扩展）+ `OsuPlayer.Sentry`（异常上报）。再加三个测试项目 `BassTest`、`CorePlayerTest`、`WpfControlsTest`。

### 2. EF Core 早期版本（比 main 早）

| 提交 | 改动 |
|---|---|
| `2d5402f1`、`b83cab0a` | 替换库 + 清理代码 |
| `2f182b17`、`12680884` | 更新 SentryNLog + DSN |
| `3c46fe66` | 修意外快捷键触发 |
| `7829a0a0` | 干掉 Dapper（`Dapperちゃん、サヨナラ`） |
| `4dd4b592` | 修 EF 错 |
| `28ecfd40`、`157afc94` | 数据库查询工作完成 |
| `96844e43` | DbContext 手动 lifetime |
| `d621bf8d` | DbContext 在外部配置 |
| `5fb57db0`、`360cb3a9` | 新增/更新 EF 迁移 |
| `bbf095c2`、`168a91ff` | 生成 EF 文件 |
| `71a4cd3e` | 修 `Coosu.Database` 替代 Holly |
| `74c872e1` | FFmpeg 版本升级 |

`ApplicationDbContext` 比 main 早一代，未走 `Func<>` 工厂（用 `ServiceProviders.GetApplicationDbContext()` 直接 `new`），无 Dapper fallback，无装饰器，复杂业务用 `partial class` 拆成 `ApplicationDbContext.PlayItem.cs` / `.PlayList.cs`（L1-170 + L1-480）。`SaveChanges` 自动填 `IAutoCreatable.CreateTime` + `IAutoUpdatable.UpdatedTime`。`ConfigureConventions` 注册 `Point/Rectangle/TimeSpan/DateTime` 自定义转换器。

### 3. 自研音频引擎（与 main 平行的早期版本）

| 提交 | 改动 |
|---|---|
| `362c349c` | **全新音频系统** |
| `b4970747` | 在做新音频系统 |
| `40518620` | 缓存音效（`AudioCacheManager`） |
| `05452702` | 从资源加载 hitsound（OGG 嵌入） |
| `e32518d5` | 修复 `BeatmapSyncService` |
| `0b232a6b`、`0aa6fbf3`、`22800ac8` | 文件移动/重排 |
| `9cc86db3` | 公开 Player |
| `efd62a4b`、`efc57b8a` | 实现 `PlayerService` |
| `24493ac4` | PlayerService 接近完成 |
| `f528b691` | 修复 |
| `6854c1bb` | 更新 `SoundMixingTrack.cs` |
| `c143683a` | 修 hitsound 边缘问题 |

`OsuMixPlayer` 持有三个独立轨道（`SoundSeekingTrack`/`SoundMixingTrack`/`SampleTrack`）和三种独立音量（`MusicVolume`/`HitsoundVolume`/`SampleVolume`），`HitsoundBalanceRatio` 平衡因子。基于 **`Milki.Extensions.MixPlayer`**（自研 NuGet，main 已升级到 KeyASIO）。`PlayerService.InitializeNewAsync` 有 `CancelPreviousInitialization` + `DisposeActiveMixPlayer` + 异步互斥锁的串行化。**Hitsound 资源已经从 WAV 改为 OGG 嵌入**（25 个 `resources/default/*.ogg` 嵌入式资源）。

### 4. 全新视觉/动效系统（3.0 的最大特色）

| 提交 | 改动 |
|---|---|
| `66599cea` | **新 `AnimationControl` 逻辑**（视频+背景图，带 BlendBorder 模糊） |
| `2f510539` | **Add scene transform**（`CornerRadiusAnimation` 自定义 timeline + `Multi_BorderClipConverter`） |
| `ecd56334` | 用 `AcrylicPanel` 替代 BlurEffect |
| `d3a94a27` | 用 `AcrylicBrush` 替代 BlurEffect |
| `a7742240` | **Use new resources**（重写 SvgDictionary + 拆 `SystemUiButton`） |
| `f1a49f10` | `UiButton` 直接用 Template |
| `0bf372d7` | 增强卡片样式 |
| `7a0b5a8e` | 加 properties |
| `0ffcae17` | 更新 LyricWindow |
| `62d38358`、`b6a3b374`、`22e2cfa5` | 多轮设计调整 |
| `5de2137f` | **新增 `AnimatedScrollViewer`**（带平滑滚动 CubicEase EaseOut 500ms） |
| `21da6f06`、`dffa3710` | 更新卡片样式 |
| `d60c6ff2`、`d5a6b55d` | **新增 `CardCollectionControl`** 替换旧 `PlayListControl` |
| `b2f6a712` | 更新 MainWindow.xaml |
| `0154ee9a` | Standalone nav bar（`NavigationBar` 抽出） |
| `a5c2d04c`、`3291f42b` | SearchPage 完成 |
| `cc6ed093`、`4d8c2663`、`9a37558b` | Refactor search page 多次 |
| `1682d198` | 优化 `GradientStopCollectionExtensions` |

**核心控件（3.0 特有，main 没有）**：
- `src/OsuPlayer/UiComponents/DropShadowPanel.cs:1-372`（FluentWPF 来源，加 `ShadowMode { Content, Inner, Outer }` 三种）
- `src/OsuPlayer/UiComponents/ButtonComponent/UiButton.xaml(.cs)`（含 `DropShadowPanel`，8 种子类）
- `src/OsuPlayer/UiComponents/RadioButtonComponent/SwitchRadio.cs:1-368`（导航调度器，含 `CheckAndAction` 跨页调用 API）
- `src/OsuPlayer/UiComponents/TextBlockComponent/OutlinedTextBlock.cs:1-318`（自绘描边文字）
- `src/OsuPlayer/UiComponents/PanelComponent/VirtualizingGalleryWrapPanel.cs:1-442`（虚拟化 + 150ms CircleEase 滚动）
- `src/OsuPlayer/UiComponents/AnimatedFrame.xaml(.cs)`（带淡入缩放过渡）
- `src/OsuPlayer/UiComponents/AnimatedScrollViewer.cs:1-250`（带平滑滚动）
- `src/OsuPlayer/UiComponents/ContentDialogComponent/ContentDialog.xaml(.cs):1-435`（自研对话框，20+ DP）
- `src/OsuPlayer/UiComponents/PaginationComponent/Pagination.xaml(.cs):1-201`（分页器）
- `src/OsuPlayer/UiComponents/NotificationComponent/NotifyControl.xaml(.cs):1-252`（通知覆盖层）
- `src/OsuPlayer/UserControls/CardCollectionControl.xaml(.cs):1-255`（差量更新 `VisiblePlayItems` 懒加载缩略图）
- `src/OsuPlayer/CornerRadiusAnimation.cs:1-101`（自定义 `AnimationTimeline`，把 `CornerRadius` 转 `Thickness` 动画）
- `src/OsuPlayer/UiComponents/ContentDialogComponent/DialogOptionFactory.cs:1-46`（4 种预设 `DiffSelectOptions / SelectPlayListOptions / AddCollectionOptions / EditPlayListOptions`）

**资源字典（19 个 xaml）**：`EasingFunction.xaml` 集中 5 个缓动（`QuinticEaseOut/SineEaseOut/CircEaseIn/Out/PowerEaseOut5`），`SvgDictionary.xaml:1-688` 大批 SVG 资源（含 `HeartEnabledTempl/HeartDisabledTempl` 等），`BrushDictionary.xaml` 含 `PinkBrushOsu=#FFFD629A` 主色 + 难度渐变 `DifficultyBrush`。

**40 个自定义 Converters**（含 `Multi_*` 多值转换器），全部单例注册到 `ConverterDictionary.xaml`。

### 5. UI 控件测试项目（main 没有）

`9ab288c2`：**新增 `Tests/src/WpfControlsTest/`**，独立项目测试 `DropShadowPanel` / `UiButton` / `BrushAnimation` 等。

### 6. 自研 MVVM 基础

| 提交 | 改动 |
|---|---|
| `6fedec10` | **改进单例** → `SingletonVm<T>` 模板 |
| `f48f03ab` | 改进 I18N；拆分 `ExportService` |
| `e47ff200` | 替换 dialog |
| `49c00559` | 替换 `CommonTextBox` |
| `8dcd0dad` | 替换 notification |
| `41adb44f` | 替换 `SystemButton` |
| `ab2f5444` | 更新 `SwitchRadio` |
| `b778cce2`、`13d56d24`、`cf99fd60`、`aae51eff`、`d58ed0ff`、`48f80137` | 多轮 refactor |
| `8af5d4eb`、`1d239bfe`、`8e6a5773` | 清理 |
| `6351db27`、`2df20752`、`68e9965f`、`d97c5b8e` | 更新/修命名 |

`OsuPlayer.Shared/Observable/VmBase.cs:1-37`（INPC + INPCchanging 双接口）、`SingletonVm<T>.Default` 模板、`RaiseAndSetIfChanged` 扩展（受 ReactiveUI 启发）。`OsuPlayer.Wpf/Command/` 有自研 `DelegateCommand` / `RelayCommand` / `EventToCommand`（Galasoft 风格）。

### 7. 资源/配置：YAML + 静态访问

`AppSettings.yaml` 49 行（main 改 JSON）。`ConfigurationFactory.GetConfiguration<AppSettings>(".", "appsettings.yaml", MyYamlConfigurationConverter.Instance)` 静态创建单例。**所有代码仍用 `AppSettings.Default.XXXSection.YYY` 静态访问**（虽然 DI 注册了 `AddTransient(_ => AppSettings.Default)`）。`MyYamlConfigurationConverter` + `BindKeysConverter` 自定义 YAML 类型转换（`"Ctrl+Shift+A"` ↔ `BindKeys`）。

### 8. 导航：基于 `SwitchRadio` 自定义控件

**没有** `INavigationService` 接口。`SwitchRadio` 的 `Checked` 事件调 `InnerNavigate`，配合 `AnimatedFrame.AnimateNavigate` 走带动画导航。`CheckAndAction(Action<FrameworkElement>)` 是 3.0 独有的"先选中再回调" API，用于跨页触发。`NavigationType` 枚举只用了 1 次（实际靠 `TargetPageType` DP 指向 Page）。

### 9. 全局快捷键：抽到 `KeyHookService`

`OsuPlayer/Services/KeyHookService.cs:1-75` 单文件服务，8 个 `Action?` 委托 + `InitializeAndActivateHotKeys` / `DeactivateHotKeys`。`MainWindow.xaml.cs:153-183 BindHotKeyActions()` 在 `Window_Loaded` 末尾一次性绑定。

### 10. 歌词服务：仅用 LyricsFinder 库

`OsuPlayer/Services/LyricsService.cs:1-166` 包装 `LyricsFinder` 库的 `SourceProviderBase`，`_lyricProvider` 内嵌私有类，`SetLyricSynchronously(PlayItem?)` 是核心入口。`WriteCache`/`TryGetCache` 抛 `NotImplementedException`（缓存未实现）。`LyricWindow.xaml.cs:1-335` 桌面歌词窗，`CompositionTarget.Rendering` 50ms 轮询驱动。

### 11. 杂项

- `1d6dcd76` / `14985adc`（revert）/ `5ad01800`：revert 来回修 typo。
- `fdd88e8b` / `4d1bff32`：`App.xaml.cs` 更新。
- `7a016df1` / `1f3b8025` / `9ab7dfbe` / `2cc29dd6` / `ecb8ac4d` / `bcb04c6b` / `b08fdf2f` / `65e2f9b3`：CI/build 调参。
- `f1af364f`、`bbd140f1`：`Replace OSharp by Coosu`（main 也做了类似迁移）。
- `12295c82`、`17bcceb6`：`Fix null ref on initial play`（main 也有）。
- `d01d6aeb`、`d840e94d`、`a22c8157`、`aae51eff` 反复修编译。
- `7a016df1`、`2cc29dd6`、CI 反复调整。

---

## 三、全面差异对比

| 维度 | main | experimental/3.0 |
|---|---|---|
| **.NET 版本** | .NET 10 + Windows | .NET 6 + Windows |
| **项目数** | 9 个（含 Abstractions 空壳） | 6 个 + 3 个测试 |
| **代码量** | 266 个 .cs + 76 个 .xaml | 249 个 .cs + 83 个 .xaml |
| **架构哲学** | 多项目 Clean-ish 分层 + 抽象接口 | 紧凑分层 + 自研丰富 |
| **MVVM** | CommunityToolkit.Mvvm 8.4.2 + 源生成器 | 自研 `VmBase` + `SingletonVm<T>` + `DelegateCommand/RelayCommand` |
| **DI 容器** | 完整 DI（OsuPlayerDbContext 走 `Func<>`、装饰器、`INavigationService` Transient、IUserPreferences/IAppPaths/IAppNotificationService 全部 DI） | `Microsoft.Extensions.DependencyInjection` 但 DbContext 直接 `new`、大量 `ServiceProviders.Default.GetService<T>()` |
| **导航** | `INavigationService` + `FrameNavigationService` + `INavigationAware` + `WeakReferenceMessenger` | `SwitchRadio` + `AnimatedFrame` + `CheckAndAction` API |
| **数据库** | EF Core 7+ + Dapper fallback + snake_case + EF Migrations + Legacy migrator | EF Core 6 早期 + `partial class` 拆业务方法 + `ServiceProviders.GetApplicationDbContext()` 手动 |
| **音频引擎** | KeyASIO + SoundTouch + 独立 `OsuAudio` 模块 + 12s 预缓存窗口 + `OsuBeatmapAudioSession` 12 秒预缓存 | 自研 `OsuMixPlayer`（基于 `Milki.Extensions.MixPlayer`） + 三轨道 + 三音量 + OGG 嵌入 |
| **音频后端** | `KeyASIO.Net` 子模块（独立仓库） | 内联 `Milki.Extensions.MixPlayer` 0.0.30 |
| **全局快捷键** | `OverallKeyHook`（未走 DI）+ 自定义 `HotKey` Json 编码为 Int64 | `KeyHookService`（走 DI）+ `BindKeys` YAML 编码 |
| **歌词** | `OsuPlayer.Media.Lyric` 自研（4 个 source provider，缓存未实现） | `LyricsService` 包装 `LyricsFinder` NuGet（缓存未实现） |
| **设置持久化** | `AppSettings.json` + `IUserPreferences` 接口 + bridge properties | `AppSettings.yaml` + 静态 `AppSettings.Default` 访问 |
| **路径** | `IAppPaths`/`AppPaths` 抽象 + DI | `AppSettings.Directories` 静态内嵌类 |
| **视觉/动效** | 朴素 | 极其丰富：CornerRadiusAnimation、AcrylicPanel、AcrylicBrush、Multi_BorderClipConverter、ContentDialog、CardCollectionControl 差量更新、AnimatedScrollViewer 平滑滚动、AnimatedFrame 淡入缩放、EasingFunction 5 种缓动 |
| **自定义控件** | 仅标准控件 + 一些转换器 | DropShadowPanel / OutlinedTextBlock / UiButton / SwitchRadio / VirtualizingGalleryWrapPanel / AnimatedScrollViewer / AnimatedFrame / ContentDialog / Pagination / CardCollectionControl / DifficultyBadge / NotifyControl |
| **测试** | xUnit 音频模块测试 | `WpfControlsTest` 测试 UI 控件 |
| **CI/CD** | GitHub Actions + SLNX | GitHub Actions |
| **命名空间** | `OsuPlayer.*` | `Milki.OsuPlayer.*` |

---

## 四、3.0 分支可以学习吸收的点

尽管 3.0 是被搁置的分支且 main 的架构更现代，但 3.0 在**UI/UX 细节**和**视觉表达**上确实领先，可以反向吸收。

### 1. 自定义控件库（可整批移植）

**直接价值**。DropShadowPanel、OutlinedTextBlock、SwitchRadio（带 CheckAndAction 跨页 API）、AnimatedScrollViewer、AnimatedFrame、Pagination、ContentDialog（带 DialogOptionFactory 预设）、CardCollectionControl、VirtualizingGalleryWrapPanel、NotifyControl、UiButton（含 8 笔刷 DP）——这些都是 main 当前缺失的可视化能力。

**建议**：抽出 `OsuPlayer.UI` 独立项目，参考 3.0 资源字典的合并顺序（`i18n → Font → Converter → Easing → Style → Brush → Svg → ...`）。

### 2. `CardCollectionControl` 差量更新 + 懒加载策略

main 的 `VirtualizingGalleryWrapPanel` 已有虚拟化（`3fdb4e28` 修复空引用），但 3.0 的 `CardCollectionControl` 多了**懒加载缩略图**（`DelayLoadPlayItem` → `CommonUtils.GetThumbByBeatmapDbId`）+ **`_existsObjHashSet` 差量更新 `VisiblePlayItems` ObservableCollection**。

**吸收点**：把 `DelayLoadPlayItem` 逻辑搬到 `OsuPlayer.Presentation` 或新 UI 项目，统一所有卡片视图的缩略图加载策略。

### 3. `SwitchRadio.CheckAndAction(Action<FrameworkElement>)` API

3.0 的核心跨页调用模式（`CheckAndAction`）："如果当前未选中则先选中触发导航，导航完成后回调"。main 的 `WeakReferenceMessenger` + `SearchRequestedMessage` 能达到同样目的但更松散，3.0 的写法更直接。

**吸收点**：在 `INavigationService` 加 `Task NavigateAndExecuteAsync<TPage>(Action<TPage> action, object? parameter = null)` 扩展。

### 4. `EasingFunction.xaml` + `SvgDictionary.xaml` 集中化

main 现在的 EasingFunction 内联在各个动画中，没有集中资源。3.0 在 `EasingFunction.xaml:1-10` 集中 5 个缓动（`QuinticEaseOut/SineEaseOut/CircEaseIn/Out/PowerEaseOut5`），可作为标准资源引用。

**吸收点**：把 5 个 EasingFunction 抽到 `Styles/EasingFunction.xaml`（3.0 路径是 `src/OsuPlayer/ResourceDictionaries/`），全部动画改用 `StaticResource` 引用。

### 5. `AcrylicPanel` / `AcrylicBrush` 替代 BlurEffect

3.0 的 `ecd56334`、`d3a94a27` 把 `BlurEffect` 改成 `AcrylicPanel` / `AcrylicBrush`，WPF 原生亚克力玻璃更省性能。main 当前还在用 BlurEffect 或纯色叠加。

**吸收点**：评估 `FluentWPF`（`H.NotifyIcon.Wpf 2.0.64` 引入）引入 `AcrylicPanel`。

### 6. `Multi_*` 多值转换器丰富

3.0 有 5 个多值转换器（`Multi_BorderClipConverter`、`Multi_EqualityToVisibilityConverter`、`Multi_ListViewSelectAndScrollConverter`、`Multi_PercentAndActualWidthToWidth`、`MarkdownConverter`），main 当前几乎没有。

**吸收点**：特别是 `Multi_EqualityToVisibilityConverter`（多值相等→Visible）可替代多个 `BoolToVisibilityConverter` 链式判断。

### 7. 40 个 Converters 中的小工具

`GetOutlinedTextConverter`（首字母缩写占位头像）、`Byte2SizeStringConverter`（字节→可读尺寸）、`IndexToStringConverter`（`int`→`"01"` 两位数字）、`MsToStringConverter`（毫秒→mm:ss）都是 main 没有但 UI 经常用得到的小工具。

### 8. 自定义 HitSound 资源从 WAV 改为 OGG

3.0 已经把 hitsound 嵌入从 `wav` 改为 `ogg`（`OsuPlayer.Audio.csproj:14-16`），体积减少明显。main 当前还在用 WAV。

**吸收点**：检查 main 的 `OsuPlayer.Audio/resources/default/*.wav`，看是否值得迁 OGG 嵌入。

### 9. `PathUtils.StandardizePath` 高效实现

`OsuPlayer.Shared/Utils/PathUtils.cs:26-51` 用 `string.Create` 高效构造标准化路径，main 的 `OsuPlayer.Shared/PathUtils` 没有这个文件。main 当前在多个地方重复写"标准化相对路径"逻辑。

**吸收点**：把 `StandardizePath` 抽到 `OsuPlayer.Shared` 共享。

### 10. `OsuBeatmapAudioSession` 设计 vs `OsuMixPlayer` 设计的对比

main 的 `OsuBeatmapAudioSession` 已经在 main 落地（**实际是更先进的版本**），3.0 的 `OsuMixPlayer` 反而是较老的实现。**这部分 main 不需要向 3.0 学习**，但可以参考 3.0 的 `OsuMixPlayer.LoadMetaFinished` 事件链触发顺序（10+ 步），与 main 的 `PlayerSessionService` 对比验证状态机完整性。

### 11. UI 控件测试项目

`WpfControlsTest`（`9ab288c2`）是 main 没有的。视觉/自定义控件应当有最小化快照/视觉测试。

**吸收点**：把 3.0 的 `WpfControlsTest` 移植并扩展，覆盖 `DropShadowPanel/UiButton/SwitchRadio/OutlinedTextBlock` 等。

### 12. `CornerRadiusAnimation` 自定义 Timeline

`src/OsuPlayer/CornerRadiusAnimation.cs:1-101` 把 `CornerRadius` 转 `Thickness` 后用内部 `ThicknessAnimation` 实现动画，是 WPF 内置能力的优雅变通。main 没有这种自定义 Timeline。

**吸收点**：评估是否在 main 引入类似 `CornerRadiusAnimation` 用于折叠/展开动画。

### 13. **`NavigationBar` 独立控件 + 折叠动画**

3.0 的 `NavigationBar.xaml.cs:1-48` 把导航栏抽为独立控件 + `SoftwareState.ShowFullNavigation` 持久化折叠状态 + `BtnNavigationTrigger_Click` 切换。main 当前导航栏内嵌在 `MainWindow.xaml`，折叠动画走样式（`87bc1040` 已优化）。3.0 的"独立控件 + 独立持久化"更易维护。

**吸收点**：评估是否将 `NavigationBar` 抽为独立 UserControl。

### 14. `DialogOptionFactory` 预设工厂

`ContentDialogComponent/DialogOptionFactory.cs:1-46` 提供 4 种预设（`DiffSelectOptions/SelectPlayListOptions/AddCollectionOptions/EditPlayListOptions`），避免每个调用方重复构造。

**吸收点**：main 的 `EntrySetup.cs` 已经把所有 service 注册为 Singleton，但**对话框选项的预设模式**值得借鉴。

---

## 五、综合判断

**main 是工程主导的演进**：把 2.x 的耦合架构按"领域/应用/接口/基础设施"分层，用 EF Core + CommunityToolkit.Mvvm + 强类型 DI 全面替换自研实现。它的问题是**视觉表达力不足**（朴素 WPF + 几张背景图）。

**3.0 是产品主导的重写**：保留业务核心、把整套 UI 控件库重新实现为 FluentWPF 风格的现代视觉，并嵌入完整视觉资源（hit sound OGG、SVG 字典、5 种缓动）。它的问题是**架构停留在 2019-2020 年代**（.NET 6、Service Locator 残留、自研 MVVM、DbContext 手动管理）。

**最有价值的吸收方向**（按 ROI 排序）：
1. 整批移植自定义 UI 控件库（DropShadowPanel/UiButton/OutlinedTextBlock/SwitchRadio/AnimatedScrollViewer/AnimatedFrame/ContentDialog/Pagination/CardCollectionControl/VirtualizingGalleryWrapPanel）
2. 集中 EasingFunction + SvgDictionary 资源
3. `SwitchRadio.CheckAndAction` 模式整合到 `INavigationService`
4. `CardCollectionControl` 的差量更新 + 缩略图懒加载策略
5. OGG 嵌入替换 WAV
6. `WpfControlsTest` 视觉测试项目
7. `AcrylicPanel` 替代 BlurEffect
