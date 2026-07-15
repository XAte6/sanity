import Foundation

enum LinkOpener {
    @discardableResult
    static func open(_ urlString: String) -> Bool {
        let config = AppConfig.load()
        var finalUrl = urlString.trimmingCharacters(in: .whitespacesAndNewlines)
        var cleaned = false

        if config.isLinkProxyActive,
           let cleanedUrl = UrlCleaner.tryClean(finalUrl, rules: config.rules) {
            finalUrl = cleanedUrl
            cleaned = true
            UsageMetrics.recordClean(url: finalUrl)
        }

        BrowserHelper.open(url: finalUrl, targetBrowser: resolveTargetBrowser(config: config))
        UpdateChecker.runSync(config: config)
        return cleaned
    }

    private static func resolveTargetBrowser(config: AppConfig) -> String {
        if !config.targetBrowser.isEmpty {
            return config.targetBrowser
        }
        return UserDefaults.standard.string(forKey: "previousHttpHandler") ?? ""
    }
}
