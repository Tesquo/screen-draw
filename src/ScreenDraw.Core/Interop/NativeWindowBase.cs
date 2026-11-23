using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ScreenDraw.Core.Interop;

public abstract class NativeWindowBase : IDisposable
{
    private static readonly HashSet<string> KnownClasses = new(StringComparer.Ordinal);
    private static readonly Lock ClassLock = new();
    private static readonly WindowProcedure Procedure = Dispatch;

    private GCHandle _self;
    private nint _handle;
    private bool _disposed;

    protected NativeWindowBase(string className, nint cursor, uint style, uint exStyle, Rectangle bounds, nint parent)
    {
        EnsureClassRegistered(className, cursor);
        _self = GCHandle.Alloc(this);
        _handle = NativeMethods.CreateWindowEx(
            exStyle,
            className,
            null,
            style,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            parent,
            0,
            NativeMethods.GetModuleHandle(null),
            GCHandle.ToIntPtr(_self));

        if (_handle == 0)
        {
            _self.Free();
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not create a '{className}' window.");
        }
    }

    public nint Handle => _handle;

    public bool IsDisposed => _disposed;

    public virtual void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_handle != 0)
        {
            NativeMethods.DestroyWindow(_handle);
            _handle = 0;
        }

        if (_self.IsAllocated)
        {
            _self.Free();
        }

        GC.SuppressFinalize(this);
    }

    protected virtual nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam) =>
        NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);

    private static nint Dispatch(nint hwnd, uint message, nint wParam, nint lParam)
    {
        if (message == WindowsMessages.NcCreate)
        {
            var create = Marshal.PtrToStructure<CreateStruct>(lParam);
            if (GCHandle.FromIntPtr(create.CreateParams).Target is not NativeWindowBase window)
            {
                return 0;
            }

            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlUserData, create.CreateParams);
            window._handle = hwnd;
        }

        var instance = FromHandle(hwnd);
        if (message == WindowsMessages.NcDestroy)
        {
            var result = instance?.WindowProc(hwnd, message, wParam, lParam)
                ?? NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);

            if (instance is not null)
            {
                instance._handle = 0;
                if (instance._self.IsAllocated)
                {
                    instance._self.Free();
                }
            }

            return result;
        }

        return instance?.WindowProc(hwnd, message, wParam, lParam)
            ?? NativeMethods.DefWindowProc(hwnd, message, wParam, lParam);
    }

    private static NativeWindowBase? FromHandle(nint hwnd)
    {
        var userData = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlUserData);
        return userData == 0 ? null : GCHandle.FromIntPtr(userData).Target as NativeWindowBase;
    }

    private static void EnsureClassRegistered(string className, nint cursor)
    {
        lock (ClassLock)
        {
            if (KnownClasses.Contains(className))
            {
                return;
            }

            var name = Marshal.StringToHGlobalUni(className);
            try
            {
                var windowClass = new WindowClass
                {
                    Size = (uint)Marshal.SizeOf<WindowClass>(),
                    WindowProcedure = Marshal.GetFunctionPointerForDelegate(Procedure),
                    Instance = NativeMethods.GetModuleHandle(null),
                    Cursor = cursor,
                    ClassName = name,
                };

                if (NativeMethods.RegisterClassEx(ref windowClass) == 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != NativeMethods.ErrorClassAlreadyExists)
                    {
                        throw new Win32Exception(error, $"Could not register window class '{className}'.");
                    }
                }

                KnownClasses.Add(className);
            }
            finally
            {
                Marshal.FreeHGlobal(name);
            }
        }
    }
}
