# Chapter 30: Danger Effects

*Part VII — Visual Effects*

## What this chapter covers

- Screen shake as Rex’s footsteps land
- A red vignette and pulsing screen edges as he closes in
- Flicker, flash and blur for moments of panic

## Changes since Chapter 29

**New files**

- `MonsterMaze.Core/Content/UI/vignette.png`
- `MonsterMaze.Core/Effects/DangerEffects.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Effects/PostProcessor.cs`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/UI/Hud.cs`


## Building and running

Open `Chapter30.slnx` in Rider or Visual Studio, or build from the command line:

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
