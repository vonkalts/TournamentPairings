# Tournament Pairings

A Windows desktop app for running 2-round wargame tournaments (Axis vs Allies): player setup, draws, results, standings and Excel export.

Made by Thanasis Kaltsoudas.

## Download

Get the latest `TournamentPairings-vX.Y.zip` from the [Releases](../../releases) page, unzip the whole folder and run `TournamentPairings.exe`. No installation and no .NET install needed (Windows 10/11, 64-bit).

## Features

- **Round 1**: random pairing, Axis vs Allies where possible, random tables.
- **Round 2 (Swiss style)**: players ranked by Score, then Units, then a random draw. Ideal pairing 1st-3rd, 2nd-4th, 5th-7th, 6th-8th, ... The pairing changes only when needed so that:
  1. Special players do not play each other (unless unavoidable),
  2. nobody meets their Round 1 opponent again (unless unavoidable),
  3. the 1st never plays the 2nd (unless unavoidable).
  
  Optional: prefer Axis vs Allies, and avoid playing on the same table as in Round 1 (on by default).
- **BYE**: never given to Special players; not given twice to the same player when possible.
- **Late Entry** players (join from Round 2) and **withdrawals** before Round 2.
- **Standings**: total Score, then total Units, then a random draw.
- Save/open `.tournament` files, automatic autosave snapshots, Excel export.
- The window scales with its size; zoom with Ctrl + / Ctrl - / Ctrl 0 or Ctrl + mouse wheel.
- Built-in user guide (**Help** button).

## Build from source

Requires the .NET 8 SDK (or newer).

```bash
dotnet build
```

Self-contained single-file build (output folder set in `Properties/PublishProfiles/FolderProfile.pubxml`):

```bash
dotnet publish -p:PublishProfile=FolderProfile
```
