import AppKit

final class ConfigWindowController: NSWindowController, NSWindowDelegate, NSTableViewDataSource, NSTableViewDelegate {
    private var config: AppConfig
    private var rules: [UrlRule]
    private let onSave: (AppConfig) -> Void

    private let tableView = NSTableView()
    private let domainColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("domain"))
    private let regexColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("regex"))
    private let actionsColumn = NSTableColumn(identifier: NSUserInterfaceItemIdentifier("actions"))

    private var domainFilterField: NSTextField!
    private var regexFilterField: NSTextField!
    private var countLabel: NSTextField!
    private var filteredIndexes: [Int] = []
    private var editSheet: RuleEditPanelController?

    private let domainColWidth: CGFloat = 180
    private let actionsColWidth: CGFloat = 78

    init(config: AppConfig, onSave: @escaping (AppConfig) -> Void) {
        self.config = config
        self.rules = config.rules.map { UrlRule(domain: $0.domain, regex: $0.regex) }
        self.onSave = onSave

        let window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 720, height: 540),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        super.init(window: window)

        window.title = "Regex Rules"
        window.center()
        window.delegate = self
        window.isReleasedWhenClosed = false
        window.contentView = buildContentView()
        refreshList()
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    func showWindow() {
        window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    func windowWillClose(_ notification: Notification) {
        persist()
    }

    private func buildContentView() -> NSView {
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 720, height: 540))

        let header = UiChrome.makeHeader(
            title: "Regex Rules",
            subtitle: "Domain host to match (* = all). Regex pattern removed from matching URLs."
        )

        let domainHeader = UiChrome.makeColumnHeader("DOMAIN")
        let regexHeader = UiChrome.makeColumnHeader("REGEX TO REMOVE")
        let actionsHeader = UiChrome.makeColumnHeader("ACTIONS")

        let headerRow = NSStackView(views: [domainHeader, regexHeader, NSView(), actionsHeader])
        headerRow.orientation = .horizontal
        headerRow.alignment = .centerY
        headerRow.spacing = 8
        headerRow.translatesAutoresizingMaskIntoConstraints = false
        domainHeader.widthAnchor.constraint(equalToConstant: domainColWidth).isActive = true

        domainFilterField = UiChrome.makeFilterField(placeholder: "Filter domain…")
        regexFilterField = UiChrome.makeFilterField(placeholder: "Filter regex…")
        domainFilterField.delegate = self
        regexFilterField.delegate = self

        let addButton = UiChrome.makeIconButton(
            image: UiChrome.drawPlusIcon(size: 18),
            tooltip: "Add rule",
            target: self,
            action: #selector(addRule)
        )

        let filterRow = NSStackView(views: [domainFilterField, regexFilterField, addButton])
        filterRow.orientation = .horizontal
        filterRow.alignment = .centerY
        filterRow.spacing = 8
        filterRow.translatesAutoresizingMaskIntoConstraints = false
        domainFilterField.widthAnchor.constraint(equalToConstant: domainColWidth).isActive = true

        domainColumn.title = ""
        domainColumn.width = domainColWidth
        domainColumn.minWidth = domainColWidth
        domainColumn.maxWidth = domainColWidth
        regexColumn.title = ""
        regexColumn.width = 400
        actionsColumn.title = ""
        actionsColumn.width = actionsColWidth
        actionsColumn.minWidth = actionsColWidth
        actionsColumn.maxWidth = actionsColWidth

        tableView.addTableColumn(domainColumn)
        tableView.addTableColumn(regexColumn)
        tableView.addTableColumn(actionsColumn)
        tableView.headerView = nil
        tableView.usesAlternatingRowBackgroundColors = true
        tableView.selectionHighlightStyle = .none
        tableView.rowHeight = 28
        tableView.columnAutoresizingStyle = .lastColumnOnlyAutoresizingStyle
        tableView.dataSource = self
        tableView.delegate = self
        tableView.doubleAction = #selector(editSelected)
        tableView.target = self
        tableView.translatesAutoresizingMaskIntoConstraints = false

        let scrollView = NSScrollView()
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.hasVerticalScroller = true
        scrollView.borderType = .bezelBorder
        scrollView.documentView = tableView
        scrollView.setContentCompressionResistancePriority(.defaultLow, for: .vertical)

        countLabel = NSTextField(labelWithString: "")
        countLabel.font = NSFont.systemFont(ofSize: 11)
        countLabel.textColor = .secondaryLabelColor
        countLabel.translatesAutoresizingMaskIntoConstraints = false

        let links = UiChrome.makeLinksPanel()
        let resetButton = NSButton(title: "Reset to defaults", target: self, action: #selector(resetToDefaults))
        resetButton.bezelStyle = .rounded
        resetButton.translatesAutoresizingMaskIntoConstraints = false

        let footer = NSStackView(views: [resetButton, NSView(), links])
        footer.orientation = .horizontal
        footer.alignment = .centerY
        footer.spacing = 12
        footer.translatesAutoresizingMaskIntoConstraints = false

        content.addSubview(header)
        content.addSubview(headerRow)
        content.addSubview(filterRow)
        content.addSubview(scrollView)
        content.addSubview(countLabel)
        content.addSubview(footer)

        NSLayoutConstraint.activate([
            header.topAnchor.constraint(equalTo: content.topAnchor, constant: 16),
            header.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            header.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),

            headerRow.topAnchor.constraint(equalTo: header.bottomAnchor, constant: 14),
            headerRow.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            headerRow.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),

            filterRow.topAnchor.constraint(equalTo: headerRow.bottomAnchor, constant: 6),
            filterRow.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            filterRow.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),
            filterRow.heightAnchor.constraint(equalToConstant: 28),

            scrollView.topAnchor.constraint(equalTo: filterRow.bottomAnchor, constant: 10),
            scrollView.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            scrollView.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),
            scrollView.bottomAnchor.constraint(equalTo: countLabel.topAnchor, constant: -8),

            countLabel.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            countLabel.bottomAnchor.constraint(equalTo: footer.topAnchor, constant: -12),

            footer.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 22),
            footer.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -22),
            footer.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -18)
        ])

        return content
    }

    @objc private func resetToDefaults() {
        let alert = NSAlert()
        alert.messageText = "Reset regex rules"
        alert.informativeText = "Replace all current rules with the default list from GitHub?"
        alert.addButton(withTitle: "Reset")
        alert.addButton(withTitle: "Cancel")
        guard alert.runModal() == .alertFirstButtonReturn else { return }

        do {
            let catalog = try DefaultRules.loadForReset()
            rules = catalog.rules.map { UrlRule(domain: $0.domain, regex: $0.regex) }
            config.rulesVersion = catalog.version
            refreshList()
        } catch {
            let failure = NSAlert(error: error)
            failure.messageText = "Could not load default rules"
            failure.runModal()
        }
    }

    private func refreshList() {
        let domainFilter = domainFilterField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        let regexFilter = regexFilterField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()

        filteredIndexes = []
        for (index, rule) in rules.enumerated() {
            if !domainFilter.isEmpty, !rule.domain.lowercased().contains(domainFilter) {
                continue
            }
            if !regexFilter.isEmpty, !rule.regex.lowercased().contains(regexFilter) {
                continue
            }
            filteredIndexes.append(index)
        }

        tableView.reloadData()
        let shown = filteredIndexes.count
        countLabel.stringValue = shown == rules.count
            ? "\(rules.count) rule\(rules.count == 1 ? "" : "s")"
            : "Showing \(shown) of \(rules.count) rules"
    }

    @objc private func addRule() {
        presentEditor(domain: "*", regex: "", title: "Add rule") { [weak self] domain, regex in
            guard let self else { return }
            self.rules.append(UrlRule(domain: domain, regex: regex))
            self.refreshList()
        }
    }

    @objc private func editSelected() {
        let row = tableView.clickedRow >= 0 ? tableView.clickedRow : tableView.selectedRow
        guard row >= 0, row < filteredIndexes.count else { return }
        editRule(atVisible: row)
    }

    private func editRule(atVisible visibleIndex: Int) {
        guard visibleIndex >= 0, visibleIndex < filteredIndexes.count else { return }
        let ruleIndex = filteredIndexes[visibleIndex]
        let rule = rules[ruleIndex]
        presentEditor(domain: rule.domain, regex: rule.regex, title: "Edit rule") { [weak self] domain, regex in
            guard let self else { return }
            self.rules[ruleIndex].domain = domain
            self.rules[ruleIndex].regex = regex
            self.refreshList()
        }
    }

    private func deleteRule(atVisible visibleIndex: Int) {
        guard visibleIndex >= 0, visibleIndex < filteredIndexes.count else { return }
        let ruleIndex = filteredIndexes[visibleIndex]
        let rule = rules[ruleIndex]
        let label = rule.domain.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? "(blank domain)" : rule.domain

        let alert = NSAlert()
        alert.messageText = "Delete rule"
        alert.informativeText = "Delete rule for \(label)?"
        alert.alertStyle = .warning
        alert.addButton(withTitle: "Delete")
        alert.addButton(withTitle: "Cancel")
        guard alert.runModal() == .alertFirstButtonReturn else { return }

        rules.remove(at: ruleIndex)
        refreshList()
    }

    private func presentEditor(domain: String, regex: String, title: String, onSave: @escaping (String, String) -> Void) {
        guard let window else { return }
        let sheet = RuleEditPanelController(domain: domain, regex: regex, title: title) { [weak self] domainValue, regexValue in
            onSave(domainValue, regexValue)
            self?.editSheet = nil
        } onCancel: { [weak self] in
            self?.editSheet = nil
        }
        editSheet = sheet
        window.beginSheet(sheet.window!) { _ in }
    }

    private func persist() {
        let cleaned = rules.compactMap { rule -> UrlRule? in
            let domain = rule.domain.trimmingCharacters(in: .whitespacesAndNewlines)
            let regex = rule.regex.trimmingCharacters(in: .whitespacesAndNewlines)
            if domain.isEmpty && regex.isEmpty { return nil }
            return UrlRule(domain: domain, regex: regex)
        }
        config.rules = cleaned
        onSave(config)
    }

    func numberOfRows(in tableView: NSTableView) -> Int {
        filteredIndexes.count
    }

    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        guard row >= 0, row < filteredIndexes.count else { return nil }
        let rule = rules[filteredIndexes[row]]
        let identifier = tableColumn?.identifier.rawValue ?? ""

        if identifier == "actions" {
            let cell = ActionCellView()
            cell.onEdit = { [weak self, weak tableView] in
                guard let self, let tableView else { return }
                let visibleRow = tableView.row(for: cell)
                self.editRule(atVisible: visibleRow)
            }
            cell.onDelete = { [weak self, weak tableView] in
                guard let self, let tableView else { return }
                let visibleRow = tableView.row(for: cell)
                self.deleteRule(atVisible: visibleRow)
            }
            return cell
        }

        let cellIdentifier = NSUserInterfaceItemIdentifier("label-\(identifier)")
        let cell = tableView.makeView(withIdentifier: cellIdentifier, owner: self) as? NSTableCellView ?? {
            let view = NSTableCellView()
            view.identifier = cellIdentifier
            let field = NSTextField(labelWithString: "")
            field.translatesAutoresizingMaskIntoConstraints = false
            field.lineBreakMode = .byTruncatingMiddle
            field.textColor = .labelColor
            field.font = NSFont.systemFont(ofSize: 12)
            view.addSubview(field)
            view.textField = field
            NSLayoutConstraint.activate([
                field.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 8),
                field.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -8),
                field.centerYAnchor.constraint(equalTo: view.centerYAnchor)
            ])
            return view
        }()

        cell.textField?.stringValue = identifier == "domain" ? rule.domain : rule.regex
        return cell
    }
}

