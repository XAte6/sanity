#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
SRC="$ROOT/Sanity/Sources"
PLIST="$ROOT/Sanity/Info.plist"
BIN="$ROOT/bin"
APP="$BIN/Sanity.app"
MACOS="$APP/Contents/MacOS"
RES="$APP/Contents/Resources"
BUILD="$ROOT/.build"

if ! command -v swiftc >/dev/null 2>&1; then
    echo "Swift compiler not found. Install Xcode Command Line Tools:"
    echo "  xcode-select --install"
    exit 1
fi

mkdir -p "$MACOS" "$RES" "$BUILD"

echo "Compiling Sanity..."
swiftc -O \
    -framework AppKit \
    -framework Cocoa \
    -framework UserNotifications \
    -framework ServiceManagement \
    "$SRC"/AboutWindowController.swift \
    "$SRC"/AppConfig.swift \
    "$SRC"/AppDelegate.swift \
    "$SRC"/AppIcon.swift \
    "$SRC"/AppLinks.swift \
    "$SRC"/BrowserHelper.swift \
    "$SRC"/BrowserRegistration.swift \
    "$SRC"/ClipboardMonitor.swift \
    "$SRC"/ConfigWindowController.swift \
    "$SRC"/LinkOpener.swift \
    "$SRC"/StartupRegistration.swift \
    "$SRC"/UiChrome.swift \
    "$SRC"/UrlCleaner.swift \
    "$SRC"/UsageMetrics.swift \
    "$SRC"/main.swift \
    -o "$MACOS/Sanity"

cp "$PLIST" "$APP/Contents/Info.plist"

ICONSET="$BUILD/Sanity.iconset"
rm -rf "$ICONSET"
mkdir -p "$ICONSET"

write_icon_png() {
    local size="$1"
    local outfile="$2"
    swift - <<SWIFT
import AppKit
let size = CGFloat($size)
let image = NSImage(size: NSSize(width: size, height: size))
image.lockFocus()
NSColor(red: 34.0/255.0, green: 139.0/255.0, blue: 34.0/255.0, alpha: 1.0).setFill()
NSBezierPath(rect: NSRect(x: 0, y: 0, width: size, height: size)).fill()
let font = NSFont.boldSystemFont(ofSize: size * 0.62)
let text = "S" as NSString
let attrs: [NSAttributedString.Key: Any] = [.font: font, .foregroundColor: NSColor.white]
let textSize = text.size(withAttributes: attrs)
let rect = NSRect(x: (size - textSize.width) / 2.0, y: (size - textSize.height) / 2.0, width: textSize.width, height: textSize.height)
text.draw(in: rect, withAttributes: attrs)
image.unlockFocus()
if let tiff = image.tiffRepresentation,
   let rep = NSBitmapImageRep(data: tiff),
   let png = rep.representation(using: .png, properties: [:]) {
    try png.write(to: URL(fileURLWithPath: "$outfile"))
}
SWIFT
}

write_icon_png 16 "$ICONSET/icon_16x16.png"
write_icon_png 32 "$ICONSET/icon_16x16@2x.png"
write_icon_png 32 "$ICONSET/icon_32x32.png"
write_icon_png 64 "$ICONSET/icon_32x32@2x.png"
write_icon_png 128 "$ICONSET/icon_128x128.png"
write_icon_png 256 "$ICONSET/icon_128x128@2x.png"
write_icon_png 256 "$ICONSET/icon_256x256.png"
write_icon_png 512 "$ICONSET/icon_256x256@2x.png"
write_icon_png 512 "$ICONSET/icon_512x512.png"
write_icon_png 1024 "$ICONSET/icon_512x512@2x.png"

iconutil -c icns "$ICONSET" -o "$RES/AppIcon.icns"
/usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string AppIcon" "$APP/Contents/Info.plist" 2>/dev/null || \
/usr/libexec/PlistBuddy -c "Set :CFBundleIconFile AppIcon" "$APP/Contents/Info.plist"

"$MACOS/Sanity" --write-default-config
if [ -f "$ROOT/config.json" ]; then
    cp "$ROOT/config.json" "$BIN/config.json"
fi

ARCH="$(uname -m)"
if [ "$ARCH" != "arm64" ]; then
    echo "Release builds require Apple Silicon (arm64). Build on an M-series Mac."
    exit 1
fi

RELEASES="$ROOT/../releases"
mkdir -p "$RELEASES"
RELEASE_ZIP="$RELEASES/Sanity-mac-arm.zip"
rm -f "$RELEASE_ZIP"
(
    cd "$BIN"
    zip -r "$RELEASE_ZIP" Sanity.app config.json
)

echo "Built $APP"
echo "Copied $RELEASE_ZIP"
echo "Run: open \"$APP\""
