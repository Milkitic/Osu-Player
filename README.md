# Osu-Player

[![Runtime](https://img.shields.io/badge/.NET-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![License](https://img.shields.io/github/license/milkitic/osu-player)](https://github.com/Milkitic/Osu-Player/blob/master/LICENSE)
[![CodeFactor](https://www.codefactor.io/repository/github/milkitic/osu-player/badge)](https://www.codefactor.io/repository/github/milkitic/osu-player)

[![Releases](https://img.shields.io/github/v/release/Milkitic/Osu-Player)](https://github.com/Milkitic/Osu-Player/releases)
[![Commits](https://img.shields.io/github/commits-since/Milkitic/Osu-Player/latest)](https://github.com/Milkitic/Osu-Player/commits/master)

**A multifunctional player for playing music, hitsound, video and storyboard for osuers!.**

![](http://puu.sh/GQ1Yy/0feabedae9.jpg)
![](http://puu.sh/GQ21u/5916015363.jpg)
![](http://puu.sh/EDjqw/dac1e29cb3.png)

Now you can close osu! and open osu player after making a cup of tea, and sit down to enjoy your afternoon.

## Currently support
* Ordinary media player interface and limited function, including 
> * volume control
> * play control
> * play list
> * my favorites
> * search loacal library
> * video support
> * shortcut
> * lyric
* Beatmap based playing (That's why fully for OSUer), no beatmap you can play nothing. But if you have (easy for osuer), except for playing music, it will
> * play the maps's hitsound (STD and mania)
> * show the background BG
> * play the video

## Future mainly function
* I18N
* Support Storyboard (based on [MikiraSora's project](https://github.com/MikiraSora/ReOsuStoryboardPlayer) his GL render and logic)
* Support DT NC HT DC mod (based on NAudio + SoundTouch)
* Support Online Explore (because now api v2 looks fit to use)
* Support slidertick, sliderslider and spinner emulation, support Taiko map playing
* Intelligent recommendation when you exploring maps.

## Compile from source code
Clone repo with submodules using git command `git clone --recursive https://github.com/Milkitic/Osu-Player`.

Open `OsuPlayer.sln` with Visual Studio, Rider or other .NET-platform IDEs.

Restore all nuget packages.

Compile the source code.

## I18N
Welcome to help to translate this program! The program support localization and display in your preferred language.

See Template: [OsuPlayer.Wpf/lang/en-US.xaml](https://github.com/Milkitic/Osu-Player/tree/master/OsuPlayer.Wpf/lang/en-US.xaml)

Note the file name format should be like `[locale-name].xaml` which can be found at: https://docs.microsoft.com/en-us/cpp/c-runtime-library/language-strings

## Develop
* GUI: OsuPlayer.Wpf
* Core with database, settings and other logics: OsuPlayer.Common
* Audio playing: OsuPlayer.Media.Audio

Welcome to open pull requests if you have some ideas or just want to join in.

If you have a destructive change (framework, lib replacement, etc), please contact me first.

Please open PR and merge to develop branch.
