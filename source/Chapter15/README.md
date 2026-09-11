# Chapter 15: Finding the Exit

*Part III — Into the Third Dimension*

## What this chapter covers

- Making the exit stand out: light spilling around the corner
- Detecting when the player reaches the exit
- Starting the escape sequence

## Changes since Chapter 14

**New files**

- `MonsterMaze.Core/Content/Textures/exit_light.png`
- `MonsterMaze.Core/Content/Textures/wall_exit.png`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Gameplay/Player.cs`
- `MonsterMaze.Core/Rendering/MazeMesh.cs`
- `MonsterMaze.Core/Rendering/MazeMeshBuilder.cs`
- `MonsterMaze.Core/Rendering/MazeRenderer.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter15.slnx` in Rider or Visual Studio, or build from the command line:

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
