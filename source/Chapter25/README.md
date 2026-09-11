# Chapter 25: The Heads-Up Display

*Part V — Rules, Scoring and Difficulty*

## What this chapter covers

- Score, difficulty and warning messages
- The pause button and a touch-friendly layout
- Scaling the HUD and SpriteFonts across screen densities

## Changes since Chapter 24

**New files**

- `MonsterMaze.Core/UI/Hud.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter25.slnx` in Rider or Visual Studio, or build from the command line:

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
