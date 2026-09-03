#!/usr/bin/env bash
# Build RadiusRings, optionally deploy it into a local Valheim install, and
# stage a Thunderstore-shaped package for manual upload.
#
# Building needs a Valheim install to reference assembly_valheim.dll and the
# UnityEngine modules against (see RadiusRings.csproj). Point VALHEIM_GAME_DIR
# at yours if it isn't the default macOS Steam path; the live-deploy step is
# skipped entirely if no BepInEx/plugins directory is found there -- useful on
# a machine that only has the game installed for building, or none at all
# (e.g. CI), since the package step below doesn't need a live install.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="1.0.0"

export VALHEIM_GAME_DIR="${VALHEIM_GAME_DIR:-$HOME/Library/Application Support/Steam/steamapps/common/Valheim}"
BEPINEX_DIR="${VALHEIM_BEPINEX_DIR:-$VALHEIM_GAME_DIR/BepInEx}"
PKG="$HERE/package"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_ROLL_FORWARD=Major
export DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet build "$HERE/RadiusRings.csproj" -c Release

# --- optional live deploy, only if a local BepInEx install is found --------
if [ -d "$BEPINEX_DIR/plugins" ]; then
    DEST="$BEPINEX_DIR/plugins/eleventhtower-RadiusRings"
    MANAGED="$(/bin/ls -d "$BEPINEX_DIR"/plugins/*-RadiusRings-*/ 2>/dev/null | head -1 || true)"
    if [ -n "$MANAGED" ]; then
        echo "SKIPPING the live deploy: a mod manager already owns this mod at"
        echo "  ${MANAGED}"
        rm -rf "$DEST"
    else
        mkdir -p "$DEST"
        cp "$HERE/bin/Release/RadiusRings.dll" "$DEST/"
        echo "deployed (live, until your mod manager's next sync):"
        echo "  $DEST/RadiusRings.dll"
    fi
else
    echo "no BepInEx install found at $BEPINEX_DIR -- skipping live deploy"
fi

# --- Thunderstore package, for manual upload --------------------------------
rm -rf "$PKG"
mkdir -p "$PKG"
cp "$HERE/bin/Release/RadiusRings.dll" "$PKG/"
cp "$HERE/manifest.json" "$PKG/"
cp "$HERE/README.md" "$PKG/"
cp "$HERE/CHANGELOG.md" "$PKG/"
cp "$HERE/icon.png" "$PKG/"

rm -f "$HERE/RadiusRings-$VERSION.zip"
( cd "$PKG" && zip -q -r -X "$HERE/RadiusRings-$VERSION.zip" . )

echo
echo "Thunderstore package ready:"
echo "  $PKG"
echo "  $HERE/RadiusRings-$VERSION.zip"
echo "  Upload the zip via your team's submit page at https://thunderstore.io/c/valheim/"
