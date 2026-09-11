# Chapter 5: Representing the Maze

*Part II — Building the Maze*

## What this chapter covers

- The maze as a grid of cells and walls
- Directions, headings and neighbour lookups
- Designing a Maze class that is easy to test

## Changes since Chapter 4

**New files**

- `MonsterMaze.Core/Mazes/Direction.cs`
- `MonsterMaze.Core/Mazes/Maze.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter05.slnx` in Rider or Visual Studio, or build from the command line:

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
