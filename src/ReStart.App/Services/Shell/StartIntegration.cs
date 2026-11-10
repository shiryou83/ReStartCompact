using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using ReStart.Core;

namespace ReStart.App.Services;

/// <summary>Adapts native shell input to dispatcher callbacks; low-level hooks must remain short and reversible.</summary>
internal sealed class StartIntegration : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Action _toggle;
    private readonly Action _dismiss;
    private readonly Action<Native.Point> _toggleFromStart;
    private readonly Native.HookProc _keyboardCallback;
    private readonly Native.HookProc _mouseCallback;
    private readonly Native.WinEventProc _foregroundCallback;
    private WindowsKeyState _keys = new();
    private nint _keyboard, _mouse, _foreground;
    private int _visibilityGeneration;
    private uint _openedAt;
    internal bool WatchesForeground => Volatile.Read(ref _foreground) != 0;
    private readonly nint _launcher;
    private readonly HwndSource _source;
    private readonly StartButtonLocator _startButtons = new();
    private volatile bool _captureStart;
    private readonly StartButtonPress _startPress = new();
    private volatile bool _captureWindows;
    private volatile bool _menuOpen, _dismissOnTaskbar, _dismissOutsideCompact;
    private readonly Thread _hookThread;
    private uint _hookThreadId;
    public string? Status { get; private set; }

    public StartIntegration(nint handle, Dispatcher dispatcher, Action toggle, Action dismiss, Action<Native.Point> toggleFromStart)
    {
        _dispatcher = dispatcher;
        _toggle = toggle;
        _dismiss = dismiss;
        _toggleFromStart = toggleFromStart;
        _keyboardCallback = KeyboardHook;
        _mouseCallback = MouseHook;
        _foregroundCallback = ForegroundChanged;
        _launcher = handle;
        _source = HwndSource.FromHwnd(handle)!;
        _source.AddHook(WindowProc);
        using var ready = new ManualResetEventSlim();
        _hookThread = new Thread(() =>
        {
            _hookThreadId = Native.GetCurrentThreadId();
            Native.PeekMessage(out _, 0, 0, 0, 0);
            ready.Set();
            try
            {
                while (Native.GetMessage(out var message, 0, 0, 0) > 0)
                {
                    if (message.Id == 0x8001) UpdateHooks();
                    else { Native.TranslateMessage(ref message); Native.DispatchMessage(ref message); }
                }
            }
            finally
            {
                if (_keyboard != 0) Native.UnhookWindowsHookEx(_keyboard);
                if (_mouse != 0) Native.UnhookWindowsHookEx(_mouse);
                if (_foreground != 0) { Native.UnhookWinEvent(_foreground); _foreground = 0; }
            }
        })
        { IsBackground = true, Name = "ReStart input hooks" };
        _hookThread.Start();
        ready.Wait();
        if (!Native.RegisterHotKey(handle, 0x5253, 0x4003, 0x20))
            Status = L.T(K.ErrorHotkeyInUse);
    }

    public void Apply(Preferences preferences)
    {
        _captureWindows = preferences.CaptureWindowsKey;
        _captureStart = preferences.CaptureStartButton;
        _startButtons.SetEnabled(_captureStart);
        _dismissOnTaskbar = preferences.CloseOnDeactivate;
        _dismissOutsideCompact = preferences.StartMenuMode == StartMenuMode.Compact;
        Native.PostThreadMessage(_hookThreadId, 0x8001, 0, 0);
    }

    /// <summary>Installs taskbar-dismiss detection only while the menu is visible.</summary>
    public void SetMenuOpen(bool visible)
    {
        if (visible) _openedAt = unchecked((uint)Environment.TickCount);
        _menuOpen = visible;
        Interlocked.Increment(ref _visibilityGeneration);
        Native.PostThreadMessage(_hookThreadId, 0x8001, 0, 0);
    }

    private void UpdateHooks()
    {
        if (_captureWindows && _keyboard == 0)
        {
            _keyboard = Native.SetWindowsHookEx(13, _keyboardCallback, Native.GetModuleHandle(null), 0);
            if (_keyboard == 0) Status = L.T(K.ErrorWindowsKeyCapture) + new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }
        if (!_captureWindows && _keyboard != 0) { Native.UnhookWindowsHookEx(_keyboard); _keyboard = 0; _keys = new(); }
        var needMouse = _captureStart || _startPress.IsPressed || _dismissOnTaskbar && _menuOpen;
        if (needMouse && _mouse == 0) _mouse = Native.SetWindowsHookEx(14, _mouseCallback, Native.GetModuleHandle(null), 0);
        if (!needMouse && _mouse != 0) { Native.UnhookWindowsHookEx(_mouse); _mouse = 0; }
        var watchForeground = _dismissOnTaskbar && _menuOpen;
        // Out-of-context callbacks use this thread's message loop; no code is
        // injected into Explorer or the selected application.
        if (watchForeground && _foreground == 0) _foreground = Native.SetWinEventHook(3, 3, 0, _foregroundCallback, 0, 0, 0);
        if (!watchForeground && _foreground != 0) { Native.UnhookWinEvent(_foreground); _foreground = 0; }
    }

    private void ForegroundChanged(nint hook, uint eventId, nint window, int objectId, int childId, uint thread, uint time)
    {
        if (!_menuOpen || !_dismissOnTaskbar || unchecked((int)(time - Volatile.Read(ref _openedAt))) < 0) return;
        var generation = Volatile.Read(ref _visibilityGeneration);
        _dispatcher.BeginInvoke(() =>
        {
            // A thumbnail can deactivate the launcher first, so WPF may not
            // raise Deactivated again when its actual application is chosen.
            // Recheck current ownership to discard queued events after reopen.
            if (!_menuOpen || !_dismissOnTaskbar || generation != Volatile.Read(ref _visibilityGeneration)) return;
            var current = Native.GetForegroundWindow();
            if (current == 0 || current != window || IsOwnedWindow(current)) return;
            var name = new System.Text.StringBuilder(128);
            Native.GetClassName(Native.GetAncestor(current, 2), name, 128);
            if (name.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "TaskListThumbnailWnd" or "#32768") return;
            _dismiss();
        }, DispatcherPriority.Input);
    }

    private bool IsOwnedWindow(nint window)
    {
        if (window == _source.Handle || Native.GetAncestor(window, 3) == Native.GetAncestor(_source.Handle, 3)) return true;
        // WPF can use a hidden native owner for windows omitted from the
        // taskbar. Its managed ownership remains authoritative for dialogs.
        for (var owned = HwndSource.FromHwnd(window)?.RootVisual as System.Windows.Window; owned != null; owned = owned.Owner)
            if (new WindowInteropHelper(owned).Handle == _source.Handle) return true;
        return false;
    }

    private nint WindowProc(nint hwnd, int msg, nint wp, nint lp, ref bool handled)
    {
        if (msg == 0x0312 && wp == 0x5253) { handled = true; QueueToggle(); }
        return 0;
    }

    private nint KeyboardHook(int code, nint wp, nint lp)
    {
        if (code < 0) return Native.CallNextHookEx(_keyboard, code, wp, lp);
        var data = Marshal.PtrToStructure<Native.KeyboardData>(lp);
        if ((data.Flags & 0x10) != 0) return Native.CallNextHookEx(_keyboard, code, wp, lp);
        var down = wp is 0x100 or 0x104;
        var modifier = (Native.GetAsyncKeyState(0x10) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x11) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x12) & 0x8000) != 0;
        switch (_keys.Process((int)data.VkCode, down, modifier))
        {
            case WindowsKeyAction.Suppress: return 1;
            case WindowsKeyAction.Toggle: QueueToggle(); return 1;
            case WindowsKeyAction.ReplayWindowsDown:
                Native.SendInput(1, [new Native.Input { Type = 1, Union = new() { Keyboard = new() { Vk = (ushort)_keys.HeldKey } } }], Marshal.SizeOf<Native.Input>());
                break;
        }
        return Native.CallNextHookEx(_keyboard, code, wp, lp);
    }

    private nint MouseHook(int code, nint wp, nint lp)
    {
        if (code >= 0 && wp is 0x201 or 0x202)
        {
            var point = Marshal.PtrToStructure<Native.MouseData>(lp).Point;
            switch (_startPress.Process(wp == 0x201, _captureStart && _startButtons.Contains(point), _captureStart))
            {
                case StartButtonAction.Toggle:
                    _dispatcher.BeginInvoke(() => _toggleFromStart(point), DispatcherPriority.Send);
                    return 1;
                case StartButtonAction.Suppress: return 1;
            }
        }
        if (code >= 0 && _menuOpen && _dismissOnTaskbar && wp is 0x201 or 0x204 or 0x207)
        {
            var data = Marshal.PtrToStructure<Native.MouseData>(lp);
            var window = Native.WindowFromPoint(data.Point);
            var root = Native.GetAncestor(window, 2);
            var rootName = new System.Text.StringBuilder(128);
            Native.GetClassName(root, rootName, 128);
            if (rootName.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "TaskListThumbnailWnd")
                _dispatcher.BeginInvoke(_dismiss, DispatcherPriority.Input);
            else if (_dismissOutsideCompact && window != 0 && root != _launcher)
            {
                var generation = Volatile.Read(ref _visibilityGeneration);
                _dispatcher.BeginInvoke(() =>
                {
                    if (_menuOpen && _dismissOnTaskbar && _dismissOutsideCompact &&
                        generation == Volatile.Read(ref _visibilityGeneration) && !IsOwnedWindow(window)) _dismiss();
                }, DispatcherPriority.Input);
            }
        }
        return Native.CallNextHookEx(_mouse, code, wp, lp);
    }

    private void QueueToggle() => _dispatcher.BeginInvoke(_toggle, DispatcherPriority.Send);

    public void Dispose()
    {
        _menuOpen = false;
        Interlocked.Increment(ref _visibilityGeneration);
        _startButtons.Dispose();
        Native.PostThreadMessage(_hookThreadId, 0x12, 0, 0);
        _hookThread.Join(TimeSpan.FromSeconds(1));
        Native.UnregisterHotKey(_source.Handle, 0x5253);
        _source.RemoveHook(WindowProc);
    }
}
