# Chapter 10: The First-Person Camera

*Part III — Into the Third Dimension*

## What this chapter covers

- Placing the camera at eye height inside a cell
- Field of view for phones and tablets in portrait and landscape
- Near and far clipping planes for long corridors

## Changes since Chapter 9

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Rendering/Camera.cs`
- `MonsterMaze.Core/Rendering/MazeMapRenderer.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter10.slnx` in Rider or Visual Studio, or build from the command line:

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
