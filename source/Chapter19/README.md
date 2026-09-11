# Chapter 19: Rex’s Brain: A State Machine

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Lurking: waiting somewhere in the dark
- Hunting: searching for the player
- Stalking: closing in
- Charging: the final run once Rex has seen the player
- Attacking: catching the player
- Moving between states and keeping Rex unpredictable

## Changes since Chapter 18

**New files**

- `MonsterMaze.Core/Monster/RexBrain.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Monster/RexTuning.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter19.slnx` in Rider or Visual Studio, or build from the command line:

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
