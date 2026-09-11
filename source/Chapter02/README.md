# Chapter 2: Setting Up Your Tools

*Part I — Foundations*

## What this chapter covers

- Installing the .NET SDK and the MonoGame project templates
- Choosing an IDE: Rider or Visual Studio
- Configuring the Android SDK, emulators and physical devices
- Setting up Xcode, signing and provisioning, and deploying to an iPhone or iPad
- Mac users: preparing a Windows virtual machine in Parallels for compiling shaders
- Building and running “Hello Maze” on both platforms

## Changes since Chapter 1

**New files**

- `MonsterMaze.Core/Content/Fonts/Large.spritefont`
- `MonsterMaze.Core/Content/Fonts/Medium.spritefont`
- `MonsterMaze.Core/Content/Fonts/README.txt`
- `MonsterMaze.Core/Content/Fonts/Roboto-Bold.ttf`
- `MonsterMaze.Core/Content/Fonts/Small.spritefont`
- `MonsterMaze.Core/Content/Fonts/Title.spritefont`
- `MonsterMaze.Core/GameAssets.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/MonsterMazeGame.cs`


## Building and running

Open `Chapter02.slnx` in Rider or Visual Studio, or build from the command line:

```
dotnet build MonsterMaze.Android/MonsterMaze.Android.csproj
dotnet build MonsterMaze.iOS/MonsterMaze.iOS.csproj -r iossimulator-arm64
```

To run on a connected Android device or emulator:

```
dotnet build MonsterMaze.Android/MonsterMaze.Android.csproj -t:Run
```

To run in the iOS Simulator:

```
dotnet build MonsterMaze.iOS/MonsterMaze.iOS.csproj -t:Run -r iossimulator-arm64
```
