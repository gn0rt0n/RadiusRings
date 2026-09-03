# Building from source

Requires the .NET SDK and a local Valheim install (its managed assemblies are
referenced at build time, not vendored). The build defaults to the macOS Steam
install path; on another platform or layout, point `VALHEIM_GAME_DIR` at the
folder that contains your Valheim installation (the one with `BepInEx/` in
it):

```sh
VALHEIM_GAME_DIR=/path/to/Valheim ./build.sh
```

`build.sh` builds the plugin, deploys it into a local `BepInEx/plugins/` if
one is found there, and stages a Thunderstore-shaped package (and zip) under
`package/` for manual upload.
