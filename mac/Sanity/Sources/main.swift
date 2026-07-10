import AppKit
import UserNotifications

if CommandLine.arguments.count >= 3 && CommandLine.arguments[1] == "--open" {
    let cleaned = LinkOpener.open(CommandLine.arguments[2])
    if cleaned {
        let config = AppConfig.load()
        if config.notificationsEnabled {
            let content = UNMutableNotificationContent()
            content.title = "Sanity"
            content.body = "Tracking removed from clicked URL."
            let request = UNNotificationRequest(
                identifier: UUID().uuidString,
                content: content,
                trigger: nil
            )
            let center = UNUserNotificationCenter.current()
            let semaphore = DispatchSemaphore(value: 0)
            center.add(request) { _ in semaphore.signal() }
            _ = semaphore.wait(timeout: .now() + 0.5)
            RunLoop.current.run(until: Date().addingTimeInterval(0.5))
        }
    }
} else {
    let delegate = AppDelegate()
    let app = NSApplication.shared
    app.delegate = delegate
    app.run()
}
