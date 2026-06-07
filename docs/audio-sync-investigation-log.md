# Audio Sync Investigation Log

日期：2026-06-07

目标：在不引入 BASS 运行时依赖的前提下，定位并修复 osu-player 中 MP3 音轨相对 osu!/BASS 的固定提前偏移问题。

## 背景判断

实测现象是：

- MP3 相对 osu!/BASS 存在偏移。
- 不同 MP3 的偏移可能不同，但同一个 MP3 的偏移固定。
- 所有不一致都是 osu-player 提前，没有发现延后。
- OGG 可能有轻微感知差异，但主要问题集中在 MP3。

初步判断这不是播放线程随机延迟，而是 decoder timeline 的差异。重点怀疑 MP3 encoder delay、padding、Xing/Info/LAME/iTunSMPB gapless metadata，以及 BASS 的 `BASS_CONFIG_MP3_OLDGAPS` 行为。

## 参考实现与外部线索

参考过：

- `ppy/osu-framework`
- `ppy/osu`
- `ManagedBass/ManagedBass`
- 本项目内 `KeyASIO.Net` 的 NAudio 解码路径

关键线索：

- osu-framework 使用 BASS 作为音频底层。
- BASS 的 `BASS_CONFIG_MP3_OLDGAPS` 会影响 MP3 gapless/encoder-delay 语义。
- `NAudio.MediaFoundationReader` 可能会自动按 gapless metadata 裁剪 encoder delay。
- 当前代码已经避免 MP3 走 MediaFoundation，改为 frame-based 的 `Mp3FileReaderBase`，但仍可能和 BASS 在 Xing/Info metadata frame timeline 上不同。

## 尝试 1：直接引入 BASS 解码器

曾经做过一次直接 BASS runtime decoder 替换实验：

- 在 `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/KeyAsio.Core.Audio.csproj` 添加过：
  - `ppy.ManagedBass`
  - `ppy.osu.Framework.NativeLibs`
- 新增过 `Wave/BassAudioDecoder.cs`
- 修改过 `AudioCacheManager`，让 MP3/OGG 优先走 BASS decode stream。
- 当时构建和基础测试能通过，也做过临时 MP3 decode probe。

结果：

- 技术方向可行。
- 但用户明确要求避免 BASS 依赖，因为商业不友好。
- 所以该方向被撤回，不作为最终方案。

当前状态：

- `BassAudioDecoder.cs` 已删除。
- `KeyAsio.Core.Audio.csproj` 不再引用 `ppy.ManagedBass` 或 `ppy.osu.Framework.NativeLibs`。
- BASS 只作为外部 oracle/参考，不进入产品运行时依赖。

## 尝试 2：移除 BASS，建立黑盒参考测试

新增测试文件：

- `tests/OsuPlayer.Media.Audio.Tests/AudioDecoderReferenceTests.cs`

新增说明文档：

- `docs/audio-sync-black-box-tests.md`

测试设计：

- 使用 `OSUPLAYER_AUDIO_SYNC_FIXTURES` 指向 fixture 根目录。
- 每个 fixture 目录包含：
  - `input.mp3` 或 `input.ogg`
  - `reference.wav`
  - 可选 `case.json`
- `reference.wav` 是外部 oracle 生成的 stereo PCM16 WAV。
- 测试用当前运行时 decoder 解码 `input.*`，再和 `reference.wav` 做黑盒对齐比较。

默认阈值：

- `maxSearchMilliseconds`: 250
- `windowMilliseconds`: 100
- `allowedOffsetFrames`: 24
- `durationToleranceFrames`: 96
- `minCorrelation`: 0.985

结果：

- 在没有外部 fixture 时，corpus test 是 no-op。
- 内建 offset estimator 自测通过。
- 当时测试通过：47/47。

局限：

- 只能检测问题，不能自动修正运行时 PCM。
- 没有真实 `reference.wav` corpus 时，无法对具体 MP3 生成校准数据。

## 尝试 3：黑盒测算 + 运行时校准替代 BASS

为了让黑盒测算不只是测试，而能参与运行时修正，新增了以下核心代码。

新增文件：

- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioSourceHash.cs`
- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioDecodeCalibration.cs`
- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/IAudioDecodeCalibrationProvider.cs`
- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioDecodeCalibrationStore.cs`
- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioDecodeCalibrationApplier.cs`

修改文件：

- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioCacheManager.cs`

设计：

