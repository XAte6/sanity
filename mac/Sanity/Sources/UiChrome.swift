import AppKit

enum UiChrome {
    static func makeHeader(title: String, subtitle: String) -> NSView {
        let panel = NSView()
        panel.translatesAutoresizingMaskIntoConstraints = false

        let iconView = NSImageView(image: AppIcon.create(size: 52))
        iconView.translatesAutoresizingMaskIntoConstraints = false
        iconView.imageScaling = .scaleProportionallyUpOrDown

        let titleField = NSTextField(labelWithString: title)
        titleField.font = NSFont.boldSystemFont(ofSize: 18)
        titleField.textColor = .labelColor
        titleField.translatesAutoresizingMaskIntoConstraints = false

        let subtitleField = NSTextField(wrappingLabelWithString: subtitle)
        subtitleField.font = NSFont.systemFont(ofSize: 12)
        subtitleField.textColor = .secondaryLabelColor
        subtitleField.translatesAutoresizingMaskIntoConstraints = false

        panel.addSubview(iconView)
        panel.addSubview(titleField)
        panel.addSubview(subtitleField)

        NSLayoutConstraint.activate([
            panel.heightAnchor.constraint(equalToConstant: 78),

            iconView.leadingAnchor.constraint(equalTo: panel.leadingAnchor),
            iconView.centerYAnchor.constraint(equalTo: panel.centerYAnchor),
            iconView.widthAnchor.constraint(equalToConstant: 52),
            iconView.heightAnchor.constraint(equalToConstant: 52),

            titleField.leadingAnchor.constraint(equalTo: iconView.trailingAnchor, constant: 14),
            titleField.topAnchor.constraint(equalTo: panel.topAnchor, constant: 12),
            titleField.trailingAnchor.constraint(lessThanOrEqualTo: panel.trailingAnchor),

            subtitleField.leadingAnchor.constraint(equalTo: titleField.leadingAnchor),
            subtitleField.topAnchor.constraint(equalTo: titleField.bottomAnchor, constant: 4),
            subtitleField.trailingAnchor.constraint(equalTo: panel.trailingAnchor)
        ])

        return panel
    }

    static func makeLinksPanel() -> NSView {
        let stack = NSStackView()
        stack.orientation = .horizontal
        stack.spacing = 10
        stack.alignment = .centerY
        stack.translatesAutoresizingMaskIntoConstraints = false

        stack.addArrangedSubview(LinkChipView(title: "GitHub", subtitle: "Repository", url: AppLinks.github, icon: drawGitHubIcon(size: 22)))
        stack.addArrangedSubview(LinkChipView(title: "Support", subtitle: "Issues", url: AppLinks.support, icon: drawSupportIcon(size: 22)))
        stack.addArrangedSubview(LinkChipView(title: "Tip me", subtitle: "PayPal", url: AppLinks.tip, icon: drawTipIcon(size: 22)))
        return stack
    }

    static func makeFilterField(placeholder: String) -> NSTextField {
        let field = NSTextField()
        field.placeholderString = placeholder
        field.font = NSFont.systemFont(ofSize: 13)
        field.isBezeled = true
        field.bezelStyle = .roundedBezel
        field.isEditable = true
        field.isSelectable = true
        field.focusRingType = .default
        field.translatesAutoresizingMaskIntoConstraints = false
        field.setContentCompressionResistancePriority(.required, for: .vertical)
        field.heightAnchor.constraint(equalToConstant: 28).isActive = true
        return field
    }

    static func makeColumnHeader(_ text: String) -> NSTextField {
        let field = NSTextField(labelWithString: text)
        field.font = NSFont.boldSystemFont(ofSize: 10)
        field.textColor = .secondaryLabelColor
        field.translatesAutoresizingMaskIntoConstraints = false
        return field
    }

    static func makeIconButton(image: NSImage, tooltip: String, target: AnyObject?, action: Selector) -> NSButton {
        let button = NSButton(image: image, target: target, action: action)
        button.isBordered = true
        button.bezelStyle = .regularSquare
        button.setButtonType(.momentaryPushIn)
        button.imagePosition = .imageOnly
        button.toolTip = tooltip
        button.translatesAutoresizingMaskIntoConstraints = false
        button.widthAnchor.constraint(equalToConstant: 30).isActive = true
        button.heightAnchor.constraint(equalToConstant: 28).isActive = true
        button.setContentCompressionResistancePriority(.required, for: .vertical)
        button.setContentCompressionResistancePriority(.required, for: .horizontal)
        return button
    }

