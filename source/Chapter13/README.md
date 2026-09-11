# Chapter 13: Moving Through the Maze

*Part III — Into the Third Dimension*

## What this chapter covers

- Grid-based movement: step forward, step back, turn left and turn right
- Animating steps and 90-degree turns smoothly
- Wall collision and blocked moves
- Head bob and footstep timing

## Changes since Chapter 12

**New files**

- `MonsterMaze.Core/Gameplay/Player.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter13.slnx` in Rider or Visual Studio, or build from the command line:

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
