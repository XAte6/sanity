import Foundation
import ServiceManagement

enum StartupRegistration {
    static func isRegistered() -> Bool {
        if #available(macOS 13.0, *) {
            return SMAppService.mainApp.status == .enabled
        }
        return launchAgentPath != nil
    }

    static func apply(_ launchOnStartup: Bool) {
        if #available(macOS 13.0, *) {
            do {
                if launchOnStartup {
                    try SMAppService.mainApp.register()
                } else {
                    try SMAppService.mainApp.unregister()
                }
            } catch {
                fputs("Startup registration failed: \(error)\n", stderr)
            }
            return
        }

        if launchOnStartup {
            installLaunchAgent()
        } else {
            removeLaunchAgent()
        }
    }

    private static var launchAgentURL: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appendingPathComponent("Library/LaunchAgents/com.sanity.urlcleaner.plist")
    }

    private static var launchAgentPath: String? {
        FileManager.default.fileExists(atPath: launchAgentURL.path) ? launchAgentURL.path : nil
    }

    private static func installLaunchAgent() {
        let executable = Bundle.main.bundleURL
            .appendingPathComponent("Contents/MacOS/Sanity")
            .path

        let plist: [String: Any] = [
            "Label": "com.sanity.urlcleaner",
            "ProgramArguments": [executable],
            "RunAtLoad": true
        ]

        do {
            let data = try PropertyListSerialization.data(fromPropertyList: plist, format: .xml, options: 0)
            try data.write(to: launchAgentURL, options: .atomic)
            runLaunchctl(["bootstrap", "gui/\(getuid())", launchAgentURL.path])
        } catch {
            fputs("Failed to install launch agent: \(error)\n", stderr)
        }
    }

    private static func removeLaunchAgent() {
        if FileManager.default.fileExists(atPath: launchAgentURL.path) {
            runLaunchctl(["bootout", "gui/\(getuid())", launchAgentURL.path])
        }
        try? FileManager.default.removeItem(at: launchAgentURL)
    }

    private static func runLaunchctl(_ arguments: [String]) {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/bin/launchctl")
        process.arguments = arguments
        try? process.run()
        process.waitUntilExit()
    }
}
