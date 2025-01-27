using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RobloxLagswitch;

public class MouseButtonListener(string targetProcessName, int buttonCode, Action buttonAction) : HookListener
{
    private const int WH_MOUSE_LL = 14;
    private readonly string targetProcessName = targetProcessName;
    private readonly Action onButtonPressed = buttonAction;
    private readonly int triggerButtonCode = buttonCode;

    protected override int HookType => WH_MOUSE_LL;

    protected override IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var mouseData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int vkCode = GetMouseButtonVKCode(wParam.ToInt32(), mouseData.mouseData);

            if (vkCode == triggerButtonCode)
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                GetWindowThreadProcessId(foregroundWindow, out uint pid);
                try
                {
                    using var proc = Process.GetProcessById((int)pid);
                    if (proc.ProcessName.Equals(targetProcessName, StringComparison.OrdinalIgnoreCase))
                        onButtonPressed?.Invoke();
                } catch { }
            }
        }
        return CallNextHookEx(nCode, wParam, lParam);
    }

    private static int GetMouseButtonVKCode(int wmCode, int mouseData) => wmCode switch
    {
        0x0201 => 0x01, // WM_LBUTTONDOWN
        0x0204 => 0x02, // WM_RBUTTONDOWN
        0x0207 => 0x04, // WM_MBUTTONDOWN
        0x020B => (mouseData >> 16) switch
        {
            1 => 0x05, // XBUTTON1
            2 => 0x06, // XBUTTON2
            _ => 0
        },
        _ => 0
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}