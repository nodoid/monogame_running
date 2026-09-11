# Chapter 22: Scoring

*Part V — Rules, Scoring and Difficulty*

## What this chapter covers

- Points for exploring: rewarding each step
- Survival, near-miss and escape bonuses
- The difficulty multiplier

## Changes since Chapter 21

**New files**

- `MonsterMaze.Core/Gameplay/ScoreKeeper.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Gameplay/GameSession.cs`
- `MonsterMaze.Core/Screens/GameOverScreen.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter22.slnx` in Rider or Visual Studio, or build from the command line:

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
