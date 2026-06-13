# 🎮 FiveTogether — Controller Manager

**Manage 5 controllers on a 4-slot Windows system without disconnecting anyone from Parsec.**

## The Problem

Windows XInput only supports 4 controller slots. When 5 friends play Cricket 19 via Parsec, the 5th controller gets no slot. Currently, everyone must disconnect and rejoin Parsec to reset slot assignments.

## The Solution

FiveTogether hides all physical controllers from the game, creates 4 clean virtual Xbox 360 controllers via ViGEmBus, and forwards input from your chosen 4 devices. **Swap the 5th controller in/out with one click — no Parsec disconnect needed.**

## Requirements

- Windows 10/11 (host PC only)
- [ViGEmBus driver](https://github.com/nefarius/ViGEmBus/releases)
- [HidHide driver](https://github.com/nefarius/HidHide/releases)

## Download

Go to [Actions → latest build](../../actions) → click the build → download **FiveTogether-win-x64** artifact.

## Build from Source

```powershell
# Requires .NET 8 SDK
dotnet restore
dotnet build
dotnet run --project src/FiveTogether
```

## How to Use

1. Connect all 5 controllers via Parsec
2. Open FiveTogether → click **Refresh** → verify all 5 detected
3. Click **Start Session** → Cricket 19 sees 4 clean virtual controllers
4. Click **Swap** on any slot to swap in a different controller
5. Click **Stop Session** when done — all controllers restored to normal

## License

MIT
