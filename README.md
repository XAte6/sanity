# Sanity

Sanity strips tracking parameters from URLs before you paste or open them. It runs as a small system tray / menu bar app on desktop, and as a lightweight link proxy on Android.

No installers, package managers, or runtimes required beyond what ships with each OS (Android requires a standard JDK + Android SDK for building).

## Features

- **Clipboard cleaning** — monitors copied URLs and removes trackers before you paste
- **Link proxy** — intercepts clicked links from other apps, cleans them, and forwards to your real browser
- Configurable rules: **domain** + **regex** pairs
- Default rules for YouTube, Amazon, Google, Facebook, Instagram, TikTok, X/Twitter, Reddit, and common global trackers (`utm_*`, `fbclid`, `gclid`, etc.)
- Tray / menu bar controls (desktop):
  - **Configuration** — edit rules
  - **Enabled** — turn clipboard cleaning on or off
  - **Clean clicked links** — register Sanity as the system link handler
  - **Target browser** — browser to forward cleaned links to
  - **Notifications** — toast popups when a URL is cleaned
  - **Launch on startup**
  - **Sleep** → 1h / 2h / 4h / 8h
  - **Exit**

## Project layout

```
sanity/
  win/           # Windows (C# / WinForms)
    bin/         # build output (Sanity.exe, config.json)
  mac/           # macOS (Swift / AppKit)
    bin/         # build output (Sanity.app, config.json)
  android/       # Android (Kotlin)
    bin/         # build output (Sanity.apk)
```

Desktop builds read and write `config.json` from each platform's `bin/` folder next to the executable. Android stores config in app-private storage using the same JSON schema.

## Windows

**Requirements:** .NET Framework 4.x (included with Windows; uses the built-in `csc.exe` compiler)

```bat
cd win
build.bat
bin\Sanity.exe
```

Build output: `win\bin\Sanity.exe`

### Link proxy setup (Windows)

1. Run Sanity from the tray.
2. Choose **Target browser** (e.g. Chrome, Edge, Firefox).
3. Enable **Clean clicked links**.
4. If Windows prompts, confirm Sanity as the default app for `http`/`https` links.

Links opened from apps like Slack, Outlook, or Notepad will pass through Sanity, be cleaned, and open in your chosen browser.

### Test link cleaning (Windows)

Quick check without setting Sanity as default — runs the same cleaning path as a clicked link:

```bat
bin\Sanity.exe --open "https://example.com/?utm_source=test&utm_medium=email&fbclid=IwAR_fake123&keep=visible"
```

Your target browser should open to `https://example.com/?keep=visible` (tracking params removed).

For a full end-to-end test, paste that URL into Notepad and Ctrl+click it (with **Clean clicked links** enabled and Sanity set as the default for HTTP/HTTPS).

## macOS

**Requirements:** Xcode Command Line Tools (`swiftc`, `iconutil`)

```bash
cd mac
chmod +x build.sh
./build.sh
open bin/Sanity.app
```

If `swiftc` is not found:

```bash
xcode-select --install
```

Build output: `mac/bin/Sanity.app`

### Link proxy setup (macOS)

1. Run Sanity from the menu bar.
2. Choose **Target browser**.
3. Enable **Clean clicked links**.
4. macOS will set Sanity as the default handler for `http`/`https` links.

## Android

**Requirements:** JDK 17+, Android SDK, Gradle (or Android Studio)

```bat
cd android
build.bat
```

Or with Gradle directly:

```bash
cd android
gradle :app:assembleRelease
cp app/build/outputs/apk/release/app-release-unsigned.apk bin/Sanity.apk
```

Build output: `android/bin/Sanity.apk`

### Install (sideload)

1. Install the APK on your phone (enable install from unknown sources if needed).
2. Open Sanity and choose a **target browser**.
3. Enable **Clean clicked links**.
4. Tap **Open default apps settings** and set **Sanity** as the default browser.

Sanity only runs when a link is opened — there is no background service and negligible idle battery use.

## Configuration

Settings are stored in `config.json` (desktop: `win/bin/config.json` or `mac/bin/config.json`):

| Key | Description |
|-----|-------------|
| `enabled` | Master on/off switch for cleaning |
| `linkProxyEnabled` | Intercept clicked links and forward to target browser |
| `targetBrowser` | Where to forward links (exe path or ProgId on Windows, bundle ID on macOS, package name on Android) |
| `notificationsEnabled` | Show toast when a URL is cleaned |
| `launchOnStartup` | Start with the OS (desktop only) |
| `sleepUntil` | ISO timestamp; cleaning paused until this time |
| `rules` | Array of `{ "domain", "regex" }` objects |

**Domain matching**

- `*` matches all hosts
- Otherwise matches exact host or subdomains (e.g. `amazon.com` matches `www.amazon.com`)

**Regex**

- Applied to the full URL string
- Matching text is removed (e.g. `[?&](utm_[a-zA-Z0-9_]+=[^&]*)`)

On first run, a default `config.json` is created if one does not exist.

## How it works

### Clipboard cleaning (desktop)

1. You copy a URL to the clipboard.
2. Sanity detects the change, checks the URL host against your rules, and applies matching regex patterns.
3. If the URL changed, the clipboard is updated before you paste.
4. Sanity ignores clipboard updates it writes itself to avoid loops.

### Link proxy (all platforms)

1. You click a link in another app (e.g. Slack, Gmail, SMS).
2. The OS sends the URL to Sanity (because Sanity is the registered link handler / default browser).
3. Sanity applies your rules and forwards the cleaned URL to your target browser.
4. Sanity exits immediately — no persistent background process.

## Limitations

| Scenario | Intercepted? |
|----------|--------------|
| Link click in Slack, Teams, Discord, SMS, email apps | Usually yes |
| Copy/paste URL (desktop) | Yes (clipboard mode) |
| Link clicked inside an open browser tab | No — browser handles it internally |
| In-app WebViews (Twitter, Reddit, some email apps) | Often no — app navigates internally |
| Android without setting Sanity as default browser | No |

For in-browser link cleaning, use a browser extension (e.g. ClearURLs) alongside Sanity.

## License

Use and modify as you like.
