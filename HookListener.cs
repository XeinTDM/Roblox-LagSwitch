using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RobloxLagswitch;

public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

public abstract class HookListener : IDisposable
{
    protected IntPtr _hookID = IntPtr.Zero;
    protected HookProc _proc;

    protected abstract int HookType { get; }
    protected abstract IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam);

    public void Start()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    public void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    private IntPtr SetHook(HookProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        return SetWindowsHookEx(HookType, proc, GetModuleHandle(module.ModuleName), 0);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    protected static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    protected static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    protected static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    protected static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    public virtual void Dispose() => Stop();

    protected IntPtr CallNextHookEx(int nCode, IntPtr wParam, IntPtr lParam) =>
        CallNextHookEx(_hookID, nCode, wParam, lParam);
}
