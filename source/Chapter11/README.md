# Chapter 11: Textures, Lighting and Atmosphere

*Part III — Into the Third Dimension*

## What this chapter covers

- High-resolution, full-colour textures for walls, floors and ceilings
- Using fog to create darkness and limit how far the player can see
- Coloured torch-style lighting and fall-off with distance
- Art direction: a rich, colourful look that still feels dark and threatening

## Changes since Chapter 10

**New files**

- `MonsterMaze.Core/Content/Textures/ceiling.png`
- `MonsterMaze.Core/Content/Textures/floor.png`
- `MonsterMaze.Core/Content/Textures/wall_crystal.png`
- `MonsterMaze.Core/Content/Textures/wall_moss.png`
- `MonsterMaze.Core/Content/Textures/wall_stone.png`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Rendering/MazeMesh.cs`
- `MonsterMaze.Core/Rendering/MazeMeshBuilder.cs`
- `MonsterMaze.Core/Rendering/MazeRenderer.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter11.slnx` in Rider or Visual Studio, or build from the command line:

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
