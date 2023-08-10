## 7 Days To Die Synchronization Tool
This is a simple tool i made to keep my friends mods synchronized without them having to do any manual work.

## The mods.zip archive
Is simply the zipped mods folder in your 7D2D directory.
The contents will be extracted directly into the mods folder.

## Changing the version and mods.zip url
Simply change `ArchiveUrl` and `VersionUrl` at the top of `Program.cs`

## Versioning
The tool will simply make sure the clients version is exactly the same as whatever is inside version.txt on the update server. If this is not the case it will assume a new version is available.