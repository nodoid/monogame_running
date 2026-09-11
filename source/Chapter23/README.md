# Chapter 23: Designing Three Difficulty Levels

*Part V — Rules, Scoring and Difficulty*

## What this chapter covers

- Easy, Normal and Hard: what each level should feel like
- What changes between levels: maze size, dead ends and loops, fog distance, Rex’s speed, senses and head start, and how much warning the player gets
- A DifficultySettings class so every system reads from one place

## Changes since Chapter 22

**New files**

- `MonsterMaze.Core/Gameplay/Difficulty.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Gameplay/GameSession.cs`
- `MonsterMaze.Core/Gameplay/WarningSystem.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/Screens/MainMenuScreen.cs`
- `MonsterMaze.Core/UI/Palette.cs`


## Building and running

Open `Chapter23.slnx` in Rider or Visual Studio, or build from the command line:

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
