import AppKit

final class ConfigWindowController: NSWindowController, NSWindowDelegate, NSTableViewDataSource, NSTableViewDelegate {
    private var config: AppConfig
    private var rules: [UrlRule]
    private let onSave: (AppConfig) -> Void
    private var aboutWindow: AboutWindowController?

    private let tableView = NSTableView()
    private let domainColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("domain"))
    private let regexColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("regex"))

    init(config: AppConfig, onSave: @escaping (AppConfig) -> Void) {
        self.config = config
        self.rules = config.rules
        self.onSave = onSave

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 720, height: 480),
            styleMask: [.titled, .closable, .miniaturizable, .resizable],
            backing: .buffered,
            defer: false
        )
        super.init(window: window)

        window.title = "Sanity - URL Tracker Rules"
        window.minSize = NSSize(width: 600, height: 360)
        window.center()
        window.delegate = self
        window.isReleasedWhenClosed = false
        window.contentView = buildContentView()
        populateTable()
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
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 720, height: 480))

        let instructions = NSTextField(labelWithString: "Domain: host name to match (* = all). Regex: pattern removed from matching URLs.")
        instructions.translatesAutoresizingMaskIntoConstraints = false
        instructions.lineBreakMode = .byWordWrapping
        instructions.maximumNumberOfLines = 2

        domainColumn.title = "Domain"
        domainColumn.width = 180
        regexColumn.title = "Regex to remove"
        regexColumn.width = 480

        tableView.addTableColumn(domainColumn)
        tableView.addTableColumn(regexColumn)
        tableView.headerView = NSTableHeaderView()
        tableView.usesAlternatingRowBackgroundColors = true
        tableView.columnAutoresizingStyle = .lastColumnOnlyAutoresizingStyle
        tableView.dataSource = self
        tableView.delegate = self
        tableView.translatesAutoresizingMaskIntoConstraints = false

        let scrollView = NSScrollView()
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.hasVerticalScroller = true
        scrollView.documentView = tableView

        let addButton = NSButton(title: "Add Row", target: self, action: #selector(addRow))
        let removeButton = NSButton(title: "Remove Selected", target: self, action: #selector(removeSelected))
        let aboutButton = NSButton(title: "About", target: self, action: #selector(openAbout))
        let cancelButton = NSButton(title: "Cancel", target: self, action: #selector(cancel))
        let saveButton = NSButton(title: "Save", target: self, action: #selector(save))

        saveButton.keyEquivalent = "\r"
        cancelButton.keyEquivalent = "\u{1b}"

        let buttonRow = NSStackView(views: [addButton, removeButton, aboutButton, NSView(), cancelButton, saveButton])
        buttonRow.translatesAutoresizingMaskIntoConstraints = false
        buttonRow.orientation = .horizontal
        buttonRow.spacing = 8
        buttonRow.alignment = .centerY

        content.addSubview(instructions)
        content.addSubview(scrollView)
        content.addSubview(buttonRow)

        NSLayoutConstraint.activate([
            instructions.topAnchor.constraint(equalTo: content.topAnchor, constant: 12),
            instructions.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 12),
            instructions.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -12),

            scrollView.topAnchor.constraint(equalTo: instructions.bottomAnchor, constant: 8),
            scrollView.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 12),
            scrollView.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -12),
            scrollView.bottomAnchor.constraint(equalTo: buttonRow.topAnchor, constant: -12),

            buttonRow.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 12),
            buttonRow.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -12),
            buttonRow.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -12),
            buttonRow.heightAnchor.constraint(equalToConstant: 32)
        ])

        return content
    }

    private func populateTable() {
        tableView.reloadData()
    }

    @objc private func addRow() {
        rules.append(UrlRule(domain: "*", regex: ""))
        tableView.reloadData()
        tableView.selectRowIndexes(IndexSet(integer: rules.count - 1), byExtendingSelection: false)
    }

    @objc private func removeSelected() {
        let selected = tableView.selectedRowIndexes.sorted(by: >)
        guard !selected.isEmpty else { return }
        for index in selected where index >= 0 && index < rules.count {
            rules.remove(at: index)
        }
        tableView.reloadData()
    }

    @objc private func cancel() {
        window?.orderOut(nil)
    }

    @objc private func openAbout() {
        aboutWindow = AboutWindowController()
        aboutWindow?.showWindow()
    }

    @objc private func save() {
        syncRulesFromTable()
        config.rules = rules.filter { !$0.domain.trimmingCharacters(in: .whitespaces).isEmpty || !$0.regex.trimmingCharacters(in: .whitespaces).isEmpty }
        onSave(config)
        window?.orderOut(nil)
    }

    private func syncRulesFromTable() {
        for row in 0..<rules.count {
            let domainView = tableView.view(atColumn: 0, row: row, makeIfNecessary: true) as? NSTableCellView
            let regexView = tableView.view(atColumn: 1, row: row, makeIfNecessary: true) as? NSTableCellView
            if let domainView {
                rules[row].domain = domainView.textField?.stringValue ?? rules[row].domain
            }
            if let regexView {
                rules[row].regex = regexView.textField?.stringValue ?? rules[row].regex
            }
        }
    }

    func numberOfRows(in tableView: NSTableView) -> Int {
        rules.count
    }

    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        let identifier = tableColumn?.identifier.rawValue ?? ""
        let value = identifier == "domain" ? rules[row].domain : rules[row].regex

        let cellIdentifier = NSUserInterfaceItemIdentifier("cell-\(identifier)")
        let cell = tableView.makeView(withIdentifier: cellIdentifier, owner: self) as? NSTableCellView ?? {
            let view = NSTableCellView()
            view.identifier = cellIdentifier
            let field = NSTextField(string: "")
            field.translatesAutoresizingMaskIntoConstraints = false
            field.isBordered = false
            field.backgroundColor = .clear
            field.lineBreakMode = .byTruncatingMiddle
            view.addSubview(field)
            view.textField = field
            NSLayoutConstraint.activate([
                field.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 4),
                field.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -4),
                field.centerYAnchor.constraint(equalTo: view.centerYAnchor)
            ])
            return view
        }()

        cell.textField?.stringValue = value
        return cell
    }
}