- 校准数据按压缩音频内容 hash 命中，而不是按路径命中。
- 运行时通过环境变量 `OSUPLAYER_AUDIO_DECODE_CALIBRATIONS` 读取 JSON manifest。
- `AudioCacheManager` 在完成 NAudio 解码、统一成目标 PCM16 stereo 后，查找 hash 对应的校准值。
- 如果命中，则应用 PCM 修正：
  - `offsetFrames < 0`：当前 decoder 比 reference 提前，补前导静音。
  - `offsetFrames > 0`：当前 decoder 比 reference 延后，跳过开头。
  - `durationDeltaFrames`：根据 reference 长度截尾或补尾部静音。

测试增强：

- `AudioDecoderReferenceTests` 现在会对每个 fixture 跑两次：
  1. raw decode：测出当前 decoder 相对 `reference.wav` 的 offset 和 length delta。
  2. corrected decode：把测出来的校准临时注入 `AudioCacheManager`，再次解码并验证已经回到阈值内。
- 设置 `OSUPLAYER_AUDIO_SYNC_CALIBRATION_OUTPUT` 后，会输出可供运行时使用的 manifest。
- 新增无需外部 fixture 的合成测试：
  - `CalibrationStore_CorrectsEarlyCandidateAndLengthDelta`
  - `CalibrationStore_CorrectsLateCandidate`

验证结果：

- `dotnet test tests\OsuPlayer.Media.Audio.Tests\OsuPlayer.Media.Audio.Tests.csproj -c Debug --no-restore`
- 通过：51/51。
- 仍有既有 `CA1416` Windows-only ASIO analyzer warnings，和本次音频校准改动无关。

局限：

- 仍需要外部 reference corpus 才能为真实 MP3 生成 manifest。
- 本仓库没有现成 MP3 fixture；扫描到的主要是 WAV 和少量 OGG。

撤回状态：

- 后续 Aihana 实测证明主问题可以直接由 MP3 Xing/Info LAME/Lavf/Lavc gapless header 修复。
- 动态 manifest 校准对当前问题没有实际收益，且会把运行时行为变复杂。
- 因此该方向已撤回，不再作为当前代码方案保留。

## 当前样本：Junk - Aihana

用户反馈：

- `E:\Games\osu!\Songs\Junk - Aihana` 中的 MP3 仍然不准。

已检查目录：

- 音频文件：`E:\Games\osu!\Songs\Junk - Aihana\audio.mp3`
- 文件大小：3,457,197 bytes
- 多个 `.osu` 文件均指向：
  - `AudioFilename: audio.mp3`
  - `AudioLeadIn: 0`
- 因此当前样本的问题不是 `.osu` 的 `AudioLeadIn`。

`ffprobe` 结果：

- codec: mp3
- sample_rate: 48000
- channels: 2
- stream duration: 144.024000
- bit_rate: 192000
- stream tag encoder: `Lavc57.33`
- format tag encoder: `Lavf57.29.101`

文件头观察：

- 开头有 ID3v2 tag。
- 随后第一帧附近包含 `Info` header。
- `Format-Hex` 可见：
  - `ID3`
  - `Lavf57.29.101`
  - `FF FB ...`
  - `Info`
  - `Lavc57.33`

当前强嫌疑：

- 该 MP3 是 48kHz 的 Lavf/Lavc CBR `Info` header 文件。
- NAudio `Mp3FileReaderBase` 在识别 `XingHeader.LoadXingHeader(firstFrame)` 后，会将 `dataStartPosition` 移到第一帧之后，即跳过 Xing/Info header frame。
- 如果 BASS/osu 的 old gaps 语义把这个 metadata frame 保留在 timeline 中，差值可能正好是一帧：
  - MPEG1 Layer3 每帧 1152 samples
  - 48kHz 下 `1152 / 48000 = 24ms`
- 这与“osu-player 提前”方向吻合。

注意：

- 这是目前的高可信假设，但还没完成 BASS 实测确认。

## 尝试 4：用本机 osu! 的 bass.dll 做一次性 oracle

目的：

- 不把 BASS 加回项目依赖。
- 只在调查阶段调用 `E:\Games\osu!\bass.dll`，测出该 MP3 的 BASS decode length / timeline。
- 对比 NAudio 输出，确认是否差一个 Info frame 或其它固定帧数。

已确认：

- `E:\Games\osu!\bass.dll` 存在。
- 该 dll 是 osu! stable 目录自带版本。
- 本机存在 x86 dotnet runtime：
  - `C:\Program Files (x86)\dotnet\dotnet.exe`
- 本机存在 x64 dotnet SDK：
  - `C:\Program Files\dotnet\dotnet.exe`

失败的尝试：

- 试图通过 `C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe` 运行 x86 PowerShell。
- 在 inline command 里用 `Add-Type` P/Invoke `bass.dll`。
- 失败原因不是 BASS 本身，而是命令字符串引用方式错误：
  - 外层 PowerShell 双引号提前展开了 `$ok`、`$h`、`$len` 等变量。
  - 内层脚本收到的代码变成 `if(-not )`、`BASS_ChannelGetLength(,0)` 这类无效语法。

