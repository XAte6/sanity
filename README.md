# Sanity

Sanity is a small system tray / menu bar app that watches the clipboard for URLs and strips tracking parameters before you paste them.

No installers, package managers, or runtimes required beyond what ships with each OS.

## Features

- Monitors the clipboard and cleans URLs in the background
- Configurable rules: **domain** + **regex** pairs
- Default rules for YouTube, Amazon, Google, Facebook, Instagram, TikTok, X/Twitter, Reddit, and common global trackers (`utm_*`, `fbclid`, `gclid`, etc.)
- Tray / menu bar controls:
  - **Configuration** — edit rules
  - **Enabled** — turn cleaning on or off
  - **Notifications** — toast popups when a URL is cleaned
  - **Launch on startup**
  - **Sleep** → 1h / 2h / 4h / 8h
  - **Exit**

## Project layout

```
sanity/
  bin/           # build output (Sanity.exe, Sanity.app, runtime config.json)
  config.json    # default rule template
  win/           # Windows (C# / WinForms)
    src/
    build.bat
  mac/           # macOS (Swift / AppKit)
    build.sh
    Sanity/
```

Both platforms read and write `config.json` from the `bin/` folder next to the executable.

## Windows

**Requirements:** .NET Framework 4.x (included with Windows; uses the built-in `csc.exe` compiler)

```bat
cd win
build.bat
..\bin\Sanity.exe
```

Build output: `bin\Sanity.exe`

## macOS

**Requirements:** Xcode Command Line Tools (`swiftc`, `iconutil`)

```bash
cd mac
chmod +x build.sh
./build.sh
open ../bin/Sanity.app
```

If `swiftc` is not found:

```bash
xcode-select --install
```

Build output: `bin/Sanity.app`

## Configuration

Settings are stored in `bin/config.json`:

| Key | Description |
|-----|-------------|
| `enabled` | Master on/off switch |
| `notificationsEnabled` | Show toast when a URL is cleaned |
| `launchOnStartup` | Start with the OS |
| `sleepUntil` | ISO timestamp; cleaning paused until this time |
| `rules` | Array of `{ "domain", "regex" }` objects |

**Domain matching**

- `*` matches all hosts
- Otherwise matches exact host or subdomains (e.g. `amazon.com` matches `www.amazon.com`)

**Regex**

- Applied to the full URL string
- Matching text is removed (e.g. `[?&](utm_[a-zA-Z0-9_]+=[^&]*)`)

On first run, a default `config.json` is created if one does not exist. The root `config.json` is a template copied into `bin/` during build.

## How it works

1. You copy a URL to the clipboard.
2. Sanity detects the change, checks the URL host against your rules, and applies matching regex patterns.
3. If the URL changed, the clipboard is updated before you paste.
4. Sanity ignores clipboard updates it writes itself to avoid loops.

## License

Use and modify as you like.
