# FastTale

A performance mod for A Township Tale, uses MelonLoader. Disables/optimizes different stuff in the game to improve performance.

Disclaimer: This repo does not contain any game files or assets, this is written by me and you must have the base game to run it.

## Features

Press F2 for the settings panel. All toggles persist via MelonPreferences.

- **MSAA toggle** — starts with MSAA off (game default 2x MSAA). Saves 1-3ms on the GPU depending on resolution.
- **Overview Camera toggle** — disables the flat desktop camera whose render is wasted in VR. Off by default.
- **Grass toggle** — hides the small grass tuft meshes. Clumps register themselves as chunks load (Harmony hook on `GrassClump.Start`), no scene scanning. On by default.
- **Bloom toggle** — disables the Bloom post processing effect on every volume. On by default.

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
