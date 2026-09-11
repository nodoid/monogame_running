# Chapter 4: The Game Loop and Game States

*Part I — Foundations*

## What this chapter covers

- Inside the Game class: Initialize, LoadContent, Update and Draw
- GameTime, fixed time steps and frame-rate independence
- Building a screen manager: title, menu, gameplay, pause, game over and high scores
- Passing data between screens: difficulty, score and how the game ended
- Handling the Android back button and back gesture

## Changes since Chapter 3

**New files**

- `MonsterMaze.Core/Content/UI/button.png`
- `MonsterMaze.Core/Content/UI/icons.png`
- `MonsterMaze.Core/Content/UI/menu_bg.png`
- `MonsterMaze.Core/Content/UI/panel.png`
- `MonsterMaze.Core/Gameplay/GameSession.cs`
- `MonsterMaze.Core/Input/InputManager.cs`
- `MonsterMaze.Core/Screens/GameOverScreen.cs`
- `MonsterMaze.Core/Screens/GameScreen.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.Core/Screens/HighScoreScreen.cs`
- `MonsterMaze.Core/Screens/MainMenuScreen.cs`
- `MonsterMaze.Core/Screens/PauseScreen.cs`
- `MonsterMaze.Core/Screens/ScoreFlow.cs`
- `MonsterMaze.Core/Screens/ScreenManager.cs`
- `MonsterMaze.Core/Screens/TitleScreen.cs`
- `MonsterMaze.Core/UI/Button.cs`
- `MonsterMaze.Core/UI/Draw2D.cs`
- `MonsterMaze.Core/UI/Icons.cs`
- `MonsterMaze.Core/UI/NineSlice.cs`
- `MonsterMaze.Core/UI/Palette.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Android/AndroidPlatformServices.cs`
- `MonsterMaze.Android/MainActivity.cs`
- `MonsterMaze.Core/Content/MonsterMaze.mgcb`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Platform/IPlatformServices.cs`
- `MonsterMaze.Core/Platform/NullPlatformServices.cs`
- `MonsterMaze.iOS/IosPlatformServices.cs`


## Building and running

Open `Chapter04.slnx` in Rider or Visual Studio, or build from the command line:

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
