import AppKit

final class ClipboardMonitor {
    private var config: AppConfig
    private var lastChangeCount = NSPasteboard.general.changeCount
    private var isUpdatingClipboard = false
    private var timer: Timer?
    var onCleaned: (() -> Void)?

    init(config: AppConfig) {
        self.config = config
        timer = Timer.scheduledTimer(withTimeInterval: 0.35, repeats: true) { [weak self] _ in
            self?.checkClipboard()
        }
    }

    func updateConfig(_ config: AppConfig) {
        self.config = config
    }

    private func checkClipboard() {
        guard !isUpdatingClipboard, config.isActive else { return }

        let pasteboard = NSPasteboard.general
        let changeCount = pasteboard.changeCount
        guard changeCount != lastChangeCount else { return }
        lastChangeCount = changeCount

        guard let text = pasteboard.string(forType: .string),
              let cleaned = UrlCleaner.tryClean(text, rules: config.rules) else {
            return
        }

        isUpdatingClipboard = true
        pasteboard.clearContents()
        pasteboard.setString(cleaned, forType: .string)
        lastChangeCount = pasteboard.changeCount
        isUpdatingClipboard = false
        UsageMetrics.recordClean(url: cleaned)
        onCleaned?()
    }
}
