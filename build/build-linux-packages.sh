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
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z][0-9A-Za-z.-]*)?$ ]] || {
    echo "Invalid version: $version" >&2; exit 1;
}
[[ $version != *-* ]] || { echo 'Distro packages require a stable X.Y.Z version' >&2; exit 1; }
rid=${WAYFILEE_RID:-linux-$(uname -m)}
case "$rid" in
    linux-x86_64) rid=linux-x64 ;;
    linux-aarch64) rid=linux-arm64 ;;
    linux-x64|linux-arm64) ;;
    *) echo "Unsupported RID: $rid" >&2; exit 1 ;;
esac
case "$rid" in
    linux-x64) rpm_arch=x86_64; deb_arch=amd64; arch_arch=x86_64 ;;
    linux-arm64) rpm_arch=aarch64; deb_arch=arm64; arch_arch=aarch64 ;;
esac

formats=${WAYFILEE_FORMATS:-"rpm deb arch"}
out=${WAYFILEE_OUTPUT:-"$root/Releases"}
mkdir -p "$out"
work=$(mktemp -d)
trap 'rm -rf -- "$work"' EXIT

for format in $formats; do
    case "$format" in rpm|deb|arch) ;; *) echo "Unknown package format: $format" >&2; exit 1 ;; esac
    case "$format" in
        rpm) command -v rpmbuild >/dev/null || { echo 'rpmbuild is required' >&2; exit 1; } ;;
        deb) command -v dpkg-deb >/dev/null || { echo 'dpkg-deb is required' >&2; exit 1; } ;;
        arch) command -v zstd >/dev/null || { echo 'zstd is required' >&2; exit 1; } ;;
    esac
    name="WayFilee-$version-$rid.$(case "$format" in rpm) echo rpm ;; deb) echo deb ;; arch) echo pkg.tar.zst ;; esac)"
    artifact="$out/$name"
    [[ ! -e $artifact ]] || { echo "Package already exists: $name" >&2; exit 1; }

    stage="$work/$format"
    install_root="$stage/root"
    app_dir="$install_root/opt/wayfilee"
    mkdir -p "$app_dir" "$install_root/usr/bin" "$install_root/usr/share/applications" \
        "$install_root/usr/share/icons/hicolor/scalable/apps" "$install_root/usr/share/kwin/scripts"
    dotnet publish "$root/src/Filee.App/Filee.App.csproj" -c Release -r "$rid" \
        --self-contained true -p:Version="$version" -p:FileeRelease=true \
        -p:WayFileePackageKind="$format" -p:PublishReadyToRun=true \
        -p:NuGetAudit=false --ignore-failed-sources -m:1 -o "$app_dir"
    dotnet publish "$root/src/Filee.Cli/Filee.Cli.csproj" -c Release -r "$rid" \
        --self-contained true -p:Version="$version" -p:FileeRelease=true \
        -p:PublishReadyToRun=true -p:NuGetAudit=false --ignore-failed-sources -m:1 -o "$app_dir"
    chmod 755 "$app_dir/Filee" "$app_dir/filee-cli"
    rm -f "$app_dir/libuiohook-x11.so" "$app_dir/libuiohook-xrecord.so"
    cp "$root/LICENSE" "$root/THIRD-PARTY-NOTICES.md" "$app_dir/"
    ln -s /opt/wayfilee/filee-cli "$install_root/usr/bin/wayfilee"
    cp "$root/src/Filee.App/Assets/Icons/filee.svg" \
        "$install_root/usr/share/icons/hicolor/scalable/apps/com.wayfilee.app.svg"
    cp -R "$root/installer/linux/kwin/wayfilee-donut" "$install_root/usr/share/kwin/scripts/"
    cat > "$install_root/usr/share/applications/com.wayfilee.app.desktop" <<'DESKTOP'
[Desktop Entry]
Type=Application
Name=WayFilee
Comment=Convert files with WayFilee
Exec=/opt/wayfilee/Filee --convert %F
Icon=com.wayfilee.app
Terminal=false
Categories=Utility;
DESKTOP

    case "$format" in
        deb)
            mkdir -p "$install_root/DEBIAN"
            cat > "$install_root/DEBIAN/control" <<CONTROL
Package: wayfilee
Version: $version
Section: utils
Priority: optional
Architecture: $deb_arch
Maintainer: WayFilee contributors <noreply@github.com>
Depends: libc6 (>= 2.35), libinput10 | libinput10t64, libudev1 | libudev1t64, libwayland-client0, fontconfig, libfreetype6, python3, pkexec
Description: Wayland-native file converter for KDE Plasma
 WayFilee offers file conversion through a native window and a KWin donut toolbar.
CONTROL
            dpkg-deb --build --root-owner-group "$install_root" "$artifact" >/dev/null
            ;;
        rpm)
            mkdir -p "$stage/rpmbuild"/{BUILD,RPMS,SOURCES,SPECS,SRPMS} "$stage/tmp"
            cat > "$stage/rpmbuild/SPECS/wayfilee.spec" <<SPEC
Name: wayfilee
Version: $version
Release: 1
Summary: Wayland-native file converter for KDE Plasma
License: MIT
URL: https://github.com/NotYetFound/WayFilee
BuildArch: $rpm_arch
AutoReqProv: no
Requires: glibc, libinput, systemd-libs, libwayland-client, fontconfig, freetype, python3, polkit

%description
WayFilee offers file conversion through a native window and a KWin donut toolbar.

%install
mkdir -p "%{buildroot}"
cp -a "$install_root"/. "%{buildroot}"/

%files
/opt/wayfilee
/usr/bin/wayfilee
/usr/share/applications/com.wayfilee.app.desktop
/usr/share/icons/hicolor/scalable/apps/com.wayfilee.app.svg
/usr/share/kwin/scripts/wayfilee-donut
SPEC
            rpmbuild -bb --quiet --define "_topdir $stage/rpmbuild" \
                --define "_buildrootdir $stage/buildroot" \
                --define "_tmppath $stage/tmp" \
                "$stage/rpmbuild/SPECS/wayfilee.spec"
            cp "$stage/rpmbuild/RPMS/$rpm_arch/wayfilee-$version-1.$rpm_arch.rpm" "$artifact"
            ;;
        arch)
            cat > "$install_root/.PKGINFO" <<PKGINFO
pkgname = wayfilee
pkgbase = wayfilee
pkgver = $version-1
pkgdesc = Wayland-native file converter for KDE Plasma
url = https://github.com/NotYetFound/WayFilee
builddate = $(date +%s)
packager = WayFilee contributors
size = $(du -sb "$install_root" | cut -f1)
arch = $arch_arch
license = MIT
depend = glibc
depend = libinput
depend = systemd-libs
depend = wayland
depend = fontconfig
depend = freetype2
depend = python
depend = polkit
PKGINFO
            tar --zstd --owner=0 --group=0 --numeric-owner -cf "$artifact" -C "$install_root" .
            ;;
    esac
    echo "$artifact"
done