    static func borderedPanel() -> NSView {
        AppearanceAwarePanel()
    }

    static func drawPencilIcon(size: CGFloat = 16) -> NSImage {
        let image = NSImage(size: NSSize(width: size, height: size), flipped: false) { _ in
            let path = NSBezierPath()
            path.move(to: NSPoint(x: size * 0.22, y: size * 0.28))
            path.line(to: NSPoint(x: size * 0.68, y: size * 0.74))
            path.line(to: NSPoint(x: size * 0.78, y: size * 0.64))
            path.line(to: NSPoint(x: size * 0.32, y: size * 0.18))
            path.move(to: NSPoint(x: size * 0.22, y: size * 0.28))
            path.line(to: NSPoint(x: size * 0.18, y: size * 0.18))
            path.line(to: NSPoint(x: size * 0.32, y: size * 0.22))
            NSColor.labelColor.setStroke()
            path.lineWidth = 1.6
            path.lineCapStyle = .round
            path.stroke()
            return true
        }
        image.isTemplate = true
        return image
    }

    static func drawBinIcon(size: CGFloat = 16) -> NSImage {
        NSImage(size: NSSize(width: size, height: size), flipped: false) { _ in
            let path = NSBezierPath()
            path.move(to: NSPoint(x: size * 0.28, y: size * 0.70))
            path.line(to: NSPoint(x: size * 0.72, y: size * 0.70))
            path.move(to: NSPoint(x: size * 0.38, y: size * 0.78))
            path.line(to: NSPoint(x: size * 0.62, y: size * 0.78))
            path.move(to: NSPoint(x: size * 0.34, y: size * 0.70))
            path.line(to: NSPoint(x: size * 0.38, y: size * 0.20))
            path.move(to: NSPoint(x: size * 0.66, y: size * 0.70))
            path.line(to: NSPoint(x: size * 0.62, y: size * 0.20))
            path.move(to: NSPoint(x: size * 0.38, y: size * 0.20))
            path.line(to: NSPoint(x: size * 0.62, y: size * 0.20))
            path.move(to: NSPoint(x: size * 0.46, y: size * 0.62))
            path.line(to: NSPoint(x: size * 0.46, y: size * 0.30))
            path.move(to: NSPoint(x: size * 0.54, y: size * 0.62))
            path.line(to: NSPoint(x: size * 0.54, y: size * 0.30))
            NSColor.systemRed.setStroke()
            path.lineWidth = 1.6
            path.lineCapStyle = .round
            path.stroke()
            return true
        }
    }

    static func drawPlusIcon(size: CGFloat = 18) -> NSImage {
        NSImage(size: NSSize(width: size, height: size), flipped: false) { _ in
            let oval = NSBezierPath(ovalIn: NSRect(x: 1, y: 1, width: size - 3, height: size - 3))
            NSColor.controlAccentColor.setFill()
            oval.fill()
            let cross = NSBezierPath()
            cross.move(to: NSPoint(x: size * 0.28, y: size * 0.5))
            cross.line(to: NSPoint(x: size * 0.72, y: size * 0.5))
            cross.move(to: NSPoint(x: size * 0.5, y: size * 0.28))
            cross.line(to: NSPoint(x: size * 0.5, y: size * 0.72))
            NSColor.white.setStroke()
            cross.lineWidth = 2
            cross.lineCapStyle = .round
            cross.stroke()
            return true
        }
    }

    static func drawGitHubIcon(size: CGFloat) -> NSImage {
        // Official Simple Icons GitHub mark path (viewBox 0 0 24 24).
        let pathData =
            "M12 .297c-6.63 0-12 5.373-12 12 0 5.303 3.438 9.8 8.205 11.385.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12"
        let image = SvgPath.render(pathData, size: size, color: .labelColor)
        image.isTemplate = true
        return image
    }

