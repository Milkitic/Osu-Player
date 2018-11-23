@echo off
taskkill /f /pid %1
del /q *.xml
del /q *.pdb
mkdir bin
move de bin\
move en bin\
move es bin\
move fr bin\
move it bin\
move ja bin\
move ko bin\
move ko bin\
move x86 bin\
move zh-Hans bin\
move zh-Hant bin\
move *.config bin\
move *.dll bin\
move bin\osu.Shared.dll osu.Shared.dll
move bin\osu-database-reader.dll osu-database-reader.dll
move bin\OsuPlayer.exe.config OsuPlayer.exe.config
start OsuPlayer.exe
del /q migrate.bat