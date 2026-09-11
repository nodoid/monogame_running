# Chapter 37: Saving Scores and Settings

*Part VIII — Game Over*

## What this chapter covers

- Where to store data on Android and iOS
- Saving high scores and settings as JSON
- Surviving app updates, reinstalls and corrupted files

## Changes since Chapter 36

**New files**

- `MonsterMaze.Core/Data/SaveStore.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Data/HighScoreTable.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Screens/DifficultyScreen.cs`
- `MonsterMaze.Core/Screens/NameEntryScreen.cs`


## Building and running

Open `Chapter37.slnx` in Rider or Visual Studio, or build from the command line:

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
