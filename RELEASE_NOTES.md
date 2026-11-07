Independent community edition for Tekken 6 USA (BLUS30359), update 1.03 on RPCS3.

- Newly written C# memory engine and Windows Forms interface; no original trainer code or extracted assets.
- Battle, campaign and general options, P1/P2 boss selection and 89 stage IDs.
- Gold Azazel support with a separate shared 32-bit flag and restoration.
- Portable Windows x64 build, original MIT source, automated regression checks and SHA-256 checksums.

**Verification update — 2026-11-07:** the maintainer confirms that **Gold Azazel works** in the local Tekken 6 USA **BLUS30359 / update 1.03** test session, using **RPCS3 0.0.43-20570-13a3cd42 Alpha** with Vulkan. Live process selection, attachment and guest-memory reads were also verified. This confirms the Gold flag layout on the tested setup.

The public community edition also passes 69 automated checks covering simulated game memory and the native Windows memory adapter. The live report followed the local legacy v2.2.1 PID-selection fix; it is not a claim that every feature of the separately rewritten v3.0.0 binary was visually tested. Other regions, versions and arcade editions remain unsupported. See [validation details](https://github.com/danstronics/tekken6-trainer-community/blob/main/docs/VALIDATION.md).

Credits and research references: [Hyper Cheats / @hypercheats1113](https://www.youtube.com/@hypercheats1113), [Ryu_Hayabusa80](https://nextgenupdate.com/forums/accounts-services/929589-tekken-hack-services-tekken-rtm-tools.html), [RPCS3 contributors](https://github.com/RPCS3/rpcs3), [.NET contributors](https://github.com/dotnet), and maintainer [@danstronics](https://github.com/danstronics).

Hyper Cheats and Ryu_Hayabusa80 are credited by verified original links; no verified GitHub handles were found. Attribution does not imply their endorsement or participation.

Extract the ZIP and run `Tekken6.Community.exe`. Preserve the included license files when redistributing it. Original trainers, game files, firmware, artwork and console packages are not included.