    static func drawSupportIcon(size: CGFloat) -> NSImage {
        NSImage(size: NSSize(width: size, height: size), flipped: false) { _ in
            let oval = NSBezierPath(ovalIn: NSRect(x: 1, y: 1, width: size - 3, height: size - 3))
            NSColor.systemBlue.setFill()
            oval.fill()
            let text = "!" as NSString
            let font = NSFont.boldSystemFont(ofSize: size * 0.48)
            let attrs: [NSAttributedString.Key: Any] = [
                .font: font,
                .foregroundColor: NSColor.white
            ]
            let textSize = text.size(withAttributes: attrs)
            text.draw(
                at: NSPoint(x: (size - textSize.width) / 2, y: (size - textSize.height) / 2 + 0.5),
                withAttributes: attrs
            )
            return true
        }
    }

    static func drawTipIcon(size: CGFloat) -> NSImage {
        NSImage(size: NSSize(width: size, height: size), flipped: false) { _ in
            let dark = NSColor(calibratedRed: 0, green: 48 / 255, blue: 135 / 255, alpha: 1)
            let light = NSColor(calibratedRed: 0, green: 156 / 255, blue: 222 / 255, alpha: 1)
            let font = NSFont.boldSystemFont(ofSize: size * 0.62)
            let text = "P" as NSString
            text.draw(at: NSPoint(x: size * 0.28, y: size * 0.08), withAttributes: [
                .font: font,
                .foregroundColor: light
            ])
            text.draw(at: NSPoint(x: size * 0.08, y: size * -0.02), withAttributes: [
                .font: font,
                .foregroundColor: dark
            ])
            return true
        }
    }
}

final class AppearanceAwarePanel: NSView {
    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        wantsLayer = true
        layer?.borderWidth = 1
        layer?.cornerRadius = 8
        translatesAutoresizingMaskIntoConstraints = false
        applyChrome()
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func viewDidChangeEffectiveAppearance() {
        super.viewDidChangeEffectiveAppearance()
        applyChrome()
    }

    private func applyChrome() {
        layer?.backgroundColor = NSColor.controlBackgroundColor.cgColor
        layer?.borderColor = NSColor.separatorColor.cgColor
    }
}

final class LinkChipView: NSView {
    private let url: String
    private var trackingArea: NSTrackingArea?

    init(title: String, subtitle: String, url: String, icon: NSImage) {
        self.url = url
        super.init(frame: NSRect(x: 0, y: 0, width: 144, height: 44))
        wantsLayer = true
        layer?.cornerRadius = 8
        layer?.borderWidth = 1
        applyNormalChrome()
        translatesAutoresizingMaskIntoConstraints = false
        widthAnchor.constraint(equalToConstant: 144).isActive = true
        heightAnchor.constraint(equalToConstant: 44).isActive = true

        let iconView = NSImageView(image: icon)
        iconView.translatesAutoresizingMaskIntoConstraints = false
        iconView.imageScaling = .scaleProportionallyUpOrDown
        iconView.contentTintColor = .labelColor

        let titleField = NSTextField(labelWithString: title)
        titleField.font = NSFont.boldSystemFont(ofSize: 12)
        titleField.textColor = .labelColor
        titleField.translatesAutoresizingMaskIntoConstraints = false

        let subtitleField = NSTextField(labelWithString: subtitle)
        subtitleField.font = NSFont.systemFont(ofSize: 10)
        subtitleField.textColor = .secondaryLabelColor
        subtitleField.translatesAutoresizingMaskIntoConstraints = false

        addSubview(iconView)
        addSubview(titleField)
        addSubview(subtitleField)

        NSLayoutConstraint.activate([
            iconView.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 10),
            iconView.centerYAnchor.constraint(equalTo: centerYAnchor),
            iconView.widthAnchor.constraint(equalToConstant: 22),
            iconView.heightAnchor.constraint(equalToConstant: 22),

            titleField.leadingAnchor.constraint(equalTo: iconView.trailingAnchor, constant: 8),
            titleField.topAnchor.constraint(equalTo: topAnchor, constant: 6),
            titleField.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor, constant: -8),

            subtitleField.leadingAnchor.constraint(equalTo: titleField.leadingAnchor),
            subtitleField.topAnchor.constraint(equalTo: titleField.bottomAnchor, constant: 1),
            subtitleField.trailingAnchor.constraint(lessThanOrEqualTo: trailingAnchor, constant: -8)
        ])
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func viewDidChangeEffectiveAppearance() {
        super.viewDidChangeEffectiveAppearance()
        applyNormalChrome()
    }

    private func applyNormalChrome() {
        layer?.backgroundColor = NSColor.controlBackgroundColor.cgColor
        layer?.borderColor = NSColor.separatorColor.cgColor
    }

    private func applyHoverChrome() {
        layer?.backgroundColor = NSColor.selectedContentBackgroundColor.withAlphaComponent(0.18).cgColor
        layer?.borderColor = NSColor.separatorColor.cgColor
    }

    override func updateTrackingAreas() {
        super.updateTrackingAreas()
        if let trackingArea {
            removeTrackingArea(trackingArea)
        }
        let area = NSTrackingArea(
            rect: bounds,
            options: [.mouseEnteredAndExited, .activeInKeyWindow, .inVisibleRect],
            owner: self,
            userInfo: nil
        )
        addTrackingArea(area)
        trackingArea = area
    }

    override func mouseEntered(with event: NSEvent) {
        applyHoverChrome()
    }

    override func mouseExited(with event: NSEvent) {
        applyNormalChrome()
    }

    override func mouseDown(with event: NSEvent) {
        AppLinks.open(url)
    }

    override func resetCursorRects() {
        addCursorRect(bounds, cursor: .pointingHand)
    }
}

