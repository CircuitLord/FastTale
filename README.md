# FastTale

A performance mod that increases CPU and GPU performance of the game without any (major) visual downgrades.

Press F2 to open the configuration menu.

Disclaimer: This repo does not contain any game files or assets, this is written by me and you must have the base game to run it.

## Features
- MSAA toggle (game default is 2x, not needed at all if using a higher base res). Saves 1-3ms on the GPU depending on resolution.
- Disables Overview Camera (mystery unused camera wasting performance in the background), improves CPU performance by ~27%
- More soon

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
