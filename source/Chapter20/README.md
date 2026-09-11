# Chapter 20: The Warnings

*Part IV — Rex, the Monster in the Maze*

## What this chapter covers

- Proximity messages in the spirit of the original: “Rex lies in wait”, “Footsteps approaching”, “Run, he is behind you”
- Choosing which message to show and when
- Animating and colouring the messages as the danger rises

## Changes since Chapter 19

**New files**

- `MonsterMaze.Core/Gameplay/WarningSystem.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter20.slnx` in Rider or Visual Studio, or build from the command line:

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
