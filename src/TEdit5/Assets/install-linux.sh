#!/usr/bin/env bash
# Installs TEdit for the current user (no sudo required).
# Run from the directory containing the TEdit binary, or pass the install dir as $1.
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="${1:-$HOME/.local/bin}"
BINARY="$SCRIPT_DIR/TEdit"

if [ ! -f "$BINARY" ]; then
    echo "Error: TEdit binary not found at $BINARY" >&2
    exit 1
fi

mkdir -p "$INSTALL_DIR"
mkdir -p "$HOME/.local/share/icons/hicolor/128x128/apps"
mkdir -p "$HOME/.local/share/icons/hicolor/128x128/mimetypes"
mkdir -p "$HOME/.local/share/applications"
mkdir -p "$HOME/.local/share/mime/packages"

# Install binary
cp "$BINARY" "$INSTALL_DIR/TEdit"
chmod +x "$INSTALL_DIR/TEdit"

# Copy shared libraries that live alongside the binary
for lib in "$SCRIPT_DIR"/*.so; do
    [ -f "$lib" ] && cp "$lib" "$INSTALL_DIR/"
done

# Install app icon
cp "$SCRIPT_DIR/tedit.png" "$HOME/.local/share/icons/hicolor/128x128/apps/tedit.png"

# Install MIME-type icons (Nautilus / Thunar / Dolphin use these for file type icons)
cp "$SCRIPT_DIR/tedit.png" "$HOME/.local/share/icons/hicolor/128x128/mimetypes/application-x-terraria-world.png"
cp "$SCRIPT_DIR/tedit.png" "$HOME/.local/share/icons/hicolor/128x128/mimetypes/application-x-teditsch.png"

# Install MIME type definitions (.wld and .TEditSch)
cp "$SCRIPT_DIR/tedit-world.xml" "$HOME/.local/share/mime/packages/tedit-world.xml"
update-mime-database "$HOME/.local/share/mime" 2>/dev/null || true

# Install desktop entry with absolute binary path
sed "s|Exec=TEdit|Exec=$INSTALL_DIR/TEdit|" "$SCRIPT_DIR/tedit.desktop" \
    > "$HOME/.local/share/applications/tedit.desktop"
update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

# Refresh icon cache
gtk-update-icon-cache -f -t "$HOME/.local/share/icons/hicolor" 2>/dev/null || true

echo "TEdit installed to $INSTALL_DIR/TEdit"
echo ""
echo "File associations registered for:"
echo "  .wld        (Terraria World)"
echo "  .TEditSch   (TEdit Schematic)"
echo ""
echo "If icons don't appear immediately, log out and back in, or run:"
echo "  xdg-mime default tedit.desktop application/x-terraria-world"
echo "  xdg-mime default tedit.desktop application/x-teditsch"
