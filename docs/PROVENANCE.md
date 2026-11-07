# Provenance and excluded material

This public project contains a new implementation. Earlier private workspace experiments rebuilt decompiled trainer code; those experiments and binaries are **not** part of this repository or its release assets. The public version is deliberately numbered **3.0.0** to distinguish it from that work.

## Research references

| Reference | Attribution | Evidence |
| --- | --- | --- |
| RPCS3 trainer series | Hyper Cheats | [2020 v1.1 demonstration](https://www.youtube.com/watch?v=gzrDEujmVBQ), [2023 v1.2 demonstration](https://www.youtube.com/watch?v=e4jwcsg0zns); the provided v2.0 binary also identifies Hyper Cheats in its credits |
| PS3 RTM tool family | Ryu_Hayabusa80 | [Author's 2016 announcement](https://nextgenupdate.com/forums/accounts-services/929589-tekken-hack-services-tekken-rtm-tools.html); the provided archive's developer note names the same author |
| Emulator | RPCS3 contributors | [RPCS3 source repository](https://github.com/RPCS3/rpcs3) |

No official source repository or redistribution license for the provided trainer binaries was located. The references identify authorship and research provenance, not permission to republish those binaries or decompiled sources. No public release announcement for the exact provided v2.0 file was independently verified. No direct derivation relationship between the two authors' original projects was established.

## Gold Azazel relocation

The original PS3 RTM tool writes the 32-bit big-endian value `1` at a pointer obtained from guest global `0x00BEF030`, plus `0xA4`. Its player-character words are at offsets `0xF8` and `0xFC`, and its stage byte is at `0xF7`.

The provided RPCS3 trainer resolves its object through `*( *(0x00B88480) + 0xF14 )`. Its full character words are at `0x174` and `0x178`, and its stage byte is at `0x173`. Both field comparisons imply a displacement of `0x7C`. The experimental implementation therefore uses:

```text
state = read32be(read32be(0x00B88480) + 0xF14)
gold  = state + 0x120    # 0xA4 + 0x7C
```

This is a structural inference from the two inspected binaries. It has not been confirmed in a real match. The trainer rejects a Gold field whose current 32-bit value is neither zero nor one, but a matching value alone does not prove the address is correct. Gold is one shared flag, not a separate character ID or per-player costume byte.

## Public payload policy

Only the newly written source, tests, documentation, build scripts and this project's release outputs are published. The original EXEs, archives, extracted code, logos, artwork, music, sound effects, screenshots from the game, game files, firmware and console packages are excluded. No third-party accounts are tagged without a verified identity link.

Portable releases additionally contain the redistributable .NET runtime inside the executable. Its original license and third-party notices accompany the executable in `licenses/`. The project MIT license covers this implementation, not games, trademarks or referenced third-party projects.
