# Chapter 36: Entering Your Name

*Part VIII — Game Over*

## What this chapter covers

- Arcade-style initials entry designed for touch
- Using the on-screen keyboard on Android and iOS
- Validating and storing entries

## Changes since Chapter 35

**New files**

- `MonsterMaze.Core/Content/Audio/Sfx/ui_type.wav`
- `MonsterMaze.Core/Screens/NameEntryScreen.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Screens/ScoreFlow.cs`


## Building and running

Open `Chapter36.slnx` in Rider or Visual Studio, or build from the command line:

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
