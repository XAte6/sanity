import Foundation

struct UsageMetrics: Codable {
    var linksCleaned: Int
    var domains: [String: Int]

    var domainCount: Int { domains.count }

    static var metricsURL: URL {
        AppConfig.configURL.deletingLastPathComponent().appendingPathComponent("metrics.json")
    }

    static func load() -> UsageMetrics {
        let url = metricsURL
        guard FileManager.default.fileExists(atPath: url.path),
              let data = try? Data(contentsOf: url),
              let metrics = try? JSONDecoder().decode(UsageMetrics.self, from: data) else {
            return empty()
        }
        return metrics
    }

    static func recordClean(url urlString: String) {
        guard let host = extractHost(urlString) else { return }

        for _ in 0..<5 {
            do {
                var metrics = load()
                metrics.linksCleaned += 1
                metrics.domains[host, default: 0] += 1
                try metrics.save()
                return
            } catch {
                Thread.sleep(forTimeInterval: 0.04)
            }
        }
    }

    func save() throws {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        let data = try encoder.encode(self)
        let url = Self.metricsURL
        let tempURL = url.appendingPathExtension("tmp")
        try data.write(to: tempURL, options: .atomic)
        if FileManager.default.fileExists(atPath: url.path) {
            _ = try FileManager.default.replaceItemAt(url, withItemAt: tempURL)
        } else {
            try FileManager.default.moveItem(at: tempURL, to: url)
        }
    }

    func summaryText() -> String {
        let linkWord = linksCleaned == 1 ? "link" : "links"
        let domainWord = domainCount == 1 ? "domain" : "domains"
        return "\(linksCleaned) \(linkWord) cleaned across \(domainCount) \(domainWord)"
    }

    func domainsByCount() -> [(host: String, count: Int)] {
        domains
            .map { (host: $0.key, count: $0.value) }
            .sorted {
                if $0.count != $1.count {
                    return $0.count > $1.count
                }
                return $0.host.localizedCaseInsensitiveCompare($1.host) == .orderedAscending
            }
    }

    private static func empty() -> UsageMetrics {
        UsageMetrics(linksCleaned: 0, domains: [:])
    }

    private static func extractHost(_ urlString: String) -> String? {
        guard let url = URL(string: urlString.trimmingCharacters(in: .whitespacesAndNewlines)),
              let host = url.host, !host.isEmpty else {
            return nil
        }
        return host.lowercased()
    }
}
