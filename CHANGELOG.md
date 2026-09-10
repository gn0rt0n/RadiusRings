# Changelog

## 1.0.1

- Ring labels now face the direction the camera is looking (falling back to
  north if looking straight up/down), instead of always sitting on the north
  side of the ring regardless of view.
- Plugin namespace changed to `EleventhTower.valheim.radiusrings`.
- Added an in-game screenshot to the README and as the package icon.

## 1.0.0

- Initial release. Toggleable concentric distance rings around the player,
  hugging terrain height, with configurable spacing, max radius, segment
  count, refresh interval and highlight interval, plus a floating metre label
  on every ring.
