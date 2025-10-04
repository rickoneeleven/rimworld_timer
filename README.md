# RimWorld Timer (RimWorld 1.6)

A lightweight productivity timer for RimWorld. Shows a countdown in the top‑right, pauses with the game, auto‑pauses on alarm, flashes and beeps until you click Reset. Unpausing (or Reset while paused) starts the next cycle.

## Features
- Real‑time countdown that halts when the game is paused
- Auto‑pause the game when time reaches 0
- Flashing panel + vanilla beep on alarm
- Reset button to stop flashing, reset timer, and resume
- Position offsets (X/Y) to avoid overlapping resource readout
- Panel width/height configurable
- Interval in seconds (easy 1–5 second testing)

## Screenshots
![Timer overlay example 1](https://notes.pinescore.com/assets/note_68494425571805.86159046/image.png)

![Timer overlay example 2](https://notes.pinescore.com/assets/note_68494425571805.86159046/image(1).png)

## Known‑Good Paths (this Mac)
- Game app: `/Users/rick111/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app`
- Managed: `/Users/rick111/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Contents/Resources/Data/Managed`
- Harmony mod (Steam Workshop id 2009463077):
  - `/Users/rick111/Library/Application Support/Steam/steamapps/workshop/content/294100/2009463077/Current/Assemblies/0Harmony.dll`

If your install paths change, update the `<HintPath>` entries in `Source/RimWorldTimer/RimWorldTimer.csproj`.

## Build Requirements (macOS)
- .NET SDK 8 or 9 via Homebrew
  - `brew install --cask dotnet-sdk`
  - Verify: `dotnet --version`
- Mono reference assemblies for .NET Framework 4.7.2
  - `brew install mono`
  - The project uses `FrameworkPathOverride` → `/opt/homebrew/opt/mono/lib/mono/4.7.2-api`
- IDE: Rider recommended (VS Code also works)

No special Rider toolset configuration is needed because the project builds with the .NET SDK MSBuild while pointing to Mono’s 4.7.2 reference assemblies.

## Project Layout
- `About/About.xml` — Mod metadata and Harmony dependency
- `Source/RimWorldTimer/RimWorldTimer.csproj` — SDK‑style project targeting `net472`
- `Source/RimWorldTimer/*.cs` — Timer component, window overlay, Harmony bootstrap
- `Assemblies/RimWorldTimer.dll` — Build output consumed by RimWorld

## Build
- CLI: `dotnet build -c Release Source/RimWorldTimer/RimWorldTimer.csproj`
- Rider: Open `Source/RimWorldTimer/RimWorldTimer.csproj` and Build → Build Solution
- Output: `Assemblies/RimWorldTimer.dll`

## Install for Testing
Option A — Symlink into the app’s Mods folder (used here):

```
ln -s \
  /Users/rick111/repos/rim_world_timer \
  "/Users/rick111/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/RimWorld Timer"
```

Option B — Copy the repo folder into your user Mods folder (if present):

- `~/Library/Application Support/RimWorld/Mods/`

Then in RimWorld:
- Enable “Harmony” (brrainz.harmony) and ensure it is above this mod (Sort by dependency helps)
- Enable “RimWorld Timer” and restart if prompted

## Settings (in‑game → Options → Mod Settings → Timer)
- Interval (seconds): any integer (1–86400)
- Position offsets from top‑right: X (right→left), Y (top→down)
- Panel size (pixels): Width, Height
- Flash when alarmed (on/off)
- Beep when alarmed (on/off)

## Behavior
- While unpaused, timer counts down in real time
- On reaching 0:
  - Plays a beep (vanilla SoundDefOf.Click)
  - Panel flashes
  - Game auto‑pauses
- Clicking Reset:
  - Stops flashing, resets the timer to the configured seconds
  - Unpauses to normal speed so the next cycle starts immediately

## Troubleshooting
- If the overlay doesn’t appear:
  - Ensure Harmony is enabled and above this mod
  - Dev Mode → open log and look for messages like:
    - `[RimWorldTimer] TimerWindow drawing`
  - Check About/About.xml contains the Harmony dependency and URLs
- If build fails, confirm:
  - .NET SDK installed (`dotnet --version`)
  - Mono installed and `Source/RimWorldTimer/RimWorldTimer.csproj` has `FrameworkPathOverride` pointing to `/opt/homebrew/opt/mono/lib/mono/4.7.2-api`
  - RimWorld reference `<HintPath>` values match your install

## Git Remote
- Remote: `git@github.com:rickoneeleven/rimworld_timer.git`
- Common commands:
  - `git add -A && git commit -m "Update" && git push`

## Notes
- Supported RimWorld version: 1.6 (About.xml)
- Harmony dependency (Steam Workshop 2009463077) declared in About.xml with workshop and download URLs
- Sound uses vanilla click; you can later add a custom `SoundDef` if desired

## Future Ideas
- Snooze button (e.g., +2 minutes)
- Global hotkey to Reset
- Optional countdown color themes
