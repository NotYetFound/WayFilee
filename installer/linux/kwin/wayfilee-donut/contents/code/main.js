// KDE Wayland places application windows. Centre WayFilee's donut each time it is mapped.

function isDonut(w) {
    return w.caption === "WayFilee Donut";
}

function centreOnCursor(w) {
    var p = workspace.cursorPos;
    var g = w.frameGeometry;
    var x = Math.round(p.x - g.width / 2);
    var y = Math.round(p.y - g.height / 2);
    if (g.x !== x || g.y !== y) {
        w.frameGeometry = { x: x, y: y, width: g.width, height: g.height };
    }
}

function bringToFront(w) {
    // A Wayland popup can otherwise be stacked behind the file manager that
    // owns the drag, making the donut appear not to have opened.
    w.keepAbove = true;
    workspace.raiseWindow(w);
}

function configureDonut(w) {
    if (!isDonut(w)) {
        return;
    }
    // KWin can choose server-side decorations even when Avalonia requests none.
    // This is a compositor-owned setting on Wayland, so remove them here.
    w.noBorder = true;
    w.skipTaskbar = true;
    w.skipSwitcher = true;
    centreOnCursor(w);
    bringToFront(w);
}

workspace.windowAdded.connect(function (w) {
    // On Wayland the caption can arrive after windowAdded, and a hidden toplevel
    // can be mapped again with fresh server-side decorations.
    w.captionChanged.connect(function () { configureDonut(w); });
    // Plasma versions without windowShown still support captionChanged and windowAdded.
    if (w.windowShown) {
        w.windowShown.connect(function () { configureDonut(w); });
    }
    if (!isDonut(w)) {
        return;
    }
    // Filee also positions the window itself (twice, to cope with DPI changes), so win the last few moves.
    var left = 4;
    configureDonut(w);
    w.frameGeometryChanged.connect(function () {
        if (left > 0) {
            left--;
            centreOnCursor(w);
            bringToFront(w);
        }
    });
});
