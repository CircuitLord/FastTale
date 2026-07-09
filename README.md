# FastTale

A performance mod for A Township Tale, uses MelonLoader.

Disables/optimizes different stuff in the game to improve performance.

## Features

- **MSAA toggle (F2)** — starts with MSAA off. Press F2 to toggle it back on (game default 2x MSAA). Saves 1-3ms on the GPU depending on resolution.
- More soon???

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
