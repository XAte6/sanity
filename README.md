# Sanity

Sanity strips tracking parameters from URLs before you paste or open them. It runs as a small system tray / menu bar app on desktop, and as a lightweight link proxy on Android.

Windows ships a single setup EXE (no separate runtime). macOS and Android need no package managers beyond what each OS already provides (Android builds need a standard JDK + Android SDK).

**GitHub:** [XAte6/sanity](https://github.com/XAte6/sanity) · **Support:** [Issues](https://github.com/XAte6/sanity/issues) · **Tip me:** [paypal.me/XAte6](https://paypal.me/XAte6)

## Features

- **Clipboard cleaning** — monitors copied URLs and removes trackers before you paste
- **Link proxy** — intercepts clicked links from other apps, cleans them, and forwards to your real browser
- **Share target (Android)** — share a URL to Sanity, then forward the cleaned link to another app
- Configurable rules: **domain** + **regex** pairs
- Default rules (see `defaults/regex-rules.json`) for shopping, social, search, news publishers, and common global trackers (`utm_*`, click IDs, HubSpot/Marketo, etc.)
- **Usage metrics** — counts cleaned links and unique domains (shown in About)
- Tray / menu bar controls (desktop):
  - **Regex Rules** — edit domain + regex patterns
  - **Enabled** — turn clipboard cleaning on or off
  - **Target browser** — browser to forward cleaned links to
  - **Notifications** — toast popups when a URL is cleaned (clipboard and clicked links)
  - **Launch on startup**
  - **Sleep** → 1h / 2h / 4h / 8h
  - **Statistics** — usage stats, GitHub, Support, Tip me
  - **Exit**

## Releases

Pre-built binaries live in the [`releases/`](releases/) folder in this repo:

| File | Platform |
|------|----------|
| `Sanity-win-x86-setup.exe` | Windows installer (Intel / AMD64) |
| `Sanity-mac-arm.zip` | macOS (Apple Silicon) |
| `Sanity-android-arm.apk` | Android (ARM64) |

Run each platform's `build.bat` / `build.sh` to refresh these files after code changes. Newer Windows setup builds upgrade over a previous Sanity install and keep your settings.

### Run a release

**Windows**

1. Download [`releases/Sanity-win-x86-setup.exe`](releases/Sanity-win-x86-setup.exe).
2. Run the installer (no admin required). Sanity installs under `%LocalAppData%\Sanity`, adds a Start Menu shortcut, and can launch when finished.
3. On first launch, complete the setup wizard (target browser, optional enable / startup / notifications). Cleaning stays off until you enable it in the wizard or later from the tray.
4. Later versions: run the newer setup EXE — it replaces the previous install and preserves `config.json` / `metrics.json`.
5. Uninstall from **Settings → Apps** (or Start Menu → Sanity → Uninstall). Your settings files are left in the install folder.

**macOS**

1. Download or copy [`releases/Sanity-mac-arm.zip`](releases/Sanity-mac-arm.zip).
2. Unzip — you get `Sanity.app` and `config.json`. Move both to Applications or run from the unzip folder.
3. **First launch only:** macOS may block unsigned apps. **Right-click** `Sanity.app` → **Open** → **Open**. After that, double-click works normally. (Alternatively: **System Settings → Privacy & Security → Open Anyway** after a blocked attempt.)

**Android**

1. Download or copy [`releases/Sanity-android-arm.apk`](releases/Sanity-android-arm.apk).
2. Install on your device (enable install from unknown sources if prompted).
3. Open Sanity, set your target browser, enable **Clean clicked links**, and set Sanity as the default browser (see **Install and use (sideload)** below).

To build from source instead of using a release, see the platform sections below.

## Project layout

```
sanity/
  defaults/      # shared regex-rules.json (versioned; source for reset + weekly updates)
  releases/      # shipped binaries (Sanity-{platform}-{arch}…); Windows is a setup EXE
  win/           # Windows (C# / WinForms + Inno Setup)
    VERSION      # bump before release (e.g. 1.0.1) so setup upgrades prior installs
    bin/         # build output (Sanity.exe, config.json)
  mac/           # macOS (Swift / AppKit)
    bin/         # build output (Sanity.app, config.json)
  android/       # Android (Kotlin)
    bin/         # build output (Sanity.apk)
```

Desktop builds read and write `config.json` from each platform's `bin/` folder next to the executable. Usage counts are stored in `metrics.json` beside it. Android stores config and metrics in app-private storage using the same JSON schemas.

### Default regex list

The shared defaults live at [`defaults/regex-rules.json`](defaults/regex-rules.json). Bump the top-level `version` whenever the rule list changes. Clients use that file for:

- First-run / reset defaults (local copy shipped with the app; reset prefers the GitHub raw URL)
- A once-weekly check (on tray/settings launch, clicked-link / share handling, and clipboard cleaning): if the remote `version` is higher than `rulesVersion` in the user's config, Sanity asks whether to replace their rules
- The same weekly check compares the last commit date of the platform file under `releases/` on GitHub to the installed executable (or package) date and asks to open the download if the release file is newer

## Windows

**Requirements to run:** .NET Framework 4.x (included with modern Windows)

**Requirements to build:** built-in `csc.exe`, plus [Inno Setup 6](https://jrsoftware.org/isinfo.php) for the setup EXE (`winget install --id JRSoftware.InnoSetup -e`)

```bat
cd win
build.bat
```

Build output:

- `win\bin\Sanity.exe` — local tray build for development
- `releases\Sanity-win-x86-setup.exe` — single-file installer (bumps over prior installs via a fixed AppId)

Version is set in `win\VERSION` (e.g. `1.0.0`). Bump it before a release so the new setup upgrades the previous one.

### Link proxy setup (Windows)

1. Complete the first-run wizard (or open the tray menu later).
2. Choose **Target browser** (e.g. Chrome, Edge, Firefox).
3. Turn on **Enabled** (clipboard cleaning and clicked-link proxy).
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

You need to **build an APK** on your PC, then install it on your phone. There are two ways to do that.

### Option A: Android Studio (recommended if you're new to this)

This is the easiest path. You do **not** need to install Gradle, the Android SDK, or a JDK separately — Android Studio bundles all of them.

1. Download and install [Android Studio](https://developer.android.com/studio) for Windows (the recommended `.exe` installer is fine).
2. Open Android Studio → **Open** → select the `android` folder in this repo.
3. Wait for the initial sync to finish (it will download the SDK and dependencies).
4. **Build → Build Bundle(s) / APK(s) → Build APK(s)**.
5. When the build finishes, click **locate** in the notification, or find the APK at:
   `android/app/build/outputs/apk/release/app-release-unsigned.apk`

Copy that APK to your phone and install it (see **Install (sideload)** below).

### Option B: Command line (`build.bat`)

Only use this if you already have Android development tools set up, or you specifically want a scriptable build.

**Requirements:** JDK 17+, Android SDK, and Gradle on your `PATH`.

- **JDK 17+** — Android Studio includes one; for CLI-only setups, install a JDK and set `JAVA_HOME`.
- **Android SDK** — install via Android Studio's SDK Manager, or the [command-line tools](https://developer.android.com/studio#command-line-tools-only). Set `ANDROID_HOME` (usually `C:\Users\<you>\AppData\Local\Android\Sdk`).
- **Gradle** — must be installed separately and available on `PATH`. `winget` does **not** currently ship Gradle. Use [Chocolatey](https://chocolatey.org/) (`choco install gradle`), [Scoop](https://scoop.sh/) (`scoop install gradle`), or a [manual install](https://gradle.org/install/) (this project uses Gradle 8.7).

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

### Install and use (sideload)

Sanity is **not** a browser and does **not** run in the background. It is a link proxy: Android hands clicked links to Sanity, Sanity strips trackers, then forwards the cleaned URL to your real browser and exits.

**Setup (do this in order):**

1. Install the APK on your phone (enable install from unknown sources if needed).
2. Open Sanity and pick a **target browser** (e.g. Chrome) from the dropdown.
3. Turn on **Enabled** and **Clean clicked links**.
4. Tap **Open default apps settings** and set **Sanity** as the default browser.

**Using it (clicked links):** tap a link in WhatsApp, Gmail, SMS, etc. You may see a brief “tracking removed” toast; your chosen browser should then open the cleaned page. You do not need to keep Sanity open.

**Using it (copied / pasted links):** select the URL (or use Share on a page/link), choose **Share → Sanity**, then pick the app to send the cleaned link to (Messages, WhatsApp, your browser, etc.). Cleaning always runs when you share to Sanity.

**Deploy from Android Studio (wireless debugging):** with your phone paired in Device Manager, click **Run** to build, install, and launch in one step.

**Deploy an existing APK via adb:**

```powershell
adb install -r android\app\build\outputs\apk\release\app-release-unsigned.apk
```

(`adb` is in `%LOCALAPPDATA%\Android\Sdk\platform-tools\`.)

**Editing rules on device:** open Sanity → **Regex Rules**. Filter by domain or regex, tap pencil to edit (with a URL test preview), bin to delete, or + to add. **Statistics** shows cleaned-link totals and per-domain shares.

## Configuration

Settings are stored in `config.json` (desktop: `win/bin/config.json` or `mac/bin/config.json`):

| Key | Description |
|-----|-------------|
| `enabled` | Master on/off switch for cleaning (Windows defaults to off until setup) |
| `linkProxyEnabled` | Intercept clicked links and forward to target browser |
| `targetBrowser` | Where to forward links (exe path or ProgId on Windows, bundle ID on macOS, package name on Android) |
| `notificationsEnabled` | Show toast when a URL is cleaned |
| `updatesEnabled` | Weekly checks for regex catalog + newer release builds (default on) |
| `launchOnStartup` | Start with the OS (desktop only) |
| `setupCompleted` | Windows only: first-run wizard finished |
| `rulesVersion` | Version of the applied default regex catalog |
| `lastUpdateCheck` | UTC time of the last weekly update check |
| `sleepUntil` | ISO timestamp; cleaning paused until this time |
| `rules` | Array of `{ "domain", "regex" }` objects |

**Domain matching**

- `*` matches all hosts
- Otherwise matches exact host or subdomains (e.g. `amazon.com` matches `www.amazon.com`)

**Regex**

- Applied to the full URL string
- Matching text is removed (e.g. `[?&](utm_[a-zA-Z0-9_]+=[^&]*)`)

On first run, a default `config.json` is created if one does not exist. On Windows, the first-run setup wizard runs until `setupCompleted` is true.

Usage is stored separately in `metrics.json` (`linksCleaned` plus a per-host `domains` map). Open **Statistics** from the tray/menu to see totals and links to GitHub, Support, and Tip me.

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

### Share target (Android)

1. You select a URL and tap **Share → Sanity** (or share a link from Chrome, etc.).
2. Sanity applies your rules.
3. Android’s share sheet opens so you can send the cleaned URL to Messages, WhatsApp, a browser, etc.
4. Sanity exits immediately.

## Limitations

| Scenario | Intercepted? |
|----------|--------------|
| Link click in Slack, Teams, Discord, SMS, email apps | Usually yes |
| Copy/paste URL (desktop) | Yes (clipboard mode) |
| Copy/paste URL (Android) | Via Share → Sanity → Share (manual two-step) |
| Link clicked inside an open browser tab | No — browser handles it internally |
| In-app WebViews (Twitter, Reddit, some email apps) | Often no — app navigates internally |
| Android without setting Sanity as default browser | No |

For in-browser link cleaning, use a browser extension (e.g. ClearURLs) alongside Sanity.

## License

Use and modify as you like.

## Support development

If Sanity is useful, you can [tip via PayPal](https://paypal.me/XAte6). Bug reports and ideas welcome on [GitHub Issues](https://github.com/XAte6/sanity/issues).
