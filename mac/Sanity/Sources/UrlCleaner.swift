import Foundation

enum UrlCleaner {
    static func tryClean(_ text: String, rules: [UrlRule]) -> String? {
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty,
              trimmed.lowercased().hasPrefix("http://") || trimmed.lowercased().hasPrefix("https://"),
              let url = URL(string: trimmed),
              let scheme = url.scheme?.lowercased(),
              scheme == "http" || scheme == "https",
              let host = url.host else {
            return nil
        }

        var result = trimmed
        for rule in rules where domainMatches(host: host, domain: rule.domain) {
            guard let regex = try? NSRegularExpression(pattern: rule.regex, options: [.caseInsensitive]) else {
                continue
            }
            let range = NSRange(result.startIndex..<result.endIndex, in: result)
            result = regex.stringByReplacingMatches(in: result, options: [], range: range, withTemplate: "")
        }

        result = tidyUrl(result)
        return result == trimmed ? nil : result
    }

    private static func domainMatches(host: String, domain: String) -> Bool {
        if domain.isEmpty || domain == "*" {
            return true
        }
        let hostLower = host.lowercased()
        let domainLower = domain.lowercased()
        return hostLower == domainLower || hostLower.hasSuffix("." + domainLower)
    }

    private static func tidyUrl(_ url: String) -> String {
        var value = url
        let patterns = [
            "[?&]+$",
            "\\?&",
            "&&+"
        ]
        let replacements = ["", "?", "&"]
        for (pattern, replacement) in zip(patterns, replacements) {
            guard let regex = try? NSRegularExpression(pattern: pattern) else { continue }
            let range = NSRange(value.startIndex..<value.endIndex, in: value)
            value = regex.stringByReplacingMatches(in: value, options: [], range: range, withTemplate: replacement)
        }
        return value
    }
}