enum SvgPath {
    static func render(_ pathData: String, size: CGFloat, color: NSColor) -> NSImage {
        NSImage(size: NSSize(width: size, height: size), flipped: true) { _ in
            let path = parse(pathData)
            let bounds = path.bounds
            guard bounds.width > 0, bounds.height > 0 else { return false }

            var transform = AffineTransform.identity
            transform.scale(size / 24)
            path.transform(using: transform)
            color.setFill()
            path.fill()
            return true
        }
    }

    private static func tokenize(_ data: String) -> [String] {
        var tokens: [String] = []
        var i = data.startIndex

        while i < data.endIndex {
            let ch = data[i]
            if ch.isWhitespace || ch == "," {
                i = data.index(after: i)
                continue
            }
            if ch.isLetter {
                tokens.append(String(ch))
                i = data.index(after: i)
                continue
            }

            let start = i
            if ch == "-" || ch == "+" {
                i = data.index(after: i)
            }
            var sawDot = false
            while i < data.endIndex {
                let c = data[i]
                if c.isNumber {
                    i = data.index(after: i)
                } else if c == "." && !sawDot {
                    sawDot = true
                    i = data.index(after: i)
                } else {
                    break
                }
            }
            if start < i {
                tokens.append(String(data[start..<i]))
            } else {
                i = data.index(after: i)
            }
        }
        return tokens
    }

    private static func parse(_ data: String) -> NSBezierPath {
        let path = NSBezierPath()
        let tokens = tokenize(data)
        var i = 0
        var command: Character = "M"
        var current = CGPoint.zero
        var start = CGPoint.zero

        func readNumber() -> CGFloat? {
            guard i < tokens.count, let value = Double(tokens[i]) else { return nil }
            i += 1
            return CGFloat(value)
        }

        func readPoint() -> CGPoint? {
            guard let x = readNumber(), let y = readNumber() else { return nil }
            return CGPoint(x: x, y: y)
        }

        while i < tokens.count {
            let token = tokens[i]
            if token.count == 1, let ch = token.first, ch.isLetter {
                command = ch
                i += 1
            }

            switch command {
            case "M", "m":
                guard var point = readPoint() else { return path }
                if command == "m" {
                    point.x += current.x
                    point.y += current.y
                }
                path.move(to: point)
                current = point
                start = point
                command = command == "M" ? "L" : "l"
            case "L", "l":
                guard var point = readPoint() else { return path }
                if command == "l" {
                    point.x += current.x
                    point.y += current.y
                }
                path.line(to: point)
                current = point
            case "C", "c":
                guard var c1 = readPoint(), var c2 = readPoint(), var end = readPoint() else { return path }
                if command == "c" {
                    c1.x += current.x; c1.y += current.y
                    c2.x += current.x; c2.y += current.y
                    end.x += current.x; end.y += current.y
                }
                path.curve(to: end, controlPoint1: c1, controlPoint2: c2)
                current = end
            case "Z", "z":
                path.close()
                current = start
            default:
                _ = readNumber()
            }
        }
        return path
    }
}
