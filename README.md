# CineSplash 🎬

<p align="center">
  <img src="https://i.ibb.co/wFq7QQ2N/Cine-Splash.png" alt="CineSplash Logo"/>
</p>

<p align="center">
  A Playnite plugin that turns your game launch into a cinematic moment.
</p>

---

## What is CineSplash?

CineSplash displays a fullscreen splash screen when you launch a game — showing the game's background art, logo overlay, and optionally a **video trailer** from [Extra Metadata Loader](https://github.com/darklinkpower/PlayniteExtensionsCollection) while the game loads. Think of it as a personal cinema intro, every time you play.

This is a fork of [VibeSplash by EvoShot](https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash), which itself is a fork of [Artzox's Playnite Splash Addon](https://github.com/artzox/Playnite-Splash-Addon), extended with video playback and additional features.

---

## Features

- 🎬 **Video splash** — plays `VideoTrailer.mp4` or `VideoMicrotrailer.mp4` from Extra Metadata Loader as a fullscreen cinematic splash
- ⏹️ **Skip splash playback** — Press a custom keyboard hotkey (with modifier support) or gamepad button combination (e.g., `Start + Back`) to interrupt playback at any time
- 🖼️ Falls back to static background art if no video is found
- 🏷️ Logo overlay from Extra Metadata Loader displayed on top
- 🎯 Choose your preferred video source — trailer, microtrailer, or either with fallback
- 🔇 Optional audio mute during video playback
- ⚙️ **Two-Mode Architecture** — Choose between **Smart Window Detection** (recommended) or **Fixed Timers** for closing the splash screen
- 🧠 **Smart Window Detection** — automatically detects when the game window appears and closes the splash screen seamlessly
- ⏳ **Minimum & Safety Net Durations** — set a minimum display duration so fast-loading games don't flash off instantly, and a max timeout safety net
- 🎮 **Calibration System** — automatically calibrates window titles on first launch, with manual recalibration overrides via a dropdown window picker
- 🖱️ **Context Menu Integration** — right-click any game to force a manual recalibration or quickly toggle the splash screen on/off just for that game
- ⏱️ **Fixed Timer Mode** — traditional timer-based splash screen closing with support for platform-specific and game-specific overrides
- 🚫 **Disabled Games DataGrid** — easily view and re-enable games that have splash screens disabled
- 🔄 Optional splash screen when returning to Playnite after closing a game
- 🚫 Suppress splash in Desktop mode, Fullscreen mode, or both
- 💻 Supports both **portable** and **installed** Playnite setups

---

## Requirements

- [Playnite](https://playnite.link/) 9 or newer
- [Extra Metadata Loader](https://github.com/darklinkpower/PlayniteExtensionsCollection) *(optional — required for logo and video features)*

---

## Installation

### Via .pext (recommended)
1. Download the latest `.pext` from the [Releases](../../releases) page
2. Double-click the `.pext` file — Playnite will prompt you to install it

### Manual
1. Download and extract the release zip
2. Copy the plugin folder into:
   - **Portable:** `{PlayniteFolder}\Extensions\`
   - **Installed:** `%AppData%\Playnite\Extensions\`
3. Restart Playnite

---

## Video Splash Setup

1. Install [Extra Metadata Loader](https://github.com/darklinkpower/PlayniteExtensionsCollection) and download trailers for your games
2. In Playnite go to **Add-ons → CineSplash → Settings**
3. Enable **"Play video trailer as splash background"**
4. Choose your preferred video source:
   - **Trailer → fallback: Microtrailer** *(default)*
   - **Microtrailer → fallback: Trailer**
   - **Trailer only**
   - **Microtrailer only**

If no video is found for a game, CineSplash falls back to the static background image automatically.

---

---

## Modes & Game Window Detection

CineSplash supports two primary modes for closing the splash screen when launching a game:

### 1. Smart Window Detection Mode (Recommended)
Actively monitors background processes to detect when your game's window actually appears, closing the splash screen at the exact right moment.
- **Auto-Calibration:** On first launch, CineSplash automatically snapshots processes and records the game window title.
- **Manual Window Selection:** If auto-calibration captures the wrong window, right-click the game in your library and select **Extensions > CineSplash > Re-detect Game Window**. On next launch, an overlay prompt with a dropdown window picker allows explicit window selection.
- **Minimum Splash Duration:** Guarantees the splash stays visible for a minimum number of seconds (default: 3s) so fast-loading games don't flash on/off too quickly.
- **Max Splash Duration:** Safety net timeout (default: 120s) to close the splash screen if a game window is never detected.

### 2. Fixed Timer Mode
Traditional timer-based approach. Closes after a configured duration (global default, platform-specific, or game-specific).

### Right-Click Context Menu & Managing Disabled Games
- **Toggle Splash Screen:** Right-click any game in Playnite and select **Extensions > CineSplash > Toggle Splash Screen (Enable/Disable)** to bypass the splash for that game.
- **Disabled Games DataGrid:** Open **CineSplash Settings** to view a table of all disabled games and click **Enable** to turn the splash screen back on.

---

## Settings Overview

| Setting | Mode / Section | Description |
|---|---|---|
| Splash Screen Close Mode | Master Toggle | Switch between Smart Window Detection and Fixed Timer mode |
| Minimum Splash Duration | Window Detection | Minimum seconds to show splash even if game loads instantly |
| Max Splash Duration | Window Detection | Safety net timeout if window detection fails |
| Show Elapsed Time | Window Detection | Live launch timer overlay on splash screen |
| Default Duration | Fixed Timers | Default splash duration in seconds |
| Wait for game to start | Fixed Timers | Starts the timer only after the game process is detected |
| Platform-specific timers | Fixed Timers | Set different durations per platform |
| Game-specific durations | Fixed Timers | Override duration for individual games by database ID |
| Logo Size | Global | Width of logo overlay in pixels |
| Skip Splash (Keyboard) | Global | Custom keyboard key/combo to interrupt video playback |
| Skip Splash (Controller) | Global | Controller button or 2-button combo (e.g. Start + Back) to interrupt video playback |
| Show splash on game close | Global | Shows splash when returning to Playnite after closing a game |
| Disable in Fullscreen/Desktop | Global | Suppress splash in specific Playnite modes |
| Enable Video Splash | Global | Use video trailer/microtrailer as splash background |
| Disabled Games DataGrid | Global | Table of games with splash disabled, with one-click Enable button |

---

## Building from Source

Requirements: **Visual Studio 2022** with .NET desktop development workload.

1. Clone the repo
2. Open `CineSplash.csproj` and update the `<HintPath>` values for `Playnite.SDK.dll` and `Newtonsoft.Json.dll` to point to your Playnite folder
3. Run the build and pack script:
   ```powershell
   .\pack.ps1
   ```
   This will build the project, copy assets, and produce a ready-to-install `.pext` in the `packed\` folder.

---

## ⚠️ Disclaimer

> **This plugin is vibe coded.**
>
> The additional features in this fork were developed with AI assistance (Claude by Anthropic). The code works, but it has not been extensively tested across all Playnite configurations, game libraries, or edge cases. Use it at your own risk.
>
> - Always back up your Playnite data before installing third-party plugins
> - If something breaks, check `{PlayniteFolder}\playnite.log` for errors
> - Issues and PRs are welcome, but support is best-effort

---

## Credits

- Original addon by [Artzox](https://github.com/artzox/Playnite-Splash-Addon)
- VibeSplash fork by [EvoShot](https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash)
- CineSplash fork by [Raoul](https://github.com/YOUR_USERNAME)
