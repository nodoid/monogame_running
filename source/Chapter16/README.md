# Chapter 16: Bringing Rex to Life

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Rex as a detailed, full-colour 3D model or high-resolution animated sprites: choosing an approach
- Drawing Rex inside the 3D world with correct depth
- Animating Rex: idle, walking, spotting the player and lunging
- Making Rex more menacing the closer he gets

## Changes since Chapter 15

**New files**

- `MonsterMaze.Core/Content/Sprites/rex_sheet.png`
- `MonsterMaze.Core/Monster/Rex.cs`
- `MonsterMaze.Core/Rendering/RexRenderer.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter16.slnx` in Rider or Visual Studio, or build from the command line:

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
