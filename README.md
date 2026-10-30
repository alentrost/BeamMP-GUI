# BeamMP-GUI

A lightweight, native Windows desktop manager and configuration GUI for [BeamMP Server](https://beammp.com).

---

## Screenshots

### Dashboard & Live Console
![Dashboard](docs/screenshots/dashboard.png)

### Dedicated Configuration Editor
![Configuration Editor](docs/screenshots/config_editor.png)

### Client Mods Manager
![Client Mods](docs/screenshots/client_mods.png)

---

## Setup Instructions

1. **Download** the latest `BeamMP-GUI.exe` from [Releases](https://github.com/alentrost/BeamMP-GUI/releases/latest).
2. **Move** `BeamMP-GUI.exe` directly into your BeamMP server directory (the folder containing `BeamMP-Server.exe` and `ServerConfig.toml`).
3. **Run** `BeamMP-GUI.exe`.

> **Note:** Requires Windows 10 or 11 (.NET Framework 4.8 is built-in). No external runtimes (Python, Node, etc.) required.

---

## Features

- **One-Click Server Control:** Start, stop, and restart `BeamMP-Server.exe` with real-time process ID, port, and uptime tracking.
- **Live Interactive Console:** Standard output and error streaming with auto-scroll, copy, clear, and interactive command sending.
- **Console Command Presets:** Quick one-click execution for `status`, `list`, `say`, `kick`, `reloadmods`, `version`, and `help`.
- **Dedicated Configuration Editor:** Visual form editor for `ServerConfig.toml`:
  - Official BeamNG map presets (*Italy, West Coast USA, East Coast USA, Gridmap v2, Utah, Jungle Rock Island, etc.*) + custom map paths.
  - Max player and car limits.
  - AuthKey field with show/hide toggle and Keymaster shortcut.
  - Server privacy, guest players, information packets, and chat logging toggles.
- **Raw TOML Editor:** Instant toggle between visual form and raw `ServerConfig.toml` text editing with comment preservation.
- **Client Mods Integration:** View installed `.zip` mods and disk usage, open `Resources/Client/` in Explorer, and drag-and-drop `.zip` files to install.
- **Safe Process Shutdown:** Gracefully sends `exit` command on stop to prevent locked networking ports.

---

## Building from Source

To build `BeamMP-GUI.exe` yourself:

1. Clone the repository:
   ```cmd
   git clone https://github.com/alentrost/BeamMP-GUI.git
   cd BeamMP-GUI
   ```
2. Run `build.bat` or build using MSBuild:
   ```cmd
   "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "AppSource\BeamMP-GUI.csproj" /p:Configuration=Release
   ```
