import AppKit
import Foundation

enum AppLinks {
    static let github = "https://github.com/XAte6/sanity"
    static let support = "https://github.com/XAte6/sanity/issues"
    static let tip = "https://paypal.me/XAte6"

    static func open(_ urlString: String) {
        guard let url = URL(string: urlString) else { return }
        NSWorkspace.shared.open(url)
    }
}
