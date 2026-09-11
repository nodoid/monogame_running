# Chapter 34: Game Over State Two: Escaped!

*Part VIII — Game Over*

## What this chapter covers

- Stepping through the exit as light floods in
- Adding up the escape bonus
- The victory screen
- Moving on to the high score table

## Changes since Chapter 33

**New files**

- `MonsterMaze.Core/Content/Audio/Sfx/score_tick.wav`
- `MonsterMaze.Core/Content/UI/escape_sky.png`
- `MonsterMaze.Core/Gameplay/EscapeSequence.cs`
- `MonsterMaze.Core/Screens/EscapedScreen.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Gameplay/GameSession.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`

**Removed files**

- `MonsterMaze.Core/Screens/GameOverScreen.cs`


## Building and running

Open `Chapter34.slnx` in Rider or Visual Studio, or build from the command line:

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
