import AppKit

enum AppIcon {
    static func create(size: CGFloat = 18) -> NSImage {
        let image = NSImage(size: NSSize(width: size, height: size))
        image.lockFocus()

        let background = NSColor(red: 34.0 / 255.0, green: 139.0 / 255.0, blue: 34.0 / 255.0, alpha: 1.0)
        background.setFill()
        NSBezierPath(rect: NSRect(x: 0, y: 0, width: size, height: size)).fill()

        let font = NSFont.boldSystemFont(ofSize: size * 0.62)
        let text = "S" as NSString
        let attributes: [NSAttributedString.Key: Any] = [
            .font: font,
            .foregroundColor: NSColor.white
        ]
        let textSize = text.size(withAttributes: attributes)
        let rect = NSRect(
            x: (size - textSize.width) / 2.0,
            y: (size - textSize.height) / 2.0,
            width: textSize.width,
            height: textSize.height
        )
        text.draw(in: rect, withAttributes: attributes)

        image.unlockFocus()
        image.isTemplate = false
        return image
    }
}
