#!/usr/bin/env bash
set -euo pipefail

root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
version=${1:-$(python3 - "$root/Directory.Build.props" <<'PY'
import sys
import xml.etree.ElementTree as ET
print(ET.parse(sys.argv[1]).findtext('./PropertyGroup/Version'))
PY
)}
version=${version#v}
if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z][0-9A-Za-z.-]*)?$ ]]; then
    echo "Invalid version: $version" >&2
    exit 1
fi

rid=${WAYFILEE_RID:-linux-$(uname -m)}
case "$rid" in
    linux-x86_64) rid=linux-x64 ;;
    linux-aarch64) rid=linux-arm64 ;;
    linux-x64|linux-arm64) ;;
    *) echo "Unsupported Linux architecture: $rid" >&2; exit 1 ;;
esac

out=${WAYFILEE_OUTPUT:-"$root/Releases"}
mkdir -p "$out"
name="Filee-$version-$rid"
archive="$out/$name.tar.gz"
sums="$out/$name-SHA256SUMS.txt"
if [[ -e $archive || -e $sums ]]; then
    echo "Release files already exist for $name" >&2
    exit 1
fi
stage=$(mktemp -d)
trap 'rm -rf -- "$stage"' EXIT
package="$stage/$name"
mkdir -p "$package"

for project in Filee.App Filee.Cli; do
    dotnet publish "$root/src/$project/$project.csproj" -c Release -r "$rid" \
        --self-contained true -p:Version="$version" -p:FileeRelease=true \
        -p:PublishReadyToRun=true -p:NuGetAudit=false --ignore-failed-sources -m:1 -o "$package"
done
for exe in Filee filee-cli; do
    test -f "$package/$exe"
    chmod +x "$package/$exe"
done
# SharpHook is retained for gesture lifecycle tests. The Linux app uses libinput
# directly, so its old X11 native backends must not ship in the Wayland archive.
rm -f "$package/libuiohook-x11.so" "$package/libuiohook-xrecord.so"

cp "$root/src/Filee.App/Assets/Icons/filee.png" "$package/"
cp "$root/src/Filee.App/Assets/Icons/filee.svg" "$package/"
cp "$root/installer/linux/"{install.sh,uninstall.sh,INSTALL.txt} "$package/"
cp -R "$root/installer/linux/kwin" "$package/"
cp "$root/"{LICENSE,THIRD-PARTY-NOTICES.md} "$package/"
chmod +x "$package/"{install.sh,uninstall.sh}

tar -czf "$archive" -C "$stage" "$name"
(cd "$out" && sha256sum "$name.tar.gz" > "$name-SHA256SUMS.txt")
echo "$archive"
echo "$sums"
