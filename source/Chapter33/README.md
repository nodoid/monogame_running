# Chapter 33: Game Over State One: Eaten!

*Part VIII — Game Over*

## What this chapter covers

- The final lunge: taking control away from the player
- Rex’s jaws, the screen flash and the roar
- The “You have been eaten” screen and the final score
- Moving on to the high score table

## Changes since Chapter 32

**New files**

- `MonsterMaze.Core/Content/Sprites/jaws_bottom.png`
- `MonsterMaze.Core/Content/Sprites/jaws_top.png`
- `MonsterMaze.Core/Content/UI/eaten_bg.png`
- `MonsterMaze.Core/Gameplay/EatenSequence.cs`
- `MonsterMaze.Core/Screens/EatenScreen.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`


## Building and running

Open `Chapter33.slnx` in Rider or Visual Studio, or build from the command line:

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
