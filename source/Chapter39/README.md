# Chapter 39: Pausing and the Mobile App Life Cycle

*Part IX — Polish and Release*

## What this chapter covers

- Pausing automatically when the app loses focus
- Handling phone calls, notifications and backgrounding
- Resuming safely in the middle of a chase

## Changes since Chapter 38

**New files**

- `MonsterMaze.Core/Content/Audio/Sfx/countdown_beep.wav`
- `MonsterMaze.Core/Content/Audio/Sfx/countdown_go.wav`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/AudioManager.cs`
- `MonsterMaze.Core/Audio/GameplayAudio.cs`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/Screens/PauseScreen.cs`


## Building and running

Open `Chapter39.slnx` in Rider or Visual Studio, or build from the command line:

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