extension ConfigWindowController: NSTextFieldDelegate {
    func controlTextDidChange(_ obj: Notification) {
        refreshList()
    }
}

private final class ActionCellView: NSView {
    var onEdit: (() -> Void)?
    var onDelete: (() -> Void)?

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        let editButton = NSButton(image: UiChrome.drawPencilIcon(size: 16), target: self, action: #selector(editTapped))
        editButton.isBordered = false
        editButton.toolTip = "Edit rule"
        editButton.translatesAutoresizingMaskIntoConstraints = false

        let deleteButton = NSButton(image: UiChrome.drawBinIcon(size: 16), target: self, action: #selector(deleteTapped))
        deleteButton.isBordered = false
        deleteButton.toolTip = "Delete rule"
        deleteButton.translatesAutoresizingMaskIntoConstraints = false

        addSubview(editButton)
        addSubview(deleteButton)

        NSLayoutConstraint.activate([
            editButton.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 10),
            editButton.centerYAnchor.constraint(equalTo: centerYAnchor),
            editButton.widthAnchor.constraint(equalToConstant: 22),
            editButton.heightAnchor.constraint(equalToConstant: 22),

            deleteButton.leadingAnchor.constraint(equalTo: editButton.trailingAnchor, constant: 8),
            deleteButton.centerYAnchor.constraint(equalTo: centerYAnchor),
            deleteButton.widthAnchor.constraint(equalToConstant: 22),
            deleteButton.heightAnchor.constraint(equalToConstant: 22)
        ])
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    @objc private func editTapped() { onEdit?() }
    @objc private func deleteTapped() { onDelete?() }
}

final class RuleEditPanelController: NSWindowController {
    private let domainField = NSTextField()
    private let regexField = NSTextField()
    private let testUrlField = NSTextField()
    private let resultLabel = NSTextField(wrappingLabelWithString: "Paste a URL above to preview this rule.")
    private let onSave: (String, String) -> Void
    private let onCancel: () -> Void

