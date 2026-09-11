# Chapter 21: Tuning the Fear

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Where Rex starts and how long he waits
- Keeping the chase fair: escape routes and near misses
- Playtesting and adjusting the tension

## Changes since Chapter 20

**New files**

- `MonsterMaze.Core/Monster/RexSpawner.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Monster/RexTuning.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter21.slnx` in Rider or Visual Studio, or build from the command line:

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
