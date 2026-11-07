# Third-party components

The source project has no external NuGet package or bundled binary dependency. It uses Microsoft's .NET and Windows Forms frameworks.

Self-contained release builds include .NET runtime **10.0.14**, including Windows Desktop components. These components are redistributed under their original terms. `build.ps1` copies the license and notice files supplied in the restored runtime packs into each release ZIP's `licenses/` directory. The core runtime pack supplies `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`; the Windows Desktop pack supplies `LICENSE`.

Upstream references:

- [.NET runtime license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)
- [.NET runtime third-party notices](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT)
- [Windows Forms license](https://github.com/dotnet/winforms/blob/main/LICENSE.TXT)

No original trainer assemblies or assets are included. GitHub Actions used to build the project run on GitHub's infrastructure and are not packaged with the application.
