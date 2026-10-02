# Game Audio Switcher for Playnite

A small Playnite Generic Plugin that changes the Windows HDMI speaker/spatial-audio mode **before a game launches**.

It is designed for a PC connected to an AVR/home-cinema system where different games need different Windows audio modes:

- **Stereo** → lets the AVR see genuine 2.0 PCM and use Dolby Surround/Auro-Matic.
- **5.1 PCM** → recommended default for a 5.x.x bed layer; AVR can add heights with an upmixer.
- **7.1 PCM** → available for systems/games where you deliberately want a 7.1 Windows endpoint.
- **Dolby Atmos** → enables `Dolby Atmos for home theater` for native Atmos games.

Per-game assignments are stored by Playnite game ID, so launcher-based games such as RuneScape via the Jagex Launcher work normally: the audio mode is changed before Playnite invokes the launcher.

## Dependency

The plugin intentionally does **not** bundle SoundVolumeView. Download SoundVolumeView from NirSoft and place it somewhere stable, for example:

`C:\Tools\SoundVolumeView\SoundVolumeView.exe`

The plugin uses SoundVolumeView's documented `/SetSpeakersConfig`, `/SetSpatial`, and `/GetColumnValue` commands.

## Build

Requirements:

- Windows
- Visual Studio 2022 with **.NET desktop development** and .NET Framework 4.6.2 targeting support, or a compatible .NET SDK setup
- NuGet access for the `PlayniteSDK` package

Open `GameAudioSwitcher.sln` and build **Release**, or run:

```powershell
.\Build-And-Pack.ps1
```

The project targets `net462` and PlayniteSDK `6.17.0`.

If Playnite's `Toolbox.exe` is found, `Build-And-Pack.ps1` also runs:

```text
Toolbox.exe pack <built extension folder> <dist folder>
```

and produces a `.pext` package in `dist`.

## Development install

After building, either add the build output as an **External extension** under Playnite's developer settings, or run:

```powershell
.\Install-Dev.ps1
```

Then restart Playnite.

## First-time setup

1. Open **Playnite → Add-ons → Extension settings → Game Audio Switcher**.
2. Set the path to `SoundVolumeView.exe`.
3. Leave **Audio device** as `DefaultRenderDevice` unless you specifically need another Windows audio endpoint.
4. The plugin includes standard defaults:
   - Stereo: `0x3 0x3 0x3`
   - 5.1: `0x3f 0x3f 0x3f`
   - 7.1: `0x63f 0x63f 0x63f`
5. For maximum reliability, configure Windows manually to each desired speaker layout and click **Capture current** beside that layout. This stores exactly what your Windows/HDMI endpoint reports.
6. Use the **Test** buttons to verify Stereo, 5.1, 7.1 and Atmos before assigning games.

## Assign games

In Playnite Desktop Mode, right-click one or more games:

`Game Audio Switcher → Audio Mode → ...`

Choose:

- **Default**: use the global default mode.
- **Stereo (AVR upmix)**: good for RuneScape and genuinely stereo titles.
- **5.1 PCM**: recommended default for a 5.2.4 system when a game supports normal surround but not Atmos.
- **7.1 PCM**: optional for titles/systems where 7.1 is preferable.
- **Dolby Atmos**: use only for games with native Atmos/Windows Spatial Sound support.

The setting is attached to the Playnite game entry, not its executable. This means games launched through Steam, EA, Ubisoft, Jagex Launcher, etc. do not need special process detection.

## Recommended setup for a 5.2.4 Denon system

- **RuneScape / stereo games:** Stereo → AVR Dolby Surround or Auro-Matic
- **Non-Atmos surround games:** 5.1 PCM → AVR Dolby Surround/Auro-Matic if you want generated heights
- **Native Atmos games:** Dolby Atmos → AVR native Atmos

The plugin defaults unconfigured games to **5.1 PCM** because a 5.2.4 layout has five bed-layer speakers. You can change the global default to 7.1 if you prefer letting the AVR fold 7.1 input into the installed 5-channel bed layer.

## Restore behaviour

By default, when the last Playnite-tracked game closes, the plugin restores **Stereo**. This is useful if you want browsers/music/audiobooks to arrive at the AVR as genuine stereo for receiver-side upmixing.

You can disable restore-on-exit or change the desktop restore mode in settings.

## Notes

- The plugin does not cancel game launch if audio switching fails; it logs/notifies the error and allows the game to start.
- Playnite context-menu entries are available in Desktop Mode, but the saved assignment still applies when launching the game from Fullscreen Mode.
- If a game launcher is not tracked correctly by Playnite, the **pre-launch switch still works**. Incorrect tracking mainly affects when the restore-on-exit action runs.

## Upstream documentation

- Playnite Generic Plugins: https://api.playnite.link/docs/tutorials/extensions/genericPlugins.html
- Playnite Toolbox: https://api.playnite.link/docs/tutorials/toolbox.html
- SoundVolumeView command line: https://www.nirsoft.net/utils/sound_volume_view.html
