# Security policy

## Supported versions

Security fixes target the [latest WayFilee release](https://github.com/NotYetFound/WayFilee/releases/latest). Check whether a problem still occurs there before reporting it.

## Reporting a vulnerability

Please don't open a public issue for a security problem.

Report it privately with **[Report a vulnerability](https://github.com/NotYetFound/WayFilee/security/advisories/new)** on
the repository's Security tab.

Include the WayFilee version, your KDE Plasma and distribution versions, what an attacker can do and the steps or file that show it. If a
crafted file triggers it, attach the file, or say how to make one.

You'll get a reply as soon as possible. Once the fix is released, the advisory is published and you're credited
unless you'd rather not be. A release that fixes a security problem is marked *required*, so every copy shows it
each time it starts until it is updated.

## Scope

In scope:

- WayFilee itself: the app, the CLI, KDE integration, and watch folders.
- Files that make WayFilee do more than convert them, such as writing outside the output folder, running code, or
  reading files they shouldn't.
- How Filee downloads and checks updates and engines.

Out of scope:

- Bugs in the engines (FFmpeg, LibreOffice, Ghostscript, Pandoc, rhwp, 7-Zip and others). Report those to their projects. Tell us too if WayFilee should update a pinned version.
- Attacks that need someone who can already run programs as you on your computer.

## How downloads are checked

- **Updates:** the app downloads a matching package or archive from GitHub Releases and compares its SHA-256 with the asset's `digest` in GitHub's release API before installing it. An absent or malformed digest prevents installation. System packages are then passed to DNF, APT, or pacman through administrator authentication.
- **Engines:** every engine comes from the official location pinned in
  [`engines.json`](src/Filee.Engines/Infrastructure/engines.json), with its SHA-256 and size. A download that doesn't
  match is discarded.
- **Release assets:** GitHub displays each asset's SHA-256 digest on the release page; separate checksum files are no longer published.

WayFilee converts files on your own computer and never uploads them. See the
[Privacy policy](README.md#privacy-policy).
