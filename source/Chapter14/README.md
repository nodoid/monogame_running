# Chapter 14: Touch Controls

*Part III — Into the Third Dimension*

## What this chapter covers

- Reading input with TouchPanel and gestures
- Swipe controls or on-screen buttons
- Designing controls that work one-handed, in the dark and under pressure
- Haptic feedback on Android and iOS
- An input layer for touch, plus keyboard and gamepad for testing

## Changes since Chapter 13

**New files**

- `MonsterMaze.Core/Input/TouchControls.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Android/AndroidManifest.xml`
- `MonsterMaze.Android/AndroidPlatformServices.cs`
- `MonsterMaze.Core/Data/Settings.cs`
- `MonsterMaze.Core/Input/InputManager.cs`
- `MonsterMaze.Core/Platform/IPlatformServices.cs`
- `MonsterMaze.Core/Platform/NullPlatformServices.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.iOS/IosPlatformServices.cs`


## Building and running

Open `Chapter14.slnx` in Rider or Visual Studio, or build from the command line:

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
