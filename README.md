# VibeSplash 🎬

A Playnite plugin that displays a **splash screen** when launching a game — showing the game's background art and logo overlay while the game loads.

This is a fork of [Artzox's Playnite Splash Addon](https://github.com/artzox/Playnite-Splash-Addon), further extended by [EvoShot](https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash), with additional features added in this fork.

---

## Features

- 🖼️ Displays game background art as a fullscreen splash when launching a game
- 🎬 **Video splash support** — plays `VideoTrailer.mp4` or `VideoMicrotrailer.mp4` from [Extra Metadata Loader](https://github.com/darklinkpower/PlayniteExtensionsCollection) as the splash background
- 🔇 Optional audio mute for video playback
- 🎯 Choose your preferred video source (trailer, microtrailer, or either with fallback)
- 🏷️ Logo overlay from Extra Metadata Loader displayed on top of the background/video
- ⏱️ Configurable splash duration — globally, per-platform, or per-game
- ⏳ Option to wait until the game has actually started before the timer begins
- 🔄 Optional splash screen when returning from a game (on game close)
- 🚫 Disable splash in Desktop mode, Fullscreen mode, or both
- 📋 Exclude specific games from showing the splash
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
2. In Playnite go to **Add-ons → VibeSplash → Settings**
3. Enable **"Play video trailer as splash background"**
4. Choose your preferred video source:
   - **Trailer → fallback: Microtrailer** *(default)*
   - **Microtrailer → fallback: Trailer**
   - **Trailer only**
   - **Microtrailer only**

If no video is found for a game, VibeSplash falls back to the static background image automatically.

---

## Settings Overview

| Setting | Description |
|---|---|
| Default Duration | How long the splash stays on screen (seconds) |
| Logo Size | Width of the logo overlay in pixels |
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
2. Open `VibeSplash.csproj` and update the `<HintPath>` values for `Playnite.SDK.dll` and `Newtonsoft.Json.dll` to point to your Playnite folder
3. Build with MSBuild:
   ```
   "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" VibeSplash.csproj /p:Configuration=Release
   ```
4. Output is in `bin\Release\`

---

## Credits

- Original addon by [Artzox](https://github.com/artzox/Playnite-Splash-Addon)
- VibeSplash fork by [EvoShot](https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash)
- Video splash & additional features by [Raoul](https://github.com/YOUR_USERNAME)
