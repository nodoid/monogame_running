# Chapter 41: Testing on Real Devices

*Part IX — Polish and Release*

## What this chapter covers

- Testing across Android devices, iPhones and iPads
- Automated tests for the maze generator, pathfinding and scoring
- Beta testing with Google Play testing tracks and TestFlight

## Changes since Chapter 40

**New files**

- `MonsterMaze.Tests/MazeGeneratorTests.cs`
- `MonsterMaze.Tests/MonsterMaze.Tests.csproj`
- `MonsterMaze.Tests/PathfindingTests.cs`
- `MonsterMaze.Tests/SaveStoreTests.cs`
- `MonsterMaze.Tests/ScoringTests.cs`
- `global.json`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/MonsterMaze.Core.csproj`


## Building and running

Open `Chapter41.slnx` in Rider or Visual Studio, or build from the command line:

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

To run the unit tests:

```
dotnet test --project MonsterMaze.Tests
```