    init(domain: String, regex: String, title: String, onSave: @escaping (String, String) -> Void, onCancel: @escaping () -> Void) {
        self.onSave = onSave
        self.onCancel = onCancel

        let panel = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 520, height: 340),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        super.init(window: panel)

        panel.title = title
        panel.isReleasedWhenClosed = false
        panel.contentView = buildContent(domain: domain, regex: regex)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    private func buildContent(domain: String, regex: String) -> NSView {
        let content = NSView(frame: NSRect(x: 0, y: 0, width: 520, height: 340))

        let domainLabel = sectionLabel("Domain")
        configureField(domainField, value: domain, mono: false)
        domainField.placeholderString = "*"

        let regexLabel = sectionLabel("Regex to remove")
        configureField(regexField, value: regex, mono: true)

        let testLabel = sectionLabel("Paste a URL to test")
        configureField(testUrlField, value: "", mono: false)
        testUrlField.placeholderString = "https://example.com/?utm_source=…"

        let resultPanel = UiChrome.borderedPanel()
        let resultTitle = NSTextField(labelWithString: "RESULT")
        resultTitle.font = NSFont.boldSystemFont(ofSize: 10)
        resultTitle.textColor = .secondaryLabelColor
        resultTitle.translatesAutoresizingMaskIntoConstraints = false

        resultLabel.font = NSFont.systemFont(ofSize: 12)
        resultLabel.textColor = .secondaryLabelColor
        resultLabel.translatesAutoresizingMaskIntoConstraints = false

        resultPanel.addSubview(resultTitle)
        resultPanel.addSubview(resultLabel)

        let saveButton = NSButton(title: "Save", target: self, action: #selector(save))
        saveButton.keyEquivalent = "\r"
        let cancelButton = NSButton(title: "Cancel", target: self, action: #selector(cancel))
        cancelButton.keyEquivalent = "\u{1b}"

        let buttons = NSStackView(views: [NSView(), cancelButton, saveButton])
        buttons.orientation = .horizontal
        buttons.spacing = 8
        buttons.translatesAutoresizingMaskIntoConstraints = false

        for view in [domainLabel, domainField, regexLabel, regexField, testLabel, testUrlField, resultPanel, buttons] {
            content.addSubview(view)
        }

        NSLayoutConstraint.activate([
            domainLabel.topAnchor.constraint(equalTo: content.topAnchor, constant: 18),
            domainLabel.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),

            domainField.topAnchor.constraint(equalTo: domainLabel.bottomAnchor, constant: 4),
            domainField.leadingAnchor.constraint(equalTo: content.leadingAnchor, constant: 20),
            domainField.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -20),
            domainField.heightAnchor.constraint(equalToConstant: 28),

            regexLabel.topAnchor.constraint(equalTo: domainField.bottomAnchor, constant: 14),
            regexLabel.leadingAnchor.constraint(equalTo: domainField.leadingAnchor),

            regexField.topAnchor.constraint(equalTo: regexLabel.bottomAnchor, constant: 4),
            regexField.leadingAnchor.constraint(equalTo: domainField.leadingAnchor),
            regexField.trailingAnchor.constraint(equalTo: domainField.trailingAnchor),
            regexField.heightAnchor.constraint(equalToConstant: 28),

            testLabel.topAnchor.constraint(equalTo: regexField.bottomAnchor, constant: 14),
            testLabel.leadingAnchor.constraint(equalTo: domainField.leadingAnchor),

            testUrlField.topAnchor.constraint(equalTo: testLabel.bottomAnchor, constant: 4),
            testUrlField.leadingAnchor.constraint(equalTo: domainField.leadingAnchor),
            testUrlField.trailingAnchor.constraint(equalTo: domainField.trailingAnchor),
            testUrlField.heightAnchor.constraint(equalToConstant: 28),

            resultPanel.topAnchor.constraint(equalTo: testUrlField.bottomAnchor, constant: 12),
            resultPanel.leadingAnchor.constraint(equalTo: domainField.leadingAnchor),
            resultPanel.trailingAnchor.constraint(equalTo: domainField.trailingAnchor),
            resultPanel.heightAnchor.constraint(equalToConstant: 72),

            resultTitle.topAnchor.constraint(equalTo: resultPanel.topAnchor, constant: 8),
            resultTitle.leadingAnchor.constraint(equalTo: resultPanel.leadingAnchor, constant: 10),

            resultLabel.topAnchor.constraint(equalTo: resultTitle.bottomAnchor, constant: 4),
            resultLabel.leadingAnchor.constraint(equalTo: resultPanel.leadingAnchor, constant: 10),
            resultLabel.trailingAnchor.constraint(equalTo: resultPanel.trailingAnchor, constant: -10),
            resultLabel.bottomAnchor.constraint(lessThanOrEqualTo: resultPanel.bottomAnchor, constant: -8),

            buttons.trailingAnchor.constraint(equalTo: content.trailingAnchor, constant: -20),
            buttons.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -18),
            buttons.heightAnchor.constraint(equalToConstant: 30)
        ])

