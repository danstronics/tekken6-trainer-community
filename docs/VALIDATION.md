# Validation results

Updated: **2026-11-07**.

## Tested setup

- Tekken 6 USA, **BLUS30359**, JB folder, official update **01.03** installed and confirmed from its `PARAM.SFO`.
- RPCS3 **0.0.43-20570-13a3cd42 Alpha**, Windows x64.
- Vulkan on NVIDIA GeForce RTX 3060 Laptop GPU, 1280x720, 60 fps frame limit.

## Results

| Check | Result | Evidence |
| --- | --- | --- |
| Game startup | Working | Intro observed running; RPCS3 displayed approximately 60 fps |
| Live guest memory | Working | The known root pointer, game state and mode were readable in the running game |
| Process selection and attach | Working | Five read-only checks against the actual RPCS3 process passed after the local v2.2.1 fix |
| Gold Azazel | **Confirmed working by the maintainer** | Explicit user confirmation during the local test session on 2026-11-07 |
| Community edition regression suite | 69 checks passed | Simulated game-state tests, per-option patch checks and native Windows memory-adapter tests; GitHub Actions builds succeeded |
| Complete per-feature gameplay sweep | Not completed | The successful Gold test does not establish that every other option has been tested in gameplay |

The Gold confirmation is a maintainer-reported visual result, not an independently captured visual test by the assistant. The report comes from the local session following the legacy **v2.2.1** PID fix. The public **v3.0.0** edition is a separately written implementation using the same documented Gold flag layout. The report validates that layout on this setup; it does not certify every feature or every public build.

## PID selection regression

The local legacy selector displayed process IDs as hexadecimal strings. For example, `00004DE0` is decimal PID `19936`. Treating that display string as decimal caused a `FormatException`.

The local v2.2.1 fix retains the numeric PID on its process-list row and handles legacy text as hexadecimal, including digit-only strings such as `00001234`. Its 35 regression checks and five live read-only checks passed.

The independent community edition already stores PIDs as integers and passes the selected integer directly to its memory adapter. It does not use the legacy selector or that display-string conversion.

No game, update package, firmware, screenshot from the game, legacy trainer binary or decompiled source is included with this report.
