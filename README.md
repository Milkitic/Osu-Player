# Osu-Player
![](https://img.shields.io/badge/.NET-4.6.1-blue.svg)
![](https://img.shields.io/badge/license-GPL-blue.svg)

**A multifunctional player for playing music, hitsound, video and storyboard for osuers!.**

![](http://puu.sh/CQWkb/7decbef183.png)
![](http://puu.sh/CQWkO/1ef95bc770.png)
![](http://puu.sh/CQYlm/01e9c6417b.jpg)

Now you can close osu! and open osu player after making a cup of tea, and sit down to enjoy your afternoon.

## Compile from source code
To compile the source code, Microsoft Expression Blend SDK for .NET 4 will be needed. You can download the SDK from [https://www.microsoft.com/en-us/download/details.aspx?id=10801].

Then, clone the repo with submodules with `git clone --recursive https://github.com/Milkitic/Osu-Player`

After that, ppen `OsuPlayer.sln` with an IDE that supports .NET Framework 4.6 and compile the source code.

## Dependencies
* Interface: DMSkin, FFME.Windows, Hardcodet.NotifyIcon.Wpf
* Func: MouseKeyHook
* Data: HoLLy.osu.DatabaseReader, OSharp.Beatmap, OSharp.Storyboard, System.Data.SQLite.Core, System.Data.SQLite.EF6.Migrations
* Audio: NAudio, NAudio.Vorbis
