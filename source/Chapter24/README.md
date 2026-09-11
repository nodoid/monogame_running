# Chapter 24: Implementing and Balancing Difficulty

*Part V — Rules, Scoring and Difficulty*

## What this chapter covers

- The difficulty selection screen
- Applying the settings to the random maze generator, Rex’s AI, lighting and scoring
- Remembering the player’s last choice
- Testing and balancing each level

## Changes since Chapter 23

**New files**

- `MonsterMaze.Core/Screens/DifficultyScreen.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Screens/MainMenuScreen.cs`
- `MonsterMaze.Core/UI/FontSet.cs`


## Building and running

Open `Chapter24.slnx` in Rider or Visual Studio, or build from the command line:

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
