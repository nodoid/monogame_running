# Monster Maze: source code

The source code for *Recreating 3D Monster Maze with MonoGame for Android and iOS*.

Each chapter has its own folder, `Chapter01` to `Chapter42`, with the game as it stands at the end
of that chapter. Every chapter folder is a complete solution you can open and build on its own
(`ChapterNN.slnx`). `MonsterMaze.slnx` in this folder opens all of them together.

Each chapter contains:

| Project | What it is |
|---|---|
| `MonsterMaze.Core` | The whole game, as a platform-neutral library, plus its content (`Content/MonsterMaze.mgcb`) |
| `MonsterMaze.Android` | The Android app (`net10.0-android`) |
| `MonsterMaze.iOS` | The iOS app (`net10.0-ios`) |
| `MonsterMaze.Tests` | Unit tests (from Chapter 41) |

Each chapter's `README.md` lists what the chapter covers and which files changed since the last one.

## Requirements

- .NET 10 SDK with the `android` and `ios` workloads (`dotnet workload install android ios`)
- Xcode 26 or later (for iOS)
- The Android SDK (installed with Android Studio, or by the .NET Android workload)
- MonoGame 3.8.5.1, which comes from NuGet automatically. The content builder (`dotnet-mgcb`) is
  restored from each chapter's `.config/dotnet-tools.json` on the first build.

## Building

```
cd Chapter42
dotnet build MonsterMaze.Android/MonsterMaze.Android.csproj
dotnet build MonsterMaze.iOS/MonsterMaze.iOS.csproj -r iossimulator-arm64
```

## The game

- Gameplay is locked to landscape; every other screen is locked to portrait.
- Every game has a brand-new, randomly generated maze. Easy, Normal and Hard change its size,
  its loops, the darkness and how dangerous Rex is.
- The high score table keeps the top six scores (with names) for each difficulty. It is saved as
  JSON in the app's private storage, so it survives restarts and app updates.

## Tools

| Folder | What it does |
|---|---|
| `Tools/Template` | The finished game, with every chapter's changes marked by `#if CHnn` symbols |
| `Tools/ChapterBuilder` | `build_chapters.py` generates the chapter folders and the master solution from the template |
| `Tools/AssetGenerator` | `make_graphics.py` and `make_audio.py` create every image and sound in the game |
| `Tools/ShaderCompiler` | `compile_shaders.sh` compiles the `.fx` shaders in a Parallels Windows VM |

The chapter folders are generated. To change the game, edit `Tools/Template` and then run:

```
python3 Tools/ChapterBuilder/build_chapters.py
```

MonoGame's shader compiler needs Windows (or Wine), so the compiled shaders are checked in under
`Content/Effects/Compiled`. You only need `compile_shaders.sh` if you change a `.fx` file.
