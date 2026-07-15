import AppKit
import Foundation

enum UpdateChecker {
    private static let checkInterval: TimeInterval = 7 * 24 * 60 * 60
    private static var running = false
    private static let lock = NSLock()

    static func runAsync(config: AppConfig, apply: @escaping (AppConfig) -> Void) {
        guard beginIfDue(config) else { return }

        DispatchQueue.global(qos: .utility).async {
            let remoteRules = (try? DefaultRules.fetchRemote())
            let remoteReleaseDate = (try? fetchReleaseFileDate())

            DispatchQueue.main.async {
                var updated = config
                prompt(config: &updated, remoteRules: remoteRules, remoteReleaseDate: remoteReleaseDate)
                updated.lastUpdateCheck = AppConfig.encodeSleepDate(Date())
                updated.save()
                endRunning()
                apply(updated)
            }
        }
    }

    /// Blocks while fetching and prompting — for short-lived --open processes.
    static func runSync(config: AppConfig) {
        guard beginIfDue(config) else { return }
        defer { endRunning() }

        let remoteRules = (try? DefaultRules.fetchRemote())
        let remoteReleaseDate = (try? fetchReleaseFileDate())

        var updated = config
        prompt(config: &updated, remoteRules: remoteRules, remoteReleaseDate: remoteReleaseDate)
        updated.lastUpdateCheck = AppConfig.encodeSleepDate(Date())
        updated.save()
    }

    private static func beginIfDue(_ config: AppConfig) -> Bool {
        guard config.updatesEnabled, isDue(config) else { return false }
        lock.lock()
        defer { lock.unlock() }
        if running { return false }
        running = true
        return true
    }

    private static func endRunning() {
        lock.lock()
        running = false
        lock.unlock()
    }

    private static func isDue(_ config: AppConfig) -> Bool {
        guard let raw = config.lastUpdateCheck, let last = parseDate(raw) else {
            return true
        }
        return Date().timeIntervalSince(last) >= checkInterval
    }

    private static func parseDate(_ value: String) -> Date? {
        let iso = ISO8601DateFormatter()
        iso.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = iso.date(from: value) { return date }
        iso.formatOptions = [.withInternetDateTime]
        return iso.date(from: value)
    }

    private static func prompt(
        config: inout AppConfig,
        remoteRules: RegexRulesCatalog?,
        remoteReleaseDate: Date?
    ) {
        if let remoteRules, remoteRules.version > config.rulesVersion {
            let alert = NSAlert()
            alert.messageText = "Regex update"
            alert.informativeText =
                "A newer regex list is available (v\(remoteRules.version)). Replace your current rules with the updated defaults?"
            alert.addButton(withTitle: "Update")
            alert.addButton(withTitle: "Not now")
            if alert.runModal() == .alertFirstButtonReturn {
                config.rules = remoteRules.rules
                config.rulesVersion = remoteRules.version
            }
        }

        let localDate = executableDate()
        if let remoteReleaseDate, let localDate, remoteReleaseDate > localDate.addingTimeInterval(60) {
            let formatter = DateFormatter()
            formatter.dateStyle = .medium
            let alert = NSAlert()
            alert.messageText = "App update"
            alert.informativeText =
                "A newer Sanity build is available on GitHub (release file dated \(formatter.string(from: remoteReleaseDate))). Open the download page?"
            alert.addButton(withTitle: "Open")
            alert.addButton(withTitle: "Not now")
            if alert.runModal() == .alertFirstButtonReturn {
                AppLinks.open(AppLinks.releaseAsset)
            }
        }
    }

    private static func executableDate() -> Date? {
        guard let path = Bundle.main.executableURL?.path else { return nil }
        let attrs = try? FileManager.default.attributesOfItem(atPath: path)
        return attrs?[.modificationDate] as? Date
    }

    private static func fetchReleaseFileDate() throws -> Date {
        let url = URL(string: AppLinks.releaseCommitsApi)!
        var request = URLRequest(url: url, timeoutInterval: 20)
        request.setValue("Sanity", forHTTPHeaderField: "User-Agent")
        request.setValue("application/vnd.github+json", forHTTPHeaderField: "Accept")

        let semaphore = DispatchSemaphore(value: 0)
        var resultData: Data?
        var resultError: Error?
        URLSession.shared.dataTask(with: request) { data, _, error in
            resultData = data
            resultError = error
            semaphore.signal()
        }.resume()
        semaphore.wait()

        if let resultError { throw resultError }
        guard let resultData else {
            throw NSError(domain: "Sanity", code: 4, userInfo: [NSLocalizedDescriptionKey: "Empty commits response."])
        }

        struct CommitItem: Decodable {
            struct Commit: Decodable {
                struct Committer: Decodable { let date: Date }
                let committer: Committer
            }
            let commit: Commit
        }

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let items = try decoder.decode([CommitItem].self, from: resultData)
        guard let date = items.first?.commit.committer.date else {
            throw NSError(domain: "Sanity", code: 5, userInfo: [NSLocalizedDescriptionKey: "No commit date."])
        }
        return date
    }
}
