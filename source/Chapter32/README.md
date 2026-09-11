# Chapter 32: Transitions and Particles

*Part VII — Visual Effects*

## What this chapter covers

- Fades and wipes between screens
- Dust and debris particles
- Title screen effects

## Changes since Chapter 31

**New files**

- `MonsterMaze.Core/Content/Sprites/particle_dust.png`
- `MonsterMaze.Core/Content/Sprites/particle_soft.png`
- `MonsterMaze.Core/Content/Sprites/particle_spark.png`
- `MonsterMaze.Core/Effects/DustParticles.cs`
- `MonsterMaze.Core/Effects/ParticleSystem2D.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Rendering/QualityProfile.cs`
- `MonsterMaze.Core/Screens/DifficultyScreen.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/Screens/ScreenManager.cs`
- `MonsterMaze.Core/Screens/TitleScreen.cs`


## Building and running

Open `Chapter32.slnx` in Rider or Visual Studio, or build from the command line:

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
