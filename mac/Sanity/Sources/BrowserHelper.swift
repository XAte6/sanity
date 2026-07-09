import AppKit
import Foundation

struct BrowserInfo {
    let name: String
    let bundleId: String
    let path: String
}

enum BrowserHelper {
    static func defaultBrowserBundleId() -> String? {
        guard let httpId = copyDefaultHandler(for: "http") else { return nil }
        if httpId != BrowserRegistration.sanityBundleId {
            return httpId
        }
        return copyDefaultHandler(for: "https")
    }

    static func installedBrowsers() -> [BrowserInfo] {
        var browsers: [BrowserInfo] = []
        var seen = Set<String>()

        let knownIds = [
            "com.google.Chrome",
            "com.google.Chrome.canary",
            "org.mozilla.firefox",
            "com.apple.Safari",
            "com.microsoft.edgemac",
            "company.thebrowser.Browser",
            "com.brave.Browser",
            "com.operasoftware.Opera",
            "com.vivaldi.Vivaldi"
        ]

        for bundleId in knownIds {
            guard let url = NSWorkspace.shared.urlForApplication(withBundleIdentifier: bundleId) else {
                continue
            }
            let path = url.path
            guard seen.insert(path).inserted else { continue }
            let name = FileManager.default.displayName(atPath: path)
            browsers.append(BrowserInfo(name: name, bundleId: bundleId, path: path))
        }

        if let defaultId = defaultBrowserBundleId(),
           let url = NSWorkspace.shared.urlForApplication(withBundleIdentifier: defaultId) {
            let path = url.path
            if seen.insert(path).inserted {
                let name = "System default (\(FileManager.default.displayName(atPath: path)))"
                browsers.insert(BrowserInfo(name: name, bundleId: defaultId, path: path), at: 0)
            }
        }

        return browsers
    }

    @discardableResult
    static func open(url: String, targetBrowser: String) -> Bool {
        guard let targetUrl = URL(string: url) else { return false }

        if !targetBrowser.isEmpty {
            if targetBrowser.contains("/"), FileManager.default.fileExists(atPath: targetBrowser) {
                let config = NSWorkspace.OpenConfiguration()
                NSWorkspace.shared.open([targetUrl], withApplicationAt: URL(fileURLWithPath: targetBrowser), configuration: config)
                return true
            }

            if targetBrowser != BrowserRegistration.sanityBundleId,
               let appUrl = NSWorkspace.shared.urlForApplication(withBundleIdentifier: targetBrowser) {
                let config = NSWorkspace.OpenConfiguration()
                NSWorkspace.shared.open([targetUrl], withApplicationAt: appUrl, configuration: config)
                return true
            }
        }

        if let defaultId = defaultBrowserBundleId(),
           defaultId != BrowserRegistration.sanityBundleId,
           let appUrl = NSWorkspace.shared.urlForApplication(withBundleIdentifier: defaultId) {
            let config = NSWorkspace.OpenConfiguration()
            NSWorkspace.shared.open([targetUrl], withApplicationAt: appUrl, configuration: config)
            return true
        }

        NSWorkspace.shared.open(targetUrl)
        return true
    }

    private static func copyDefaultHandler(for scheme: String) -> String? {
        guard let cfValue = LSCopyDefaultHandlerForURLScheme(scheme as CFString)?.takeRetainedValue() else {
            return nil
        }
        return cfValue as String
    }
}
