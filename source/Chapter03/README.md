# Chapter 3: One Game, Two Platforms

*Part I — Foundations*

## What this chapter covers

- Structuring the solution: a shared game library with Android and iOS projects
- The MonoGame Content Pipeline and the MGCB Editor
- Keeping platform-specific code to a minimum
- Handling screen sizes, aspect ratios, orientation and safe areas (notches and rounded corners)

## Changes since Chapter 2

**New files**

- `MonsterMaze.Android/AndroidPlatformServices.cs`
- `MonsterMaze.Core/Platform/IPlatformServices.cs`
- `MonsterMaze.Core/Platform/NullPlatformServices.cs`
- `MonsterMaze.Core/UI/FontSet.cs`
- `MonsterMaze.Core/UI/ScreenScaler.cs`
- `MonsterMaze.iOS/IosPlatformServices.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Android/MainActivity.cs`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.iOS/Program.cs`


## Building and running

Open `Chapter03.slnx` in Rider or Visual Studio, or build from the command line:

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
