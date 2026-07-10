import AppKit

final class AboutWindowController: NSWindowController, NSTableViewDataSource, NSTableViewDelegate {
    private let domainColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("domain"))
    private let cleansColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("cleans"))
    private var domainRows: [(host: String, count: Int, percent: Int)] = []

    init() {
        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 500, height: 520),
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
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 500, height: 520))

        let metrics = UsageMetrics.load()
        let topShare = topDomainSharePercent(metrics)

        let header = UiChrome.makeHeader(
            title: "Statistics",
            subtitle: "Tracking parameters removed before you paste or open links."
        )
        let statsPanel = buildStatsPanel(metrics: metrics, topShare: topShare)
        let domainSection = buildDomainSection(metrics: metrics)
        let links = UiChrome.makeLinksPanel()

        content.addSubview(header)
        content.addSubview(statsPanel)
        content.addSubview(domainSection)
        content.addSubview(links)

        NSLayoutConstraint.activate([
            header.topAnchor.constraint(equalTo: content.topAnchor, constant: 16),
            header.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            header.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),

            statsPanel.topAnchor.constraint(equalTo: header.bottomAnchor, constant: 12),
            statsPanel.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            statsPanel.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),
            statsPanel.heightAnchor.constraint(equalToConstant: 118),

            domainSection.topAnchor.constraint(equalTo: statsPanel.bottomAnchor, constant: 16),
            domainSection.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            domainSection.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),
            domainSection.bottomAnchor.constraint(equalTo: links.topAnchor, constant: -16),

            links.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            links.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -18)
        ])

        return content
    }

    private func buildStatsPanel(metrics: UsageMetrics, topShare: Int?) -> NSView {
        let panel = UiChrome.borderedPanel()

        let performance = NSTextField(labelWithString: "PERFORMANCE")
        performance.font = NSFont.boldSystemFont(ofSize: 11)
        performance.textColor = .secondaryLabelColor
        performance.translatesAutoresizingMaskIntoConstraints = false

        let cleanedValue = formatted(metrics.linksCleaned)
        let cleanedCaption = "Total cleaned of \(cleanedValue) "
            + (metrics.linksCleaned == 1 ? "click" : "clicks")
        let domainsCaption = metrics.domainCount == 1 ? "Domain protected" : "Domains protected"

        let cleanedStat = statColumn(
            value: cleanedValue,
            percent: topShare.map { "\($0)%" },
            caption: cleanedCaption
        )
        let domainsStat = statColumn(
            value: formatted(metrics.domainCount),
            percent: nil,
            caption: domainsCaption
        )

        let divider = NSView()
        divider.wantsLayer = true
        divider.layer?.backgroundColor = NSColor.separatorColor.cgColor
        divider.translatesAutoresizingMaskIntoConstraints = false

        let statsRow = NSStackView(views: [cleanedStat, domainsStat])
        statsRow.orientation = .horizontal
        statsRow.distribution = .fillEqually
        statsRow.spacing = 24
        statsRow.translatesAutoresizingMaskIntoConstraints = false

        panel.addSubview(performance)
        panel.addSubview(divider)
        panel.addSubview(statsRow)

        NSLayoutConstraint.activate([
            performance.topAnchor.constraint(equalTo: panel.topAnchor, constant: 10),
            performance.leadingAnchor.constraint(equalTo: panel.leadingAnchor, constant: 16),

            divider.centerXAnchor.constraint(equalTo: panel.centerXAnchor),
            divider.topAnchor.constraint(equalTo: panel.topAnchor, constant: 22),
            divider.bottomAnchor.constraint(equalTo: panel.bottomAnchor, constant: -22),
            divider.widthAnchor.constraint(equalToConstant: 1),

            statsRow.topAnchor.constraint(equalTo: performance.bottomAnchor, constant: 10),
            statsRow.leadingAnchor.constraint(equalTo: panel.leadingAnchor, constant: 16),
            statsRow.trailingAnchor.constraint(equalTo: panel.trailingAnchor, constant: -16),
            statsRow.bottomAnchor.constraint(lessThanOrEqualTo: panel.bottomAnchor, constant: -12)
        ])

        return panel
    }

    private func buildDomainSection(metrics: UsageMetrics) -> NSView {
        let panel = NSView()
        panel.translatesAutoresizingMaskIntoConstraints = false

        let title = NSTextField(labelWithString: "Domains")
        title.font = NSFont.boldSystemFont(ofSize: 13)
        title.textColor = .labelColor
        title.translatesAutoresizingMaskIntoConstraints = false

        let subtitle = NSTextField(labelWithString: "Share of total cleaned clicks")
        subtitle.font = NSFont.systemFont(ofSize: 11)
        subtitle.textColor = .secondaryLabelColor
        subtitle.translatesAutoresizingMaskIntoConstraints = false

        let total = max(metrics.linksCleaned, 1)
        let ranked = metrics.domainsByCount()
        if ranked.isEmpty {
            domainRows = []
        } else {
            domainRows = ranked.map { host, count in
                (host, count, Int((100.0 * Double(count) / Double(total)).rounded()))
            }
        }

        let tableView = NSTableView()
        domainColumn.title = "DOMAIN"
        domainColumn.width = 300
        cleansColumn.title = "CLEANS"
        cleansColumn.width = 136
        tableView.addTableColumn(domainColumn)
        tableView.addTableColumn(cleansColumn)
        tableView.headerView = NSTableHeaderView()
        tableView.usesAlternatingRowBackgroundColors = true
        tableView.selectionHighlightStyle = .none
        tableView.rowHeight = 26
        tableView.dataSource = self
        tableView.delegate = self
        tableView.translatesAutoresizingMaskIntoConstraints = false

        let scrollView = NSScrollView()
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.hasVerticalScroller = true
        scrollView.borderType = .bezelBorder
        scrollView.documentView = tableView

        panel.addSubview(title)
        panel.addSubview(subtitle)
        panel.addSubview(scrollView)

        NSLayoutConstraint.activate([
            title.topAnchor.constraint(equalTo: panel.topAnchor),
            title.leadingAnchor.constraint(equalTo: panel.leadingAnchor),

            subtitle.topAnchor.constraint(equalTo: title.bottomAnchor, constant: 2),
            subtitle.leadingAnchor.constraint(equalTo: panel.leadingAnchor),

            scrollView.topAnchor.constraint(equalTo: subtitle.bottomAnchor, constant: 8),
            scrollView.leadingAnchor.constraint(equalTo: panel.leadingAnchor),
            scrollView.trailingAnchor.constraint(equalTo: panel.trailingAnchor),
            scrollView.bottomAnchor.constraint(equalTo: panel.bottomAnchor)
        ])

        return panel
    }

    private func statColumn(value: String, percent: String?, caption: String) -> NSView {
        let column = NSView()
        column.translatesAutoresizingMaskIntoConstraints = false

        let valueField = NSTextField(labelWithString: value)
        valueField.font = fittedFont(for: value, maxWidth: percent == nil ? 190 : 146)
        valueField.textColor = .labelColor
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

            captionField.topAnchor.constraint(equalTo: valueField.bottomAnchor, constant: 6),
            captionField.leadingAnchor.constraint(equalTo: column.leadingAnchor),
            captionField.trailingAnchor.constraint(equalTo: column.trailingAnchor),
            captionField.bottomAnchor.constraint(equalTo: column.bottomAnchor)
        ])

        if let percent {
            let percentField = NSTextField(labelWithString: percent)
            percentField.font = NSFont.boldSystemFont(ofSize: 11)
            percentField.textColor = .secondaryLabelColor
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
        guard metrics.linksCleaned > 0 else { return nil }
        let rows = metrics.domainsByCount()
        guard let top = rows.first else { return nil }
        return Int((100.0 * Double(top.count) / Double(metrics.linksCleaned)).rounded())
    }

    func numberOfRows(in tableView: NSTableView) -> Int {
        max(domainRows.count, 1)
    }

    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        let identifier = tableColumn?.identifier.rawValue ?? ""
        let cellIdentifier = NSUserInterfaceItemIdentifier("stats-\(identifier)")
        let cell = tableView.makeView(withIdentifier: cellIdentifier, owner: self) as? NSTableCellView ?? {
            let view = NSTableCellView()
            view.identifier = cellIdentifier
            let field = NSTextField(labelWithString: "")
            field.translatesAutoresizingMaskIntoConstraints = false
            field.lineBreakMode = .byTruncatingMiddle
            view.addSubview(field)
            view.textField = field
            NSLayoutConstraint.activate([
                field.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 10),
                field.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -10),
                field.centerYAnchor.constraint(equalTo: view.centerYAnchor)
            ])
            return view
        }()

        if domainRows.isEmpty {
            cell.textField?.stringValue = identifier == "domain"
                ? "No cleans recorded yet — copy or open a tracked link"
                : "—"
            cell.textField?.textColor = .secondaryLabelColor
            cell.textField?.font = NSFont.systemFont(ofSize: 12)
            cell.textField?.alignment = identifier == "cleans" ? .right : .left
            return cell
        }

        let item = domainRows[row]
        if identifier == "domain" {
            cell.textField?.stringValue = item.host
            cell.textField?.textColor = .labelColor
            cell.textField?.font = row < 3
                ? NSFont.boldSystemFont(ofSize: 12)
                : NSFont.systemFont(ofSize: 12)
            cell.textField?.alignment = .left
        } else {
            cell.textField?.stringValue = "\(formatted(item.count))  (\(item.percent)%)"
            cell.textField?.textColor = .secondaryLabelColor
            cell.textField?.font = NSFont.systemFont(ofSize: 12)
            cell.textField?.alignment = .right
        }
        return cell
    }
}
