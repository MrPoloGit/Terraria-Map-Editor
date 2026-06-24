#!/usr/bin/env bash
# Installs TEdit5 for the current user (no sudo required).
# Run from the directory containing the TEdit5 binary, or pass the install dir as $1.
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
mkdir -p "$HOME/.local/share/applications"
mkdir -p "$HOME/.local/share/mime/packages"

# Install binary and required shared libraries
cp "$BINARY" "$INSTALL_DIR/TEdit"
chmod +x "$INSTALL_DIR/TEdit"
for lib in "$SCRIPT_DIR"/*.so; do
    [ -f "$lib" ] && cp "$lib" "$INSTALL_DIR/"
done

# Install icon
cp "$SCRIPT_DIR/tedit.png" "$HOME/.local/share/icons/hicolor/128x128/apps/tedit.png"

# Install MIME type
cp "$SCRIPT_DIR/tedit-world.xml" "$HOME/.local/share/mime/packages/tedit-world.xml"
update-mime-database "$HOME/.local/share/mime" 2>/dev/null || true

# Install desktop entry with absolute binary path
sed "s|Exec=TEdit|Exec=$INSTALL_DIR/TEdit|" "$SCRIPT_DIR/tedit.desktop" \
    > "$HOME/.local/share/applications/tedit.desktop"
update-desktop-database "$HOME/.local/share/applications" 2>/dev/null || true

# Refresh icon cache
gtk-update-icon-cache -f -t "$HOME/.local/share/icons/hicolor" 2>/dev/null || true

echo "TEdit installed to $INSTALL_DIR/TEdit"
echo "Log out and back in (or run 'xdg-mime default tedit.desktop application/x-terraria-world') to apply file associations."
