import AppKit

if CommandLine.arguments.count >= 3 && CommandLine.arguments[1] == "--open" {
    _ = LinkOpener.open(CommandLine.arguments[2])
} else {
    let delegate = AppDelegate()
    let app = NSApplication.shared
    app.delegate = delegate
    app.run()
}
