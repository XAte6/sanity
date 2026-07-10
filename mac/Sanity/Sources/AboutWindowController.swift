import AppKit

final class AboutWindowController: NSWindowController {
    init() {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 460, height: 320),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        super.init(window: window)

        window.title = "Statistics"
        window.center()
        window.isReleasedWhenClosed = false
        window.contentView = buildContentView()
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    func showWindow() {
        window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    private func buildContentView() -> NSView {
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 460, height: 320))
        let metrics = UsageMetrics.load()
        let topShare = topDomainSharePercent(metrics)

        let title = NSTextField(labelWithString: "Statistics")
        title.font = NSFont.boldSystemFont(ofSize: 18)
        title.translatesAutoresizingMaskIntoConstraints = false

        let blurb = NSTextField(wrappingLabelWithString: "Tracking parameters removed before you paste or open links.")
        blurb.textColor = .secondaryLabelColor
        blurb.translatesAutoresizingMaskIntoConstraints = false

        let cleanedValue = formatted(metrics.linksCleaned)
        let domainsValue = formatted(metrics.domainCount)
        let cleanedCaption = "Total cleaned of \(cleanedValue) "
            + (metrics.linksCleaned == 1 ? "click" : "clicks")
        let domainsCaption = metrics.domainCount == 1 ? "Domain protected" : "Domains protected"

        let cleanedStat = statColumn(
            value: cleanedValue,
            percent: topShare.map { "\($0)%" },
            caption: cleanedCaption
        )
        let domainsStat = statColumn(
            value: domainsValue,
            percent: nil,
            caption: domainsCaption
        )

        let statsRow = NSStackView(views: [cleanedStat, domainsStat])
        statsRow.orientation = .horizontal
        statsRow.distribution = .fillEqually
        statsRow.spacing = 16
        statsRow.translatesAutoresizingMaskIntoConstraints = false

        let githubButton = linkButton(title: "GitHub", url: AppLinks.github)
        let supportButton = linkButton(title: "Support", url: AppLinks.support)
        let tipButton = linkButton(title: "Tip", url: AppLinks.tip)

        let linkStack = NSStackView(views: [githubButton, supportButton, tipButton])
        linkStack.orientation = .horizontal
        linkStack.spacing = 12
        linkStack.translatesAutoresizingMaskIntoConstraints = false

        content.addSubview(title)
        content.addSubview(blurb)
        content.addSubview(statsRow)
        content.addSubview(linkStack)

        NSLayoutConstraint.activate([
            title.topAnchor.constraint(equalTo: content.topAnchor, constant: 20),
            title.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),

            blurb.topAnchor.constraint(equalTo: title.bottomAnchor, constant: 8),
            blurb.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),
            blurb.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -20),

            statsRow.topAnchor.constraint(equalTo: blurb.bottomAnchor, constant: 20),
            statsRow.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),
            statsRow.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -20),

            linkStack.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),
            linkStack.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -20)
        ])

        return content
    }

    private func statColumn(value: String, percent: String?, caption: String) -> NSView {
        let column = NSView()
        column.translatesAutoresizingMaskIntoConstraints = false

        let valueField = NSTextField(labelWithString: value)
        valueField.font = fittedFont(for: value, maxWidth: percent == nil ? 190 : 146)
        valueField.translatesAutoresizingMaskIntoConstraints = false

        let captionField = NSTextField(wrappingLabelWithString: caption)
        captionField.font = NSFont.systemFont(ofSize: 11)
        captionField.textColor = .secondaryLabelColor
        captionField.translatesAutoresizingMaskIntoConstraints = false

        column.addSubview(valueField)
        column.addSubview(captionField)

        NSLayoutConstraint.activate([
            valueField.topAnchor.constraint(equalTo: column.topAnchor),
            valueField.leadingAnchor.constraint(equalTo: column.leadingAnchor),
            valueField.trailingAnchor.constraint(lessThanOrEqualTo: column.trailingAnchor),

            captionField.topAnchor.constraint(equalTo: valueField.bottomAnchor, constant: 6),
            captionField.leadingAnchor.constraint(equalTo: column.leadingAnchor),
            captionField.trailingAnchor.constraint(equalTo: column.trailingAnchor),
            captionField.bottomAnchor.constraint(equalTo: column.bottomAnchor)
        ])

        if let percent {
            let percentField = NSTextField(labelWithString: percent)
            percentField.font = NSFont.boldSystemFont(ofSize: 11)
            percentField.textColor = NSColor(calibratedRed: 0.24, green: 0.70, blue: 0.44, alpha: 1)
            percentField.translatesAutoresizingMaskIntoConstraints = false
            column.addSubview(percentField)

            NSLayoutConstraint.activate([
                percentField.firstBaselineAnchor.constraint(equalTo: valueField.firstBaselineAnchor),
                percentField.leadingAnchor.constraint(equalTo: valueField.trailingAnchor, constant: 6),
                percentField.trailingAnchor.constraint(lessThanOrEqualTo: column.trailingAnchor)
            ])
        }

        return column
    }

    private func fittedFont(for text: String, maxWidth: CGFloat, maxSize: CGFloat = 26, minSize: CGFloat = 11) -> NSFont {
        var size = maxSize
        while size > minSize {
            let font = NSFont.boldSystemFont(ofSize: size)
            let width = (text as NSString).size(withAttributes: [.font: font]).width
            if width <= maxWidth {
                return font
            }
            size -= 1
        }
        return NSFont.boldSystemFont(ofSize: minSize)
    }

    private func formatted(_ value: Int) -> String {
        let formatter = NumberFormatter()
        formatter.numberStyle = .decimal
        return formatter.string(from: NSNumber(value: value)) ?? "\(value)"
    }

    private func topDomainSharePercent(_ metrics: UsageMetrics) -> Int? {
        guard metrics.linksCleaned > 0,
              let top = metrics.domains.values.max() else {
            return nil
        }
        return Int((100.0 * Double(top) / Double(metrics.linksCleaned)).rounded())
    }

    private func linkButton(title: String, url: String) -> NSButton {
        let button = NSButton(title: title, target: self, action: #selector(openLink(_:)))
        button.bezelStyle = .recessed
        button.isBordered = false
        button.contentTintColor = .linkColor
        button.identifier = NSUserInterfaceItemIdentifier(url)
        return button
    }

    @objc private func openLink(_ sender: NSButton) {
        AppLinks.open(sender.identifier?.rawValue ?? "")
    }
}
