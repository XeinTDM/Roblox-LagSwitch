using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RobloxLagswitch;

public class KeyboardListener(string targetProcessName, int key, Action keyAction) : HookListener
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private readonly string targetProcessName = targetProcessName;
    private readonly Action onKeyPressed = keyAction;
    private readonly int triggerKey = key;

    protected override int HookType => WH_KEYBOARD_LL;

    protected override IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var kbData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (kbData.vkCode == triggerKey)
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                GetWindowThreadProcessId(foregroundWindow, out uint pid);
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    if (proc.ProcessName.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase))
                        onKeyPressed?.Invoke();
                } catch { }
            }
        }
        return CallNextHookEx(nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}