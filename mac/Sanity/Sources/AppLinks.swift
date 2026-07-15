import AppKit
import Foundation

enum AppLinks {
    static let github = "https://github.com/XAte6/sanity"
    static let support = "https://github.com/XAte6/sanity/issues"
    static let tip = "https://paypal.me/XAte6"
    static let regexRulesRaw =
        "https://raw.githubusercontent.com/XAte6/sanity/main/defaults/regex-rules.json"
    static let releaseAsset =
        "https://github.com/XAte6/sanity/raw/main/releases/Sanity-mac-arm.zip"
    static let releaseCommitsApi =
        "https://api.github.com/repos/XAte6/sanity/commits?path=releases/Sanity-mac-arm.zip&per_page=1"

    static func open(_ urlString: String) {
        guard let url = URL(string: urlString) else { return }
        NSWorkspace.shared.open(url)
    }
}
