import AppKit
import Foundation

enum BrowserRegistration {
    static let sanityBundleId = Bundle.main.bundleIdentifier ?? "com.sanity.urlcleaner"
    private static let backupHttpKey = "previousHttpHandler"
    private static let backupHttpsKey = "previousHttpsHandler"

    static func isRegistered() -> Bool {
        currentHandler(for: "http") == sanityBundleId
    }

    static func apply(enabled: Bool, config: inout AppConfig) {
        if enabled {
            if config.targetBrowser.isEmpty {
                config.targetBrowser = BrowserHelper.defaultBrowserBundleId() ?? ""
            }
            backupCurrentHandlers()
            registerSanityAsHandler()
        } else {
            restorePreviousHandlers()
        }
    }

    private static func backupCurrentHandlers() {
        let defaults = UserDefaults.standard
        if let http = currentHandler(for: "http"), http != sanityBundleId {
            defaults.set(http, forKey: backupHttpKey)
        }
        if let https = currentHandler(for: "https"), https != sanityBundleId {
            defaults.set(https, forKey: backupHttpsKey)
        }
    }

    private static func restorePreviousHandlers() {
        let defaults = UserDefaults.standard
        if let http = defaults.string(forKey: backupHttpKey) {
            LSSetDefaultHandlerForURLScheme("http" as CFString, http as CFString)
            defaults.removeObject(forKey: backupHttpKey)
        }
        if let https = defaults.string(forKey: backupHttpsKey) {
            LSSetDefaultHandlerForURLScheme("https" as CFString, https as CFString)
            defaults.removeObject(forKey: backupHttpsKey)
        }
    }

    private static func registerSanityAsHandler() {
        LSSetDefaultHandlerForURLScheme("http" as CFString, sanityBundleId as CFString)
        LSSetDefaultHandlerForURLScheme("https" as CFString, sanityBundleId as CFString)
    }

    private static func currentHandler(for scheme: String) -> String? {
        guard let value = LSCopyDefaultHandlerForURLScheme(scheme as CFString)?.takeRetainedValue() else {
            return nil
        }
        return value as String
    }
}
