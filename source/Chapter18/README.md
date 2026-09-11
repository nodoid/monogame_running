# Chapter 18: Hunting the Player: Pathfinding

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Breadth-first search and A* on a grid
- Recalculating paths efficiently as the player moves
- Rex’s movement timing and speed

## Changes since Chapter 17

**New files**

- `MonsterMaze.Core/Mazes/Pathfinder.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Mazes/DistanceMap.cs`
- `MonsterMaze.Core/Monster/Rex.cs`
- `MonsterMaze.Core/Monster/RexTuning.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter18.slnx` in Rider or Visual Studio, or build from the command line:

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
