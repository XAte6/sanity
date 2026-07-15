import AppKit
import UserNotifications

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var config = AppConfig.load()
    private var statusItem: NSStatusItem!
    private var clipboardMonitor: ClipboardMonitor!
    private var configWindow: ConfigWindowController?
    private var aboutWindow: AboutWindowController?
    private var refreshTimer: Timer?

    private var enabledItem: NSMenuItem!
    private var targetBrowserMenu: NSMenuItem!
    private var notificationsItem: NSMenuItem!
    private var launchOnStartupItem: NSMenuItem!
    private var updatesItem: NSMenuItem!

    func applicationDidFinishLaunching(_ notification: Notification) {
        if isAnotherInstanceRunning() {
            NSApp.terminate(nil)
            return
        }

        if CommandLine.arguments.contains("--write-default-config") {
            AppConfig.createDefault().save()
            NSApp.terminate(nil)
            return
        }

        if let openIndex = CommandLine.arguments.firstIndex(of: "--open"),
           openIndex + 1 < CommandLine.arguments.count {
            let cleaned = LinkOpener.open(CommandLine.arguments[openIndex + 1])
            if cleaned {
                showNotification("Tracking removed from clicked URL.")
                // Allow the notification to be delivered before exit.
                RunLoop.current.run(until: Date().addingTimeInterval(0.5))
            }
            NSApp.terminate(nil)
            return
        }

        NSApp.setActivationPolicy(.accessory)
        NSApp.applicationIconImage = AppIcon.create(size: 128)
        StartupRegistration.apply(config.launchOnStartup)
        applyLinkHandling(enabled: config.enabled)
        requestNotificationPermission()

        clipboardMonitor = ClipboardMonitor(config: config)
        clipboardMonitor.onCleaned = { [weak self] in
            self?.showNotification("Tracking removed from copied URL.")
        }

        setupStatusItem()
        refreshTimer = Timer.scheduledTimer(withTimeInterval: 30, repeats: true) { [weak self] _ in
            self?.refreshMenuState()
        }
        refreshMenuState()
        UpdateChecker.runAsync(config: config) { [weak self] updated in
            self?.config = updated
            self?.clipboardMonitor.updateConfig(updated)
            self?.refreshMenuState()
        }
    }

    private func isAnotherInstanceRunning() -> Bool {
        let bundleId = Bundle.main.bundleIdentifier ?? "com.sanity.urlcleaner"
        let current = ProcessInfo.processInfo.processIdentifier
        let others = NSRunningApplication.runningApplications(withBundleIdentifier: bundleId)
            .filter { $0.processIdentifier != current }
        return !others.isEmpty
    }

    private func setupStatusItem() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.button?.image = AppIcon.create()
        statusItem.button?.image?.accessibilityDescription = "Sanity"

        let menu = NSMenu()

        let aboutItem = NSMenuItem(title: "Statistics", action: #selector(openAbout), keyEquivalent: "")
        aboutItem.target = self
        menu.addItem(aboutItem)

        let configItem = NSMenuItem(title: "Regex Rules", action: #selector(openConfiguration), keyEquivalent: "")
        configItem.target = self
        menu.addItem(configItem)
        menu.addItem(.separator())

        enabledItem = NSMenuItem(title: "Enabled", action: #selector(toggleEnabled), keyEquivalent: "")
        enabledItem.target = self
        menu.addItem(enabledItem)

        updatesItem = NSMenuItem(title: "Check for updates", action: #selector(toggleUpdates), keyEquivalent: "")
        updatesItem.target = self
        menu.addItem(updatesItem)

        targetBrowserMenu = NSMenuItem(title: "Target browser", action: nil, keyEquivalent: "")
        rebuildTargetBrowserMenu()
        menu.addItem(targetBrowserMenu)

        notificationsItem = NSMenuItem(title: "Notifications", action: #selector(toggleNotifications), keyEquivalent: "")
        notificationsItem.target = self
        menu.addItem(notificationsItem)

        launchOnStartupItem = NSMenuItem(title: "Launch on startup", action: #selector(toggleLaunchOnStartup), keyEquivalent: "")
        launchOnStartupItem.target = self
        menu.addItem(launchOnStartupItem)

        menu.addItem(.separator())

        let exitItem = NSMenuItem(title: "Exit", action: #selector(exitApp), keyEquivalent: "q")
        exitItem.target = self
        menu.addItem(exitItem)

        statusItem.menu = menu
    }

    @objc private func openConfiguration() {
        if let configWindow, configWindow.window?.isVisible == true {
            configWindow.showWindow()
            return
        }
        configWindow = ConfigWindowController(config: config) { [weak self] updated in
            guard let self else { return }
            self.config = updated
            self.config.save()
            self.clipboardMonitor.updateConfig(self.config)
            self.refreshMenuState()
        }
        configWindow?.showWindow()
    }

    @objc private func openAbout() {
        if let aboutWindow, aboutWindow.window?.isVisible == true {
            aboutWindow.showWindow()
            return
        }
        aboutWindow = AboutWindowController()
        aboutWindow?.showWindow()
    }

    @objc private func toggleEnabled() {
        config.enabled.toggle()
        if config.enabled {
            config.sleepUntil = nil
        }
        applyLinkHandling(enabled: config.enabled)
        config.save()
        clipboardMonitor.updateConfig(config)
        rebuildTargetBrowserMenu()
        refreshMenuState()
    }

    private func applyLinkHandling(enabled: Bool) {
        config.linkProxyEnabled = enabled
        if enabled && config.targetBrowser.isEmpty {
            config.targetBrowser = BrowserHelper.defaultBrowserBundleId() ?? ""
        }
        BrowserRegistration.apply(enabled: enabled, config: &config)
    }

    private func rebuildTargetBrowserMenu() {
        let submenu = NSMenu()
        for browser in BrowserHelper.installedBrowsers() {
            let item = NSMenuItem(title: browser.name, action: #selector(selectTargetBrowser(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = browser.bundleId
            submenu.addItem(item)
        }
        if submenu.items.isEmpty {
            submenu.addItem(NSMenuItem(title: "(no browsers found)", action: nil, keyEquivalent: ""))
        }
        targetBrowserMenu.submenu = submenu
        updateTargetBrowserChecks()
    }

    @objc private func selectTargetBrowser(_ sender: NSMenuItem) {
        guard let bundleId = sender.representedObject as? String else { return }
        config.targetBrowser = bundleId
        config.save()
        refreshMenuState()
    }

    @objc private func toggleNotifications() {
        config.notificationsEnabled.toggle()
        config.save()
        refreshMenuState()
    }

    @objc private func toggleUpdates() {
        config.updatesEnabled.toggle()
        config.save()
        refreshMenuState()
    }

    @objc private func toggleLaunchOnStartup() {
        config.launchOnStartup.toggle()
        StartupRegistration.apply(config.launchOnStartup)
        config.save()
        refreshMenuState()
    }

    @objc private func exitApp() {
        NSApp.terminate(nil)
    }

    private func refreshMenuState() {
        let sleeping = config.isSleeping()
        let active = config.isActive

        enabledItem.state = (config.enabled && !sleeping) ? .on : .off
        enabledItem.title = sleeping ? "Enabled (sleeping)" : "Enabled"
        updateTargetBrowserChecks()
        notificationsItem.state = config.notificationsEnabled ? .on : .off
        updatesItem.state = config.updatesEnabled ? .on : .off
        launchOnStartupItem.state = config.launchOnStartup ? .on : .off

        if active {
            statusItem.button?.toolTip = "Sanity - active"
        } else if sleeping, let until = config.sleepUntilDate() {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            statusItem.button?.toolTip = "Sanity - sleeping until \(formatter.string(from: until))"
        } else {
            statusItem.button?.toolTip = "Sanity - disabled"
        }
    }

    private func updateTargetBrowserChecks() {
        guard let submenu = targetBrowserMenu.submenu else { return }
        for item in submenu.items {
            guard let bundleId = item.representedObject as? String else {
                item.state = .off
                continue
            }
            item.state = bundleId == config.targetBrowser ? .on : .off
        }
    }

    func application(_ sender: NSApplication, open urls: [URL]) {
        for url in urls {
            let cleaned = LinkOpener.open(url.absoluteString)
            if cleaned {
                showNotification("Tracking removed from clicked URL.")
            }
        }
    }

    private func requestNotificationPermission() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

    private func showNotification(_ message: String) {
        guard config.notificationsEnabled else { return }

        let content = UNMutableNotificationContent()
        content.title = "Sanity"
        content.body = message

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )
        UNUserNotificationCenter.current().add(request)
    }
}
