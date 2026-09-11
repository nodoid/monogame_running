# Chapter 42: Publishing

*Part IX — Polish and Release*

## What this chapter covers

- App icons, splash screens and store artwork
- Signing and releasing on Google Play
- Signing and submitting to the App Store
- What next? Ideas for extending the game

## Changes since Chapter 41

**New files**

- `Store/AppStore/icon_1024.png`
- `Store/GooglePlay/feature_graphic.png`
- `Store/GooglePlay/icon_512.png`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Android/MonsterMaze.Android.csproj`
- `MonsterMaze.iOS/Info.plist`
- `MonsterMaze.iOS/MonsterMaze.iOS.csproj`


## Building and running

Open `Chapter42.slnx` in Rider or Visual Studio, or build from the command line:

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

To run the unit tests:

```
dotnet test --project MonsterMaze.Tests
```
