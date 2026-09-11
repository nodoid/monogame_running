# Chapter 7: Seeing the Maze from Above

*Part II — Building the Maze*

## What this chapter covers

- Drawing a 2D debug map with SpriteBatch
- Showing the player, Rex and the exit on the map
- Developer tools: debug toggles, overlays and displaying the maze seed

## Changes since Chapter 6

**New files**

- `MonsterMaze.Core/Diagnostics/DebugOverlay.cs`
- `MonsterMaze.Core/Rendering/MazeMapRenderer.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter07.slnx` in Rider or Visual Studio, or build from the command line:

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
