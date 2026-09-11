# Chapter 38: Title Screen, Menus and Options

*Part IX — Polish and Release*

## What this chapter covers

- The title screen and attract mode
- Main menu, difficulty select and options (sound, controls, graphics quality)
- The How to Play screen

## Changes since Chapter 37

**New files**

- `MonsterMaze.Core/Content/UI/logo.png`
- `MonsterMaze.Core/Gameplay/AttractMode.cs`
- `MonsterMaze.Core/Screens/HowToPlayScreen.cs`
- `MonsterMaze.Core/Screens/OptionsScreen.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Screens/HighScoreScreen.cs`
- `MonsterMaze.Core/Screens/MainMenuScreen.cs`
- `MonsterMaze.Core/Screens/TitleScreen.cs`


## Building and running

Open `Chapter38.slnx` in Rider or Visual Studio, or build from the command line:

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
