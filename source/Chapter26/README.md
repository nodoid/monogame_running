# Chapter 26: Audio in MonoGame

*Part VI — Sound*

## What this chapter covers

- SoundEffect, SoundEffectInstance and Song
- Audio formats and the Content Pipeline for Android and iOS
- An audio manager for sound effects, music and volume settings

## Changes since Chapter 25

**New files**

- `MonsterMaze.Core/Audio/AudioManager.cs`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/Audio/Music/title_music.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/footstep_1.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/footstep_2.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/footstep_3.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/footstep_4.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/ui_back.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/ui_click.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/ui_select.wav`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Screens/DifficultyScreen.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/Screens/MainMenuScreen.cs`
- `MonsterMaze.Core/Screens/TitleScreen.cs`
- `MonsterMaze.Core/UI/Button.cs`


## Building and running

Open `Chapter26.slnx` in Rider or Visual Studio, or build from the command line:

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
