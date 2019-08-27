# Osu-Player
![](https://img.shields.io/badge/.NET-4.6.1-blue.svg)
![](https://img.shields.io/badge/license-GPL-blue.svg)

**A multifunctional player for playing music, hitsound, video and storyboard for osuers!.**

![](http://puu.sh/CQWkb/7decbef183.png)
![](http://puu.sh/CQWkO/1ef95bc770.png)
![](http://puu.sh/CQYlm/01e9c6417b.jpg)

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
* Support Storyboard (based on [MikiraSora's project](https://github.com/MikiraSora/ReOsuStoryboardPlayer) his GL render and logic)
* Support DT NC HT DC mod (based on NAudio + SoundTouch)
* Support Online Explore (because now api v2 looks fit to use)
* Support slidertick, sliderslider and spinner emulation, support Taiko map playing
* Intelligent recommendation when you exploring maps.

## Compile from source code
To compile the source code, Microsoft Expression Blend SDK for .NET 4 will be needed. You can download the SDK from [https://www.microsoft.com/en-us/download/details.aspx?id=10801].

Then, clone the repo with submodules with `git clone --recursive https://github.com/Milkitic/Osu-Player`

After that, ppen `OsuPlayer.sln` with an IDE that supports .NET Framework 4.6 and compile the source code.

## Dependencies
* Interface: DMSkin, FFME.Windows, Hardcodet.NotifyIcon.Wpf
* Func: MouseKeyHook
* Data: HoLLy.osu.DatabaseReader, OSharp.Beatmap, OSharp.Storyboard, System.Data.SQLite.Core, System.Data.SQLite.EF6.Migrations
* Audio: NAudio, NAudio.Vorbis
