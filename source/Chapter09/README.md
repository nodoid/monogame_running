# Chapter 9: Turning a Grid into a World

*Part III — Into the Third Dimension*

## What this chapter covers

- Generating wall, floor and ceiling geometry from the maze data
- Vertex and index buffers: building the whole maze as a single mesh
- Only building the faces the player can see
- Marking the exit in the 3D world

## Changes since Chapter 8

**New files**

- `MonsterMaze.Core/Rendering/MazeMesh.cs`
- `MonsterMaze.Core/Rendering/MazeMeshBuilder.cs`
- `MonsterMaze.Core/Rendering/MazeRenderer.cs`
- `MonsterMaze.Core/Rendering/WorldSpace.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`

**Removed files**

- `MonsterMaze.Core/Rendering/TestScene.cs`


## Building and running

Open `Chapter09.slnx` in Rider or Visual Studio, or build from the command line:

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
