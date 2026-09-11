# Chapter 29: Render Targets and Custom Effects

*Part VII — Visual Effects*

## What this chapter covers

- Rendering to a RenderTarget2D for post-processing
- Writing and compiling custom effects (shaders) for mobile
- Compiling shaders on a Mac: the effect compiler needs Windows (or Wine), so we run it in a Parallels Windows virtual machine
- Automating the shader build with a script and shipping the precompiled effects with each platform project
- Anti-aliasing and render targets: what OpenGL ES on phones can and cannot do
- Performance considerations on phones and tablets

## Changes since Chapter 28

**New files**

- `MonsterMaze.Core/Content/Effects/Compiled/Android/PostProcess.xnb`
- `MonsterMaze.Core/Content/Effects/Compiled/iOS/PostProcess.xnb`
- `MonsterMaze.Core/Content/Effects/PostProcess.fx`
- `MonsterMaze.Core/Effects/PostProcessor.cs`

**Changed files**

- `Directory.Build.props`
- `MonsterMaze.Android/MonsterMaze.Android.csproj`
- `MonsterMaze.Core/GameAssets.cs`
- `MonsterMaze.Core/MonsterMazeGame.cs`
- `MonsterMaze.Core/Screens/GameplayScreen.cs`
- `MonsterMaze.iOS/MonsterMaze.iOS.csproj`


## Building and running

Open `Chapter29.slnx` in Rider or Visual Studio, or build from the command line:

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
