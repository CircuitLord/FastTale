# FastTale

A performance mod for A Township Tale, uses MelonLoader. Disables/optimizes different stuff in the game to improve performance.

Disclaimer: This repo does not contain any game files or assets, this is written by me and you must have the base game to run it.

## Features

Press F2 for the settings panel. All settings persist via MelonPreferences.

- **MSAA toggle**: starts with MSAA off (game default 2x MSAA). Saves 1-3ms on the GPU depending on resolution.
- **Overview Camera toggle**: some random camera in the scene, unclear what it does, but it was wasting perf, saves 27% on the CPU
- **Grass toggle**: toggle off/on the small grass meshes. doesn't seem to provide any perf benefit but the option is there.
- **Bloom toggle**: toggles Bloom post processing effect on every volume. On by default.

## Install

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader) onto A Township Tale.
2. Build `FastTale.dll` (see below) or grab it from
   [Releases](../../releases).
3. Put `FastTale.dll` into the game's `Mods/` folder.
4. yippee

## Build

Requires the .NET SDK. Edit `GamePath` in `FastTale.csproj` if your install lives
somewhere other than `C:\Games\Alta\A Township Tale`.

```sh
dotnet build -c Release
```

The build copies `FastTale.dll` straight into the game's `Mods/` folder.

## License

[MIT](LICENSE)
