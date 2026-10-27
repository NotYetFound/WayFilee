using System.Runtime.InteropServices;
using Filee.Core.Settings;

namespace Filee.App.Services.Triggers;

// libinput is a read-only observer here. It never grabs devices or injects events.
// The compositor still owns the pointer and performs the actual drag and drop.
internal sealed class WaylandInputHook : IDisposable
{
    private const string Libinput = "libinput.so.10";
    private const string Libudev = "libudev.so.1";
    private const string Libc = "libc.so.6";
    private readonly OpenRestricted _open = OpenDevice;
    private readonly CloseRestricted _close = CloseDevice;
    private readonly Action<TriggerMouseButton, int, int, ModifierKeys> _down;
    private readonly Action<TriggerMouseButton, int, int> _up;
    private readonly Action<int, int, ModifierKeys, TriggerMouseButton> _move;
    private readonly Action<string, bool> _key;
    private readonly Action _wheel;
    private Thread? _thread;
    private volatile bool _stop;
    private volatile bool _running;
    private double _x, _y;
    private ModifierKeys _modifiers;
    private TriggerMouseButton _button;

    internal WaylandInputHook(
        Action<TriggerMouseButton, int, int, ModifierKeys> down,
        Action<TriggerMouseButton, int, int> up,
        Action<int, int, ModifierKeys, TriggerMouseButton> move,
        Action<string, bool> key,
        Action wheel)
    {
        _down = down;
        _up = up;
        _move = move;
        _key = key;
        _wheel = wheel;
    }

    internal bool IsRunning => _running;
    internal event Action<Exception>? Failed;
    internal event Action? StatusChanged;

    internal void Start()
    {
        if (_thread is not null)
            return;
        _stop = false;
        _thread = new Thread(Run) { IsBackground = true, Name = "WayFilee libinput" };
        _thread.Start();
    }

    private void Run()
    {
        nint udev = 0, input = 0;
        nint callbacks = 0;
        try
        {
            udev = udev_new();
            if (udev == 0) throw new InvalidOperationException("Could not initialize udev.");
            callbacks = Marshal.AllocHGlobal(Marshal.SizeOf<InputInterface>());
            Marshal.StructureToPtr(new InputInterface(
                Marshal.GetFunctionPointerForDelegate(_open),
                Marshal.GetFunctionPointerForDelegate(_close)), callbacks, false);
            input = libinput_udev_create_context(callbacks, 0, udev);
            if (input == 0 || libinput_udev_assign_seat(input, "seat0") != 0)
                throw new InvalidOperationException("Could not open seat0 with libinput. Check /dev/input permission.");
            var fd = libinput_get_fd(input);
            if (fd < 0) throw new InvalidOperationException("libinput did not provide an event descriptor.");
            _running = true;
            StatusChanged?.Invoke();
            while (!_stop)
            {
                var pollFd = new PollFd(fd, 1, 0);
                var pollResult = poll(ref pollFd, 1, 100);
                if (pollResult < 0 && Marshal.GetLastPInvokeError() != 4)
                    throw new IOException("libinput event polling failed.");
                if (libinput_dispatch(input) != 0)
                    throw new IOException("libinput event dispatch failed.");
                nint ev;
                while ((ev = libinput_get_event(input)) != 0)
                {
                    try { ProcessEvent(ev); }
                    finally { libinput_event_destroy(ev); }
                }
            }
        }
        catch (Exception ex)
        {
            if (!_stop) Failed?.Invoke(ex);
        }
        finally
        {
            _running = false;
            if (input != 0) libinput_unref(input);
            if (callbacks != 0) Marshal.FreeHGlobal(callbacks);
            if (udev != 0) udev_unref(udev);
            StatusChanged?.Invoke();
        }
    }

    private void ProcessEvent(nint ev)
    {
        switch (libinput_event_get_type(ev))
        {
            case 300: // LIBINPUT_EVENT_KEYBOARD_KEY
                var keyboard = libinput_event_get_keyboard_event(ev);
                var code = libinput_event_keyboard_get_key(keyboard);
                var pressed = libinput_event_keyboard_get_key_state(keyboard) == 1;
                _modifiers = UpdateModifiers(_modifiers, code, pressed);
                if (KeyName(code) is { } name) _key(name, pressed);
                break;
            case 400: // LIBINPUT_EVENT_POINTER_MOTION
                var motion = libinput_event_get_pointer_event(ev);
                _x += libinput_event_pointer_get_dx(motion);
                _y += libinput_event_pointer_get_dy(motion);
                _move((int)Math.Round(_x), (int)Math.Round(_y), _modifiers, _button);
                break;
            case 402: // LIBINPUT_EVENT_POINTER_BUTTON
                var pointer = libinput_event_get_pointer_event(ev);
                var button = MouseButton(libinput_event_pointer_get_button(pointer));
                if (button == TriggerMouseButton.None) break;
                if (libinput_event_pointer_get_button_state(pointer) == 1)
                {
                    _button = button;
                    _down(button, (int)Math.Round(_x), (int)Math.Round(_y), _modifiers);
                }
                else
                {
                    _up(button, (int)Math.Round(_x), (int)Math.Round(_y));
                    if (_button == button) _button = TriggerMouseButton.None;
                }
                break;
            case 404: // LIBINPUT_EVENT_POINTER_SCROLL_WHEEL
                var scroll = libinput_event_get_pointer_event(ev);
                if (libinput_event_pointer_has_axis(scroll, 0) != 0 &&
                    libinput_event_pointer_get_scroll_value_v120(scroll, 0) != 0)
                    _wheel();
                break;
        }
    }

