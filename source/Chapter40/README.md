# Chapter 40: Performance on Mobile Devices

*Part IX — Polish and Release*

## What this chapter covers

- Measuring frame rate and draw calls
- Keeping high-resolution rendering smooth: fill rate and resolution scaling
- Avoiding garbage collection spikes
- Battery life, heat and target frame rates

## Changes since Chapter 39

**New files**

- `MonsterMaze.Core/Diagnostics/FrameCounter.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Effects/PostProcessor.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Rendering/QualityProfile.cs`
- `MonsterMaze.Core/Screens/OptionsScreen.cs`
- `MonsterMaze.Core/UI/FontSet.cs`


## Building and running

Open `Chapter40.slnx` in Rider or Visual Studio, or build from the command line:

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
