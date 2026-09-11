# Chapter 35: The Colourful High Score Table

*Part VIII — Game Over*

## What this chapter covers

- Separate tables for Easy, Normal and Hard
- Rainbow cycling text, gradients and glowing new entries
- Animating the table: scrolling, sparkles and colour pulses
- Showing how each game ended: eaten or escaped

## Changes since Chapter 34

**New files**

- `MonsterMaze.Core/Content/Audio/Sfx/new_highscore.wav`
- `MonsterMaze.Core/Data/HighScoreTable.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Screens/HighScoreScreen.cs`
- `MonsterMaze.Core/Screens/ScoreFlow.cs`


## Building and running

Open `Chapter35.slnx` in Rider or Visual Studio, or build from the command line:

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
