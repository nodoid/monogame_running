# Chapter 8: 3D Fundamentals in MonoGame

*Part III — Into the Third Dimension*

## What this chapter covers

- Coordinate systems, vectors and matrices
- World, view and projection transforms
- Vertices, primitives and BasicEffect
- The GraphicsDevice: depth buffer, culling and render states

## Changes since Chapter 7

**New files**

- `MonsterMaze.Core/Rendering/Camera.cs`
- `MonsterMaze.Core/Rendering/TestScene.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Mazes/Direction.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter08.slnx` in Rider or Visual Studio, or build from the command line:

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
