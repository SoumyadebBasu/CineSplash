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
- ⏱️ Configurable splash duration — globally, per-platform, or per-game
- ⏳ Option to wait until the game has actually started before the timer begins
- 🔄 Optional splash screen when returning to Playnite after closing a game
- 🚫 Disable splash in Desktop mode, Fullscreen mode, or both
- 📋 Exclude specific games from the splash
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

## Settings Overview

| Setting | Description |
|---|---|
| Default Duration | How long the splash stays on screen (seconds) |
| Logo Size | Width of the logo overlay in pixels |
| Skip Splash (Keyboard) | Custom keyboard key/combo to interrupt video playback |
| Skip Splash (Controller) | Controller button or 2-button combo (e.g. Start + Back) to interrupt video playback |
| Wait for game to start | Starts the timer only after the game process is detected |
| Show splash on game close | Shows the splash when you return to Playnite after closing a game |
| Disable in Fullscreen/Desktop | Suppress the splash in a specific Playnite mode |
| Enable Video Splash | Use a video file as the splash background |
| Video Source | Which video file to prefer (trailer vs. microtrailer) |
| Mute Video Audio | Silence the video during the splash |
| Platform-specific durations | Set different durations per platform |
| Game-specific durations | Override duration for individual games by database ID |
| Excluded Game IDs | Skip the splash entirely for specific games |

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
