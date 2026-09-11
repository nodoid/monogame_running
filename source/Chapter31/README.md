# Chapter 31: Colour Grading and Glow

*Part VII — Visual Effects*

## What this chapter covers

- Colour grading to shift the mood as the danger rises
- Bloom and glow: the light pouring from the exit
- Keeping colours vivid and readable on phone screens in bright light and in the dark

## Changes since Chapter 30

**New files**

- `MonsterMaze.Core/Content/Effects/Bloom.fx`
- `MonsterMaze.Core/Content/Effects/Compiled/Android/Bloom.xnb`
- `MonsterMaze.Core/Content/Effects/Compiled/iOS/Bloom.xnb`
- `MonsterMaze.Core/Effects/Bloom.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Effects/DangerEffects.cs`
- `MonsterMaze.Core/Effects/PostProcessor.cs`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Rendering/QualityProfile.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter31.slnx` in Rider or Visual Studio, or build from the command line:

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
