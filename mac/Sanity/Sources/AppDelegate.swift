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
    private var linkProxyItem: NSMenuItem!
    private var targetBrowserMenu: NSMenuItem!
    private var notificationsItem: NSMenuItem!
    private var launchOnStartupItem: NSMenuItem!
    private var sleepMenu: NSMenuItem!
    private var sleep1hItem: NSMenuItem!
    private var sleep2hItem: NSMenuItem!
    private var sleep4hItem: NSMenuItem!
    private var sleep8hItem: NSMenuItem!

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
        if config.linkProxyEnabled {
            BrowserRegistration.apply(enabled: true, config: &config)
            config.save()
        }
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

        let configItem = NSMenuItem(title: "Configuration", action: #selector(openConfiguration), keyEquivalent: "")
        configItem.target = self
        menu.addItem(configItem)
        menu.addItem(.separator())

        enabledItem = NSMenuItem(title: "Enabled", action: #selector(toggleEnabled), keyEquivalent: "")
        enabledItem.target = self
        menu.addItem(enabledItem)

        linkProxyItem = NSMenuItem(title: "Clean clicked links", action: #selector(toggleLinkProxy), keyEquivalent: "")
        linkProxyItem.target = self
        menu.addItem(linkProxyItem)

        targetBrowserMenu = NSMenuItem(title: "Target browser", action: nil, keyEquivalent: "")
        rebuildTargetBrowserMenu()
        menu.addItem(targetBrowserMenu)

        notificationsItem = NSMenuItem(title: "Notifications", action: #selector(toggleNotifications), keyEquivalent: "")
        notificationsItem.target = self
        menu.addItem(notificationsItem)

        launchOnStartupItem = NSMenuItem(title: "Launch on startup", action: #selector(toggleLaunchOnStartup), keyEquivalent: "")
        launchOnStartupItem.target = self
        menu.addItem(launchOnStartupItem)

        sleepMenu = NSMenuItem(title: "Sleep", action: nil, keyEquivalent: "")
        let sleepSubmenu = NSMenu()
        sleep1hItem = makeSleepItem(title: "1 hour", hours: 1)
        sleep2hItem = makeSleepItem(title: "2 hours", hours: 2)
        sleep4hItem = makeSleepItem(title: "4 hours", hours: 4)
        sleep8hItem = makeSleepItem(title: "8 hours", hours: 8)
        sleepSubmenu.addItem(sleep1hItem)
        sleepSubmenu.addItem(sleep2hItem)
        sleepSubmenu.addItem(sleep4hItem)
        sleepSubmenu.addItem(sleep8hItem)
        sleepMenu.submenu = sleepSubmenu
        menu.addItem(sleepMenu)

        menu.addItem(.separator())

        let aboutItem = NSMenuItem(title: "Statistics", action: #selector(openAbout), keyEquivalent: "")
        aboutItem.target = self
        menu.addItem(aboutItem)

        let exitItem = NSMenuItem(title: "Exit", action: #selector(exitApp), keyEquivalent: "q")
        exitItem.target = self
        menu.addItem(exitItem)

        statusItem.menu = menu
    }

    private func makeSleepItem(title: String, hours: Int) -> NSMenuItem {
        let item = NSMenuItem(title: title, action: #selector(setSleep(_:)), keyEquivalent: "")
        item.target = self
        item.tag = hours
        return item
    }

    @objc private func openConfiguration() {
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
        aboutWindow = AboutWindowController()
        aboutWindow?.showWindow()
    }

    @objc private func toggleEnabled() {
        config.enabled.toggle()
        if config.enabled {
            config.sleepUntil = nil
        }
        config.save()
        clipboardMonitor.updateConfig(config)
        refreshMenuState()
    }

    @objc private func toggleLinkProxy() {
        config.linkProxyEnabled.toggle()
        if config.linkProxyEnabled && config.targetBrowser.isEmpty {
            config.targetBrowser = BrowserHelper.defaultBrowserBundleId() ?? ""
        }
        BrowserRegistration.apply(enabled: config.linkProxyEnabled, config: &config)
        config.save()
        rebuildTargetBrowserMenu()
        refreshMenuState()
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

    @objc private func toggleLaunchOnStartup() {
        config.launchOnStartup.toggle()
        StartupRegistration.apply(config.launchOnStartup)
        config.save()
        refreshMenuState()
    }

    @objc private func setSleep(_ sender: NSMenuItem) {
        let hours = sender.tag
        if let sleepUntil = config.sleepUntilDate(),
           sleepUntil > Date(),
           abs(sleepUntil.timeIntervalSince(Date().addingTimeInterval(Double(hours) * 3600))) < 300 {
            config.sleepUntil = nil
        } else {
            config.sleepUntil = AppConfig.encodeSleepDate(Date().addingTimeInterval(Double(hours) * 3600))
        }
        config.save()
        clipboardMonitor.updateConfig(config)
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
        linkProxyItem.state = config.linkProxyEnabled ? .on : .off
        updateTargetBrowserChecks()
        notificationsItem.state = config.notificationsEnabled ? .on : .off
        launchOnStartupItem.state = config.launchOnStartup ? .on : .off

        if sleeping, let until = config.sleepUntilDate() {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            sleepMenu.title = "Sleep (until \(formatter.string(from: until)))"
        } else {
            sleepMenu.title = "Sleep"
        }

        updateSleepItem(sleep1hItem, hours: 1, sleeping: sleeping)
        updateSleepItem(sleep2hItem, hours: 2, sleeping: sleeping)
        updateSleepItem(sleep4hItem, hours: 4, sleeping: sleeping)
        updateSleepItem(sleep8hItem, hours: 8, sleeping: sleeping)

        if active {
            statusItem.button?.toolTip = config.linkProxyEnabled
                ? "Sanity - active (clipboard + links)"
                : "Sanity - active"
        } else if sleeping, let until = config.sleepUntilDate() {
            let formatter = DateFormatter()
            formatter.timeStyle = .short
            statusItem.button?.toolTip = "Sanity - sleeping until \(formatter.string(from: until))"
        } else {
            statusItem.button?.toolTip = "Sanity - disabled"
        }
    }

    private func updateSleepItem(_ item: NSMenuItem, hours: Int, sleeping: Bool) {
        guard sleeping, let sleepUntil = config.sleepUntilDate() else {
            item.state = .off
            return
        }
        let target = Date().addingTimeInterval(Double(hours) * 3600)
        item.state = abs(sleepUntil.timeIntervalSince(target)) < 300 ? .on : .off
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
