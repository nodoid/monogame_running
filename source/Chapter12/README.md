# Chapter 12: High-Resolution Graphics on Mobile

*Part III — Into the Third Dimension*

## What this chapter covers

- Rendering at the device’s native resolution, including Retina and high-density Android screens
- Mipmapping, anisotropic filtering and anti-aliasing (MSAA)
- Texture compression formats for Android and iOS
- Graphics quality settings for older and newer devices

## Changes since Chapter 11

**New files**

- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Rendering/QualityProfile.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Rendering/MazeRenderer.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter12.slnx` in Rider or Visual Studio, or build from the command line:

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