下一步应改为：

- 写临时 `.ps1` 脚本再由 x86 PowerShell 执行，或
- 使用 `-EncodedCommand`，避免变量被外层 PowerShell 展开。

后续已完成：

- 改用 `-EncodedCommand` 成功调用 x86 PowerShell。
- `DllImport("bass.dll")` 无法按工作目录加载，改为 `DllImport(@"E:\Games\osu!\bass.dll")`。
- `BASS_ChannelFree` 在该版本 DLL 中不可用，改用 `BASS_StreamFree`。
- `BASS_ChannelGetData` 到结尾返回 error 45，按 `BASS_ChannelGetLength` 读满后停止。
- 生成了临时 BASS reference WAV：
  - `C:\Users\milki\AppData\Local\Temp\osu-player-audio-sync-fixtures\Junk-Aihana\reference.wav`
  - bytes: 27,648,188
  - frames at 48kHz stereo PCM16: 6,912,047
  - seconds: 144.00097916666667

还确认：

- `BASS_SetConfig(68, 1)` 返回成功。
- 对该样本，`BASS_CONFIG_MP3_OLDGAPS` 的 0/1 输出长度相同，都是 6,912,047 frames。

## 尝试 5：Aihana 黑盒测算结果

用 BASS reference WAV 作为 fixture 跑：

```powershell
$env:OSUPLAYER_AUDIO_SYNC_FIXTURES = "$env:TEMP\osu-player-audio-sync-fixtures"
$env:OSUPLAYER_AUDIO_SYNC_CALIBRATION_OUTPUT = "$env:TEMP\osu-player-audio-sync-fixtures\audio-decode-calibrations.json"
dotnet test tests\OsuPlayer.Media.Audio.Tests\OsuPlayer.Media.Audio.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~AudioDecoderReferenceTests.ReferenceCorpus_AllCasesStayWithinOffsetBudget"
```

结果：

- `Junk-Aihana: rawOffset=1105 frames (23.021ms)`
- `rawLengthDelta=1105 frames`
- `correctedOffset=0 frames`
- `correctedLengthDelta=0 frames`
- `corr=1.00000`

这说明当前 NAudio 解码结果相对 BASS reference 多了开头 1105 samples，且总长度也多 1105 samples。

`ffprobe` 同时给出：

- 第一包 `Skip Samples = 1105`
- `discard_padding = 0`
- `start: 0.023021`

FFmpeg 源码中的 MP3 demuxer 逻辑说明：

- 从 Xing/Info 后的 LAME/Lavf/Lavc extension 读取 12-bit encoder delay 和 12-bit padding。
- start skip = encoder delay + 528 + 1。
- 对该样本，encoder delay 是 576，因此 start skip 是 1105。

## 尝试 6：自动 MP3 gapless 修正

新增：

- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/Mp3GaplessInfo.cs`

修改：

- `dependencies/KeyASIO.Net/src/Core/KeyAsio.Core.Audio/Caching/AudioCacheManager.cs`
- `tests/OsuPlayer.Media.Audio.Tests/AudioDecoderReferenceTests.cs`

运行时策略：

- 尝试解析 MP3 Xing/Info LAME/Lavf/Lavc gapless 信息。
- 按 header 自动计算：
  - `OffsetFrames = encoderDelay + 529`
  - `DurationDeltaFrames = OffsetFrames + max(0, encoderPadding - 529)`
- 在 PCM cache 阶段裁剪开头并修正长度。

为了让测试仍能测 raw offset，`AudioCacheManager` 增加了可选参数：

- `useAutomaticMp3GaplessCorrection`
- 默认值为 `true`
- 测试 raw 测算时显式传 `false`

验证默认运行时自动修正：

- 临时 probe 直接调用默认 `AudioCacheManager` 解 `E:\Games\osu!\Songs\Junk - Aihana\audio.mp3`
- 与 BASS reference WAV 比较长度：
  - decodedFrames = 6,912,047
  - referenceFrames = 6,912,047
  - deltaFrames = 0

## 尝试 7：补齐自动修正回归测试

新增测试覆盖：

- `Mp3GaplessInfo_ReadsLavcInfoHeaderDelayAndPadding`
  - 构造带 ID3v2 前缀、MPEG1 Layer3 48kHz stereo header、`Info` marker、`Lavc57.33` encoder 字段的合成 MP3 frame。
  - 验证 `encoderDelay=576` 时自动解析出：
    - `StartSkipSamples = 1105`
    - `EndDiscardSamples = 0`
    - `TotalDiscardSamples = 1105`
  - 同时验证 padding 大于 decoder delay 时，尾部 discard 会被计入。

同时增强了 fixture corpus test：

- raw decode 仍然关闭自动修正，用来测原始偏移。
- 默认 decode 开启 MP3 header 修正，用来验证当前运行时路径。

验证结果：

- 普通测试：
  - `dotnet test tests\OsuPlayer.Media.Audio.Tests\OsuPlayer.Media.Audio.Tests.csproj -c Debug --no-restore`
  - 通过：51/51。
- Aihana fixture：
  - `rawOffset=1105 frames (23.021ms)`
  - `rawLengthDelta=1105 frames`
  - `correctedOffset=0 frames`
  - `correctedLengthDelta=0 frames`

## 尝试 8：撤回动态校准，保留 MP3 header 修复

用户判断：

- 中间的动态校准实测价值不大。
- 针对 MP3 头的修复有效。
- 因此撤回动态校准相关运行时能力。

已撤回：

- `AudioDecodeCalibration`
- `IAudioDecodeCalibrationProvider`
- `AudioDecodeCalibrationStore`
- `AudioDecodeCalibrationApplier`
- `AudioSourceHash`
- 环境变量 `OSUPLAYER_AUDIO_DECODE_CALIBRATIONS`
- corpus test 中“测 raw -> 生成 manifest -> 回放 manifest”的流程

当前保留：

- `Mp3GaplessInfo`：解析 ID3v2 后第一帧里的 Xing/Info LAME/Lavf/Lavc gapless 信息。
- `Mp3GaplessAudioTrimmer`：只根据 MP3 header 的 start skip / end discard 裁剪 PCM。
- `AudioCacheManager` 的默认自动 MP3 gapless 修正。
- 测试用 `useAutomaticMp3GaplessCorrection=false` 开关，用于黑盒测试输出 raw offset，不进入产品配置面。

新的 fixture corpus test 语义：

1. 关闭 MP3 header 修正，测 raw offset/length delta，作为诊断输出。
2. 开启默认运行时 MP3 header 修正，直接对齐 `reference.wav` 并验证阈值。

## 当前代码状态

根仓库：

- `dependencies/KeyASIO.Net` 子模块 dirty。
- 新增：
  - `docs/audio-sync-black-box-tests.md`
  - `docs/audio-sync-investigation-log.md`
  - `tests/OsuPlayer.Media.Audio.Tests/AudioDecoderReferenceTests.cs`

子模块 `dependencies/KeyASIO.Net`：

- 当前分支：`dev/bass-decoder`
- 修改：
  - `src/Core/KeyAsio.Core.Audio/Caching/AudioCacheManager.cs`
  - `src/Core/KeyAsio.Core.Audio/KeyAsio.Core.Audio.csproj`
- 删除：
  - `src/Core/KeyAsio.Core.Audio/Wave/BassAudioDecoder.cs`
- 新增：
  - `src/Core/KeyAsio.Core.Audio/Caching/Mp3GaplessInfo.cs`
  - `src/Core/KeyAsio.Core.Audio/Caching/Mp3GaplessAudioTrimmer.cs`
  - `src/Core/KeyAsio.Core.Audio/Properties/AssemblyInfo.cs`

BASS runtime 依赖状态：

- 项目文件中没有 `ppy.ManagedBass`。
- 项目文件中没有 `ppy.osu.Framework.NativeLibs`。
- `BassAudioDecoder.cs` 不存在。
- 当前只有文档和注释提到 BASS，作为兼容语义说明或外部 oracle。

## 当前结论

1. 不需要知道 BASS 源码才能做替代。
2. 需要知道 BASS/osu 对具体文件的可观察输出行为。
3. 当前最稳的无 BASS 运行时方案是：
   - 外部 oracle 生成 `reference.wav`。
   - 黑盒测试测出 raw offset/length delta 作为诊断。
   - 运行时解析 MP3 header 并直接修正 PCM。
4. 对标准 Xing/Info LAME/Lavf/Lavc gapless metadata，可以自动解析并修正。
5. 对 `Junk - Aihana/audio.mp3`，最终确认的差异是 1105 samples，即 23.021ms。
6. 当前默认运行时自动修正已能让该样本的 decoded length 和 BASS reference 对齐。

## 后续建议

优先继续做两件事：

1. 收集更多真实 MP3 样本，尤其是 LAME VBR、iTunes iTunSMPB、无 gapless metadata 的老 MP3。
2. 用 BASS oracle 批量生成 reference WAV，跑 `OSUPLAYER_AUDIO_SYNC_FIXTURES`，确认自动规则覆盖率。
3. 对自动规则无法覆盖的文件，优先补对应 header/metadata 解析或 decoder 行为修复，不再走动态 manifest 校准。
