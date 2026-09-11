# Chapter 6: Generating Random Mazes

*Part II — Building the Maze*

## What this chapter covers

- A brand-new, randomly created maze for every game
- Perfect mazes and the recursive backtracker algorithm
- Random seeds: a fresh seed for each game, and fixed seeds for reproducible mazes when testing
- Adding loops and dead ends to control difficulty
- Placing the start and the exit as far apart as possible using breadth-first search
- Making sure every maze can be solved

## Changes since Chapter 5

**New files**

- `MonsterMaze.Core/Mazes/DistanceMap.cs`
- `MonsterMaze.Core/Mazes/MazeGenerator.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Mazes/Maze.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter06.slnx` in Rider or Visual Studio, or build from the command line:

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
