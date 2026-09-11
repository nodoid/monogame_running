# Chapter 28: Positional Audio and Tension

*Part VI — Sound*

## What this chapter covers

- AudioEmitter and AudioListener: hearing where Rex is
- Volume and muffling based on distance through the maze
- Using dynamic music, and silence, to build tension
- Mobile audio: the silent switch, interruptions and headphones

## Changes since Chapter 27

**New files**

- `MonsterMaze.Core/Content/Audio/Music/exit_hum.wav`
- `MonsterMaze.Core/Content/Audio/Music/tension_loop.wav`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Audio/GameplayAudio.cs`
- `MonsterMaze.Core/Audio/Sounds.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.iOS/Program.cs`


## Building and running

Open `Chapter28.slnx` in Rider or Visual Studio, or build from the command line:

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
