# Chapter 1: Welcome to the Maze

*Part I — Foundations*

## What this chapter covers

- The story of 3D Monster Maze and why it still frightens players
- What we are building: a modern, full-colour, high-resolution MonoGame remake for Android and iOS
- The feature list: the 3D maze, Rex, three difficulty levels, sound, visual effects, the high score table and the two endings
- How to use this book and the accompanying source code


## Building and running

Open `Chapter01.slnx` in Rider or Visual Studio, or build from the command line:

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
