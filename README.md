# Terraria Map Editor - BinaryConstruct

[![Build status](https://ci.appveyor.com/api/projects/status/xi3k3j54un10a0o4?svg=true)](https://ci.appveyor.com/project/BinaryConstruct/terraria-map-editor) [![GitHub Version](https://img.shields.io/github/tag/TEdit/Terraria-Map-Editor.svg?label=GitHub)](https://github.com/TEdit/Terraria-Map-Editor) [![CodeFactor](https://www.codefactor.io/repository/github/tedit/terraria-map-editor/badge)](https://www.codefactor.io/repository/github/tedit/terraria-map-editor)

![tedit](https://github.com/TEdit/Terraria-Map-Editor/blob/main/docs/images/te-logo.png)

TEdit - Terraria Map Editor is a stand alone, open source map editor for Terraria. It lets you edit maps just like (almost) paint!

## Important Links

- [TEdit Documentation](https://docs.tedit.dev/)
- [Join us on Discord](https://discord.gg/xHcHd7mfpn)
- [Dev Blog](http://binaryconstruct.com/)

## Download

- [Source](http://github.com/TEdit/Terraria-Map-Editor)
- [Download](https://github.com/TEdit/Terraria-Map-Editor/releases/latest)
- [GitHub Releases](https://github.com/TEdit/Terraria-Map-Editor/releases)
- [Change Log](http://github.com/TEdit/Terraria-Map-Editor/commits/master)

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows only: Windows 10 SDK (for the WPF/legacy build)

### Projects

| Project | UI Framework | Platforms |
|---------|-------------|-----------|
| `src/TEdit` | WPF | Windows only |
| `src/TEdit5` | Avalonia | Windows, macOS, Linux |

### macOS / Linux (Avalonia — TEdit5)

#### Prerequisites

Install [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). On Ubuntu/Debian you can install without sudo using Microsoft's script:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"  # add to ~/.bashrc or ~/.zshrc to persist
```

#### macOS

**Run in development:**
```bash
dotnet run --project src/TEdit5/TEdit5.csproj -r osx-arm64
# or for Intel Mac:
dotnet run --project src/TEdit5/TEdit5.csproj -r osx-x64
```

**Build a distributable `.app` bundle:**
```bash
dotnet publish src/TEdit5/TEdit5.csproj -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true -o publish/osx-arm64
# TEdit.app is created at publish/TEdit.app
open publish/TEdit.app
```

To install system-wide: `cp -r publish/TEdit.app /Applications/`

> **First launch on macOS:** if Gatekeeper blocks the app (unsigned binary), right-click -> Open -> Open, or run `xattr -cr publish/TEdit.app` before opening.

#### Linux

**Run in development:**
```bash
dotnet run --project src/TEdit5/TEdit5.csproj -r linux-x64
# Open a specific world file:
dotnet run --project src/TEdit5/TEdit5.csproj -r linux-x64 -- /path/to/world.wld
```

**Build and install (self-contained, no .NET required at runtime):**
```bash
dotnet publish src/TEdit5/TEdit5.csproj -c Release -r linux-x64 --self-contained \
  -p:PublishSingleFile=true -o publish/linux-x64

# Install to ~/.local/bin, register .wld icon and file association:
bash publish/linux-x64/install-linux.sh

# Set TEdit as the default app for .wld files:
xdg-mime default tedit.desktop application/x-terraria-world
```

After installation, double-clicking a `.wld` file in your file manager will open it in TEdit, and you can also launch from the terminal:
```bash
TEdit5 /path/to/world.wld
```

### Windows (WPF — TEdit, legacy)

Open `src/TEdit.slnx` in Visual Studio 2022 and build, or:
```powershell
dotnet publish src/TEdit/TEdit.csproj -c Release -r win-x64 --self-contained
```

### Release builds (all platforms)

```powershell
# Avalonia (Windows + macOS + Linux)
.\build-avalonia.ps1 -VersionPrefix "5.0.0" -VersionSuffix "alpha0"

# Legacy WPF Windows build (used by the main build.ps1 pipeline)
.\build-legacy.ps1
```

## Languages

Help update TEdit language support on crowdin https://crowdin.com/project/tedit/invite?h=9c3543b1a29200f5f240fc426a928b1b2694868

## Support

Do you enjoy TEdit, and would you like to show your support? Every donation is appreciated and helps keep development going, servers online and ad free. Thank you for considering becoming a patron. [Patreon](https://www.patreon.com/join/BinaryConstruct)