        domainField.delegate = self
        regexField.delegate = self
        testUrlField.delegate = self
        return content
    }

    private func sectionLabel(_ text: String) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = NSFont.boldSystemFont(ofSize: 11)
        field.textColor = .secondaryLabelColor
        field.translatesAutoresizingMaskIntoConstraints = false
        return field
    }

    private func configureField(_ field: NSTextField, value: String, mono: Bool) {
        field.stringValue = value
        field.font = mono
            ? NSFont.monospacedSystemFont(ofSize: 12, weight: .regular)
            : NSFont.systemFont(ofSize: 13)
        field.isBezeled = true
        field.bezelStyle = .roundedBezel
        field.isEditable = true
        field.isSelectable = true
        field.translatesAutoresizingMaskIntoConstraints = false
    }

    private func updatePreview() {
        let url = testUrlField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
        if url.isEmpty {
            resultLabel.textColor = .secondaryLabelColor
            resultLabel.stringValue = "Paste a URL above to preview this rule."
            return
        }

        let rules = [UrlRule(
            domain: domainField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines),
            regex: regexField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
        )]

        if let cleaned = UrlCleaner.tryClean(url, rules: rules) {
            resultLabel.textColor = .systemGreen
            resultLabel.stringValue = cleaned
        } else if !url.lowercased().hasPrefix("http://"), !url.lowercased().hasPrefix("https://") {
            resultLabel.textColor = .systemRed
            resultLabel.stringValue = "Enter a full http:// or https:// URL."
        } else {
            resultLabel.textColor = .secondaryLabelColor
            resultLabel.stringValue = "No change — domain or regex did not match."
        }
    }

    @objc private func save() {
        let domain = domainField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
        let regex = regexField.stringValue.trimmingCharacters(in: .whitespacesAndNewlines)
        window?.sheetParent?.endSheet(window!)
        onSave(domain, regex)
    }

    @objc private func cancel() {
        window?.sheetParent?.endSheet(window!)
        onCancel()
    }
}

extension RuleEditPanelController: NSTextFieldDelegate {
    func controlTextDidChange(_ obj: Notification) {
        updatePreview()
    }
}
