using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RobloxLagswitch;

public class MouseListener(string processName, Action leftButtonDownAction, Action leftButtonUpAction) : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private LowLevelMouseProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private readonly string targetProcessName = processName;
    private readonly Action onLeftButtonDown = leftButtonDownAction;
    private readonly Action onLeftButtonUp = leftButtonUpAction;

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

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        using Process curProcess = Process.GetCurrentProcess();
        using ProcessModule curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_MOUSE_LL, proc,
            GetModuleHandle(curModule.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                int wmCode = wParam.ToInt32();

                if (wmCode == WM_LBUTTONDOWN || wmCode == WM_LBUTTONUP)
                {
                    IntPtr foregroundWindow = GetForegroundWindow();
                    GetWindowThreadProcessId(foregroundWindow, out uint pid);
                    try
                    {
                        Process proc = Process.GetProcessById((int)pid);
                        if (proc.ProcessName.Equals(targetProcessName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (wmCode == WM_LBUTTONDOWN)
                            {
                                onLeftButtonDown?.Invoke();
                            }
                            else if (wmCode == WM_LBUTTONUP)
                            {
                                onLeftButtonUp?.Invoke();
                            }
                        }
                    } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("MouseListener exception: " + ex.Message);
        }
         
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto,
        SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public void Dispose()
    {
        Stop();
    }
}
