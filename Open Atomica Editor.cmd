@echo off
set DOTNET_ROLL_FORWARD=LatestMinor
set DOTNET_CLI_HOME=%~dp0.tools\dotnet-home
set NUGET_PACKAGES=%~dp0.tools\nuget
set MSBuildEnableWorkloadResolver=false
start "" "%~dp0.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --editor --path "%~dp0Prototype"
