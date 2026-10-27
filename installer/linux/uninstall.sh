#!/bin/sh
set -eu

data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
install_dir="$data_dir/wayfilee/application"
desktop_file="$data_dir/applications/com.wayfilee.app.desktop"
kwin_script_dir="$data_dir/kwin/scripts/wayfilee-donut"
python3 - "$data_dir" "$install_dir" <<'PY'
import pathlib
import sys

root, target = map(pathlib.Path, sys.argv[1:])
expected = root.resolve() / 'wayfilee' / 'application'
if target.is_symlink() or target.resolve() != expected:
    raise SystemExit('Refusing to remove an installation outside its expected directory.')
if (target / '.wayfilee-package').read_text(encoding='utf-8').strip() != 'com.wayfilee.app':
    raise SystemExit('The directory is not an installation created by WayFilee.')
PY
"$install_dir/Filee" --uninstall-cleanup
if [ -d "$kwin_script_dir" ] && [ ! -L "$kwin_script_dir" ] \
    && [ -f "$kwin_script_dir/.wayfilee-package" ] && [ ! -L "$kwin_script_dir/.wayfilee-package" ] \
    && [ "$(cat "$kwin_script_dir/.wayfilee-package")" = 'com.wayfilee.app' ]; then
    if command -v kwriteconfig6 >/dev/null 2>&1; then
        kwriteconfig6 --file kwinrc --group Plugins --key wayfilee-donutEnabled false || true
    fi
    rm -rf -- "$kwin_script_dir"
    if command -v qdbus6 >/dev/null 2>&1; then
        qdbus6 org.kde.KWin /KWin reconfigure >/dev/null 2>&1 || true
    fi
fi
if [ -L "$HOME/.local/bin/wayfilee" ] && [ "$(readlink "$HOME/.local/bin/wayfilee")" = "$install_dir/filee-cli" ]; then
    rm -- "$HOME/.local/bin/wayfilee"
fi
if [ -f "$desktop_file" ] && grep -q '^X-WayFilee-Managed=true$' "$desktop_file"; then
    rm -- "$desktop_file"
fi
rm -rf -- "$install_dir"
printf '%s\n' 'WayFilee was removed. Your settings and conversion results were kept.'
