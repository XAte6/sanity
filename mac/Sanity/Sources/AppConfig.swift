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
    var sleepUntil: String?
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
        sleepUntil: String? = nil,
        rules: [UrlRule] = []
    ) {
        self.enabled = enabled
        self.linkProxyEnabled = linkProxyEnabled
        self.targetBrowser = targetBrowser
        self.launchOnStartup = launchOnStartup
        self.notificationsEnabled = notificationsEnabled
        self.sleepUntil = sleepUntil
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
        sleepUntil = try container.decodeIfPresent(String.self, forKey: .sleepUntil)
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
                config.rules = createDefaultRules()
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
        AppConfig(rules: createDefaultRules())
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

    private static func createDefaultRules() -> [UrlRule] {
        var rules: [UrlRule] = []

        rules += globalRule("[?&](utm_[a-zA-Z0-9_]+=[^&]*)")
        rules += globalRule("[?&](fbclid=[^&]*)")
        rules += globalRule("[?&](gclid=[^&]*)")
        rules += globalRule("[?&](msclkid=[^&]*)")
        rules += globalRule("[?&](twclid=[^&]*)")
        rules += globalRule("[?&](dclid=[^&]*)")
        rules += globalRule("[?&](gbraid=[^&]*)")
        rules += globalRule("[?&](wbraid=[^&]*)")
        rules += globalRule("[?&](srsltid=[^&]*)")
        rules += globalRule("[?&](mc_[a-z]+=[^&]*)")

        addPlatformRules(&rules, domains: ["youtube.com", "youtu.be"],
                         params: ["si=[^&]*", "is=[^&]*", "feature=[^&]*", "pp=[^&]*", "embeds_referring_euri=[^&]*"])
        addPlatformRules(&rules, domains: ["amazon.com", "amazon.co.uk", "amazon.de", "amazon.fr", "amazon.ca", "amazon.es", "amazon.it", "amazon.co.jp", "amzn.to", "a.co"],
                         params: ["tag=[^&]*", "linkCode=[^&]*", "ref_=[^&]*", "ascsubtag=[^&]*", "creative=[^&]*", "creativeASIN=[^&]*", "linkId=[^&]*", "pd_rd_w=[^&]*", "pd_rd_wg=[^&]*", "pd_rd_r=[^&]*", "pf_rd_p=[^&]*", "pf_rd_r=[^&]*"])
        addPlatformRules(&rules, domains: ["google.com", "google.co.uk", "google.de", "google.fr", "google.ca", "google.com.au"],
                         params: ["ved=[^&]*", "usg=[^&]*", "sa=[^&]*", "source=[^&]*", "gs_lcp=[^&]*", "ei=[^&]*", "sclient=[^&]*", "oq=[^&]*", "gs_l=[^&]*", "cad=[^&]*"])
        addPlatformRules(&rules, domains: ["facebook.com", "fb.com", "fb.watch", "m.facebook.com"],
                         params: ["ref=[^&]*", "refid=[^&]*", "__tn__=[^&]*", "__cft__=[^&]*", "mibextid=[^&]*"])
        addPlatformRules(&rules, domains: ["instagram.com"], params: ["igsh=[^&]*", "ig_rid=[^&]*"])
        addPlatformRules(&rules, domains: ["tiktok.com", "vm.tiktok.com", "www.tiktok.com"],
                         params: ["_t=[^&]*", "_r=[^&]*", "share_app_id=[^&]*", "share_link_id=[^&]*", "tt_medium=[^&]*", "tt_source=[^&]*", "is_from_webapp=[^&]*"])
        addPlatformRules(&rules, domains: ["x.com", "twitter.com", "t.co", "mobile.twitter.com"],
                         params: ["s=[^&]*", "ref_src=[^&]*", "ref_url=[^&]*", "t=[^&]*"])
        addPlatformRules(&rules, domains: ["reddit.com", "old.reddit.com", "www.reddit.com", "redd.it", "new.reddit.com"],
                         params: ["share_id=[^&]*", "ref_source=[^&]*", "ref_campaign=[^&]*", "embed=[^&]*"])

        return rules
    }

    private static func globalRule(_ regex: String) -> [UrlRule] {
        [UrlRule(domain: "*", regex: regex)]
    }

    private static func addPlatformRules(_ rules: inout [UrlRule], domains: [String], params: [String]) {
        for domain in domains {
            for param in params {
                rules.append(UrlRule(domain: domain, regex: "[?&](\(param))"))
            }
        }
    }
}
