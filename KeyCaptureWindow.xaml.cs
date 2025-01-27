using System.Windows.Input;
using System.Windows;

namespace RobloxLagswitch;

public partial class KeyCaptureWindow : Window
{
    public InputTrigger CapturedInput { get; private set; }

    public KeyCaptureWindow()
    {
        InitializeComponent();
        KeyDown += KeyCaptureWindow_KeyDown;
        MouseDown += KeyCaptureWindow_MouseDown;
    }

    private void KeyCaptureWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        int virtualKeyCode = KeyInterop.VirtualKeyFromKey(e.Key);
        CapturedInput = new InputTrigger
        {
            Type = InputTrigger.TriggerType.Key,
            Code = virtualKeyCode
        };
        DialogResult = true;
        Close();
    }

    private void KeyCaptureWindow_MouseDown(object sender, MouseButtonEventArgs e)
    {
        int mouseButtonCode = GetMouseButtonCode(e.ChangedButton);
        if (mouseButtonCode != 0)
        {
            CapturedInput = new InputTrigger
            {
                Type = InputTrigger.TriggerType.MouseButton,
                Code = mouseButtonCode
            };
            DialogResult = true;
            Close();
        }
    }

    private static int GetMouseButtonCode(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => 0x01,
            MouseButton.Right => 0x02,
            MouseButton.Middle => 0x04,
            MouseButton.XButton1 => 0x05,
            MouseButton.XButton2 => 0x06,
            _ => 0,
        };
    }
}
