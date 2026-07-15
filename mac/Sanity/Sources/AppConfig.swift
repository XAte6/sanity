import Foundation

struct UrlRule: Codable {
    var domain: String
    var regex: String
}

struct AppConfig: Codable {
    var enabled: Bool
    var linkProxyEnabled: Bool
    var targetBrowser: String
    var launchOnStartup: Bool
    var notificationsEnabled: Bool
    var updatesEnabled: Bool
    var sleepUntil: String?
    var rulesVersion: Int
    var lastUpdateCheck: String?
    var rules: [UrlRule]

    var isActive: Bool {
        guard enabled else { return false }
        if isSleeping() { return false }
        return true
    }

    var isLinkProxyActive: Bool {
        isActive
    }

    init(
        enabled: Bool = true,
        linkProxyEnabled: Bool = true,
        targetBrowser: String = "",
        launchOnStartup: Bool = false,
        notificationsEnabled: Bool = true,
        updatesEnabled: Bool = true,
        sleepUntil: String? = nil,
        rulesVersion: Int = 1,
        lastUpdateCheck: String? = nil,
        rules: [UrlRule] = []
    ) {
        self.enabled = enabled
        self.linkProxyEnabled = linkProxyEnabled
        self.targetBrowser = targetBrowser
        self.launchOnStartup = launchOnStartup
        self.notificationsEnabled = notificationsEnabled
        self.updatesEnabled = updatesEnabled
        self.sleepUntil = sleepUntil
        self.rulesVersion = rulesVersion
        self.lastUpdateCheck = lastUpdateCheck
        self.rules = rules
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        enabled = try container.decodeIfPresent(Bool.self, forKey: .enabled) ?? true
        // Kept for config compatibility; sanitisation is gated by `enabled` / `isActive`.
        linkProxyEnabled = try container.decodeIfPresent(Bool.self, forKey: .linkProxyEnabled) ?? enabled
        targetBrowser = try container.decodeIfPresent(String.self, forKey: .targetBrowser) ?? ""
        launchOnStartup = try container.decodeIfPresent(Bool.self, forKey: .launchOnStartup) ?? false
        notificationsEnabled = try container.decodeIfPresent(Bool.self, forKey: .notificationsEnabled) ?? true
        updatesEnabled = try container.decodeIfPresent(Bool.self, forKey: .updatesEnabled) ?? true
        sleepUntil = try container.decodeIfPresent(String.self, forKey: .sleepUntil)
        rulesVersion = try container.decodeIfPresent(Int.self, forKey: .rulesVersion) ?? 1
        lastUpdateCheck = try container.decodeIfPresent(String.self, forKey: .lastUpdateCheck)
        rules = try container.decodeIfPresent([UrlRule].self, forKey: .rules) ?? []
    }

    static var configURL: URL {
        Bundle.main.bundleURL
            .deletingLastPathComponent()
            .appendingPathComponent("config.json")
    }

    static func load() -> AppConfig {
        let url = configURL
        guard FileManager.default.fileExists(atPath: url.path) else {
            let defaults = createDefault()
            defaults.save()
            return defaults
        }

        do {
            let data = try Data(contentsOf: url)
            var config = try JSONDecoder().decode(AppConfig.self, from: data)
            if config.rules.isEmpty {
                let catalog = try DefaultRules.loadLocal()
                config.rules = catalog.rules
                config.rulesVersion = catalog.version
            }
            return config
        } catch {
            let defaults = createDefault()
            defaults.save()
            return defaults
        }
    }

    func save() {
        do {
            let data = try JSONEncoder().encode(self)
            try data.write(to: Self.configURL, options: .atomic)
        } catch {
            fputs("Failed to save config: \(error)\n", stderr)
        }
    }

    func sleepUntilDate() -> Date? {
        guard let sleepUntil else { return nil }
        if let date = Self.isoFormatter.date(from: sleepUntil) {
            return date
        }
        return Self.parseMicrosoftJsonDate(sleepUntil)
    }

    func isSleeping() -> Bool {
        guard let date = sleepUntilDate() else { return false }
        return date > Date()
    }

    static func encodeSleepDate(_ date: Date) -> String {
        isoFormatter.string(from: date)
    }

    static func createDefault() -> AppConfig {
        do {
            let catalog = try DefaultRules.loadLocal()
            return AppConfig(rulesVersion: catalog.version, rules: catalog.rules)
        } catch {
            fputs("Failed to load default rules: \(error)\n", stderr)
            return AppConfig()
        }
    }

    private static let isoFormatter: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter
    }()

    private static func parseMicrosoftJsonDate(_ value: String) -> Date? {
        guard value.hasPrefix("/Date("), value.hasSuffix(")/") else { return nil }
        let inner = value.dropFirst(6).dropLast(2)
        let digits = inner.prefix { $0.isNumber || $0 == "-" }
        guard let millis = Double(digits) else { return nil }
        return Date(timeIntervalSince1970: millis / 1000.0)
    }
}
