# Community Trainer for Tekken 6

An independent Windows trainer for **Tekken 6 USA, BLUS30359, update 1.03, running in RPCS3**. New C# implementation and interface, with portable builds and documented memory behavior.

**Gold Azazel confirmed working in local testing:** the maintainer reports that Gold Azazel works with **BLUS30359 / update 1.03 on RPCS3 0.0.43-20570**. Process selection, attachment and live guest-memory reads have also been verified. See the [test results and scope](docs/VALIDATION.md). Other regions, updates and arcade editions are unsupported.

## Download and use

Download the Windows x64 ZIP from [Releases](https://github.com/danstronics/tekken6-trainer-community/releases), extract it, and run `Tekken6.Community.exe`. The portable ZIP includes .NET; no separate runtime installation is required. Keep the accompanying licenses with redistributed builds.

1. Start your own game in RPCS3.
2. Select the emulator's process and press **Connect**. The list identifies instances by PID.
3. Leave the default PPU base `300000000` unless your emulator uses a different mapping.
4. Choose a boss before loading the fight, or enable an option in the Battle, Campaign or General tabs.
5. Use **Stop all options** or **Disconnect** to stop forcing values. Persistent configuration fields are restored when the same game object is still present and the field still contains the trainer's last value. Health, time, money, character and stage changes are not rewound.

If RPCS3 runs elevated, Windows requires matching privileges to access it. The trainer itself does not request elevation by default. The program neither downloads nor includes a game, emulator, update, firmware or console plugin.

## Features

- P1/P2 Azazel and Nancy selection; Gold Azazel with one shared skin flag, confirmed working by the maintainer on the tested layout.
- Battle health, invulnerability, blocking, clock, elapsed time, roulette and one-hit options.
- Campaign team health, clock, rage, item drops, elemental resistance, attack effects and bonus damage.
- Fight money, player-control routing, model visibility and movement speed.
- Stage selection by numeric ID from the 89 recovered stage identifiers.
- Explicit memory error status, checked big-endian pointers, and no patches enabled at startup.

P2 boss replacement is supported only by the battle layout. In campaign, only the P1 replacement slot is known. Gold may affect both Azazel instances because the skin flag is global. A zero/one field check reduces accidental writes but does not establish that an unknown build uses the same layout.

## Credits and origins

- **[Hyper Cheats / @hypercheats1113](https://www.youtube.com/@hypercheats1113)** — the original RPCS3 trainer series and its compatibility information. Public references: [v1.1 video, 2020](https://www.youtube.com/watch?v=gzrDEujmVBQ), [v1.2 video, 2023](https://www.youtube.com/watch?v=e4jwcsg0zns).
- **[Ryu_Hayabusa80](https://nextgenupdate.com/forums/accounts-services/929589-tekken-hack-services-tekken-rtm-tools.html)** — the separately authored PS3 RTM tools and Gold Azazel research reference. His public announcement dates to 2016.
- **[@danstronics](https://github.com/danstronics)** — this independent community implementation, testing and packaging.
- **[RPCS3 contributors](https://github.com/RPCS3/rpcs3)** — the emulator this tool interoperates with.
- **[.NET contributors](https://github.com/dotnet)** — the runtime and Windows Forms framework used by the portable build.

No verified GitHub accounts were identified for Hyper Cheats or Ryu_Hayabusa80. Their original profile/publication links are used instead of tagging unrelated GitHub users. These references are acknowledgments, not claims that those authors contributed to or endorsed this implementation.

## Source and licensing

The newly written implementation is licensed under [MIT](LICENSE). This repository does **not** redistribute the original trainers, decompiled source, extracted resources, images, icons, sounds, SharpDX binaries, DevExpress components or PS3 packages. Only recovered interoperability facts (addresses, field widths and numeric values) were used to design the new implementation. This was not a formal clean-room process: the prior binaries were inspected during research.

Tekken, character names and related marks belong to their respective owners, including Bandai Namco. Names are used to identify interoperability; no affiliation or endorsement is implied. Third-party runtime copyright/license notices are preserved rather than removed. See [provenance and exclusions](docs/PROVENANCE.md) and [runtime notices](THIRD_PARTY_NOTICES.md).

## Build

On Windows x64, install .NET SDK **10.0.403** (or a compatible patch as specified by `global.json`) and run:

```powershell
./build.ps1
```

The script runs the automated tests, publishes a compressed self-contained EXE, copies runtime license notices, and creates a ZIP plus SHA-256 checksums in `artifacts/`.

To run only the tests:

```powershell
dotnet run --project tests/Trainer.Tests/Trainer.Tests.csproj -c Release
```

GitHub Actions builds and tests pushes and pull requests. A `v*` tag also publishes a prerelease with the portable ZIP and checksums. Builds remain prereleases while feature and version coverage expands.
