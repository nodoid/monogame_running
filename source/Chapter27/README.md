# Chapter 27: Sound Effects of the Maze

*Part VI — Sound*

## What this chapter covers

- The player’s footsteps and bumping into walls
- Rex’s thundering footsteps, snorts and roar
- A heartbeat that speeds up as Rex gets closer
- Ambient sounds: drips, echoes and distant rumbles
- The sounds of being eaten and of escaping

## Changes since Chapter 26

**New files**

- `MonsterMaze.Core/Audio/GameplayAudio.cs`
- `MonsterMaze.Core/Content/Audio/Music/ambient_loop.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/chomp.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/drip_1.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/drip_2.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/drip_3.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/escape_fanfare.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/heartbeat.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/near_miss.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_growl.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_roar.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_roar_far.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_snort.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_step.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/rex_step_far.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/stinger_seen.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/wall_bump.wav`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter27.slnx` in Rider or Visual Studio, or build from the command line:

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