    internal static TriggerMouseButton MouseButton(uint code) => code switch
    {
        0x110 => TriggerMouseButton.Left,
        0x111 => TriggerMouseButton.Right,
        0x112 => TriggerMouseButton.Middle,
        0x113 => TriggerMouseButton.X1,
        0x114 => TriggerMouseButton.X2,
        _ => TriggerMouseButton.None,
    };

    internal static ModifierKeys UpdateModifiers(ModifierKeys current, uint code, bool pressed)
    {
        var modifier = code switch
        {
            29 or 97 => ModifierKeys.Ctrl,
            42 or 54 => ModifierKeys.Shift,
            56 or 100 => ModifierKeys.Alt,
            125 or 126 => ModifierKeys.Meta,
            _ => ModifierKeys.None,
        };
        return pressed ? current | modifier : current & ~modifier;
    }

    internal static string? KeyName(uint code) => code switch
    {
        15 => "Tab", 57 => "Space", 1 => "Escape", 28 => "Enter",
        59 => "F1", 60 => "F2", 61 => "F3", 62 => "F4", 63 => "F5", 64 => "F6",
        65 => "F7", 66 => "F8", 67 => "F9", 68 => "F10", 87 => "F11", 88 => "F12",
        _ => null,
    };

    public void Dispose()
    {
        _stop = true;
        if (_thread is { IsAlive: true } thread && !thread.Join(TimeSpan.FromSeconds(3)))
            throw new TimeoutException("libinput event thread did not stop.");
        _thread = null;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int OpenRestricted(nint path, int flags, nint userData);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CloseRestricted(int fd, nint userData);
    [StructLayout(LayoutKind.Sequential)]
    private struct InputInterface(nint open, nint close)
    {
        public nint Open = open;
        public nint Close = close;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd(int fd, short events, short returned)
    {
        public int Fd = fd;
        public short Events = events;
        public short Returned = returned;
    }

    private static int OpenDevice(nint path, int flags, nint userData)
    {
        var fd = open(path, flags);
        return fd >= 0 ? fd : -Marshal.GetLastPInvokeError();
    }
    private static void CloseDevice(int fd, nint userData) => close(fd);

    [DllImport(Libudev)] private static extern nint udev_new();
    [DllImport(Libudev)] private static extern nint udev_unref(nint udev);
    [DllImport(Libinput)] private static extern nint libinput_udev_create_context(nint callbacks, nint userData, nint udev);
    [DllImport(Libinput, CharSet = CharSet.Ansi)] private static extern int libinput_udev_assign_seat(nint input, string seat);
    [DllImport(Libinput)] private static extern int libinput_get_fd(nint input);
    [DllImport(Libinput)] private static extern int libinput_dispatch(nint input);
    [DllImport(Libinput)] private static extern nint libinput_get_event(nint input);
    [DllImport(Libinput)] private static extern void libinput_event_destroy(nint ev);
    [DllImport(Libinput)] private static extern nint libinput_unref(nint input);
    [DllImport(Libinput)] private static extern int libinput_event_get_type(nint ev);
    [DllImport(Libinput)] private static extern nint libinput_event_get_keyboard_event(nint ev);
    [DllImport(Libinput)] private static extern nint libinput_event_get_pointer_event(nint ev);
    [DllImport(Libinput)] private static extern uint libinput_event_keyboard_get_key(nint ev);
    [DllImport(Libinput)] private static extern int libinput_event_keyboard_get_key_state(nint ev);
    [DllImport(Libinput)] private static extern double libinput_event_pointer_get_dx(nint ev);
    [DllImport(Libinput)] private static extern double libinput_event_pointer_get_dy(nint ev);
    [DllImport(Libinput)] private static extern uint libinput_event_pointer_get_button(nint ev);
    [DllImport(Libinput)] private static extern int libinput_event_pointer_get_button_state(nint ev);
    [DllImport(Libinput)] private static extern int libinput_event_pointer_has_axis(nint ev, int axis);
    [DllImport(Libinput)] private static extern double libinput_event_pointer_get_scroll_value_v120(nint ev, int axis);
    [DllImport(Libc, SetLastError = true)] private static extern int open(nint path, int flags);
    [DllImport(Libc)] private static extern int close(int fd);
    [DllImport(Libc, SetLastError = true)] private static extern int poll(ref PollFd fds, nuint count, int timeout);
}
