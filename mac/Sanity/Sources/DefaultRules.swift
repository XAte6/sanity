import Foundation

struct RegexRulesCatalog: Codable {
    var version: Int
    var rules: [UrlRule]
}

enum DefaultRules {
    static let fileName = "regex-rules.json"

    static func loadLocal() throws -> RegexRulesCatalog {
        if let url = Bundle.main.url(forResource: "regex-rules", withExtension: "json"),
           let data = try? Data(contentsOf: url) {
            return try decode(data)
        }

        let besideApp = Bundle.main.bundleURL
            .deletingLastPathComponent()
            .appendingPathComponent(fileName)
        if FileManager.default.fileExists(atPath: besideApp.path) {
            return try decode(Data(contentsOf: besideApp))
        }

        let repoDefault = Bundle.main.bundleURL
            .deletingLastPathComponent()
            .deletingLastPathComponent()
            .appendingPathComponent("defaults")
            .appendingPathComponent(fileName)
        if FileManager.default.fileExists(atPath: repoDefault.path) {
            return try decode(Data(contentsOf: repoDefault))
        }

        throw NSError(
            domain: "Sanity",
            code: 1,
            userInfo: [NSLocalizedDescriptionKey: "Default regex rules file not found."]
        )
    }

    static func fetchRemote() throws -> RegexRulesCatalog {
        let url = URL(string: AppLinks.regexRulesRaw)!
        var request = URLRequest(url: url, timeoutInterval: 20)
        request.setValue("Sanity", forHTTPHeaderField: "User-Agent")
        let semaphore = DispatchSemaphore(value: 0)
        var resultData: Data?
        var resultError: Error?

        URLSession.shared.dataTask(with: request) { data, _, error in
            resultData = data
            resultError = error
            semaphore.signal()
        }.resume()

        semaphore.wait()
        if let resultError {
            throw resultError
        }
        guard let resultData else {
            throw NSError(domain: "Sanity", code: 2, userInfo: [NSLocalizedDescriptionKey: "Empty response."])
        }
        return try decode(resultData)
    }

    static func loadForReset() throws -> RegexRulesCatalog {
        do {
            return try fetchRemote()
        } catch {
            return try loadLocal()
        }
    }

    private static func decode(_ data: Data) throws -> RegexRulesCatalog {
        let catalog = try JSONDecoder().decode(RegexRulesCatalog.self, from: data)
        guard !catalog.rules.isEmpty else {
            throw NSError(domain: "Sanity", code: 3, userInfo: [NSLocalizedDescriptionKey: "Regex rules catalog is empty."])
        }
        return catalog
    }
}
