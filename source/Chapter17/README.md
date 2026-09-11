# Chapter 17: Rex’s Senses

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Grid-based line of sight down corridors
- Hearing: detecting the player’s footsteps through the maze
- Measuring distance in steps through the maze, not in straight lines

## Changes since Chapter 16

**New files**

- `MonsterMaze.Core/Monster/RexSenses.cs`
- `MonsterMaze.Core/Monster/RexTuning.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter17.slnx` in Rider or Visual Studio, or build from the command line:

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
