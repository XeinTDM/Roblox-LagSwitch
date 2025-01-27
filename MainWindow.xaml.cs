using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Color = System.Windows.Media.Color;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;
using System.IO;

namespace RobloxLagswitch;

public partial class MainWindow : Window
{
    private KeyboardListener keyboardListener;
    private MouseButtonListener mouseButtonListener;
    private MouseListener mouseListener;
    private bool isBlocked = false;
    private string overlayMode = "Outline";
    private string targetProcessName = "RobloxPlayerBeta";
    private string executablePath;
    private string ruleName;

    private InputTrigger selectedTrigger = new InputTrigger
    {
        Type = InputTrigger.TriggerType.Key,
        Code = KeyInterop.VirtualKeyFromKey(Key.G)
    };

    private bool isOperationInProgress = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeProcessPath();

        if (!string.IsNullOrEmpty(executablePath) && !string.IsNullOrEmpty(ruleName))
        {
            NetworkManager.EnsureRuleExists(executablePath, ruleName);
            UpdateStatus();
            InitializeListener();
        }
        else
        {
            MessageBox.Show("Failed to initialize process path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            DisableBlockingFeatures();
        }

        string triggerName = GetTriggerName(selectedTrigger);
        KeyChangeButton.Content = $"Trigger: {triggerName}";
    }

    private void InitializeListener()
    {
        keyboardListener?.Dispose();
        mouseButtonListener?.Dispose();

        if (selectedTrigger.Type == InputTrigger.TriggerType.Key)
        {
            keyboardListener = new KeyboardListener(targetProcessName, selectedTrigger.Code, OnKeyPressed);
            keyboardListener.Start();
        }
        else if (selectedTrigger.Type == InputTrigger.TriggerType.MouseButton)
        {
            mouseButtonListener = new MouseButtonListener(targetProcessName, selectedTrigger.Code, OnKeyPressed);
            mouseButtonListener.Start();
        }
    }

    private void InitializeProcessPath()
    {
        if (ProcessComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            string selection = selectedItem.Content.ToString();

            if (selection == "Roblox")
            {
                InitializeRobloxPath();
            }
            else if (selection == "Add...")
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Executable Files (*.exe)|*.exe"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    executablePath = openFileDialog.FileName;
                    targetProcessName = Path.GetFileNameWithoutExtension(executablePath);

                    ComboBoxItem newItem = new ComboBoxItem { Content = targetProcessName };
                    ProcessComboBox.Items.Insert(ProcessComboBox.Items.Count - 1, newItem);
                    newItem.IsSelected = true;

                    ruleName = "Block " + Path.GetFileName(executablePath);
                    NetworkManager.EnsureRuleExists(executablePath, ruleName);
                }
                else
                {
                    ProcessComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                targetProcessName = selection;
                executablePath = FindExecutablePath(targetProcessName);
                if (string.IsNullOrEmpty(executablePath))
                {
                    MessageBox.Show($"Executable for {targetProcessName} not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DisableBlockingFeatures();
                }
                else
                {
                    ruleName = "Block " + Path.GetFileName(executablePath);
                    NetworkManager.EnsureRuleExists(executablePath, ruleName);
                }
            }
            InitializeListener();
            UpdateStatus();
        }
        else
        {
            if (ProcessComboBox.Items.Count > 0)
            {
                ProcessComboBox.SelectedIndex = 0;
                InitializeProcessPath();
                return;
            }
            else
            {
                MessageBox.Show("No items in ProcessComboBox.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DisableBlockingFeatures();
                return;
            }
        }
    }

    private void ProcessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        InitializeProcessPath();
    }

    private void InitializeRobloxPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string robloxVersionsPath = Path.Combine(localAppData, "Roblox", "Versions");

        if (!Directory.Exists(robloxVersionsPath))
        {
            MessageBox.Show($"Roblox Versions folder not found at:\n{robloxVersionsPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            DisableBlockingFeatures();
            return;
        }

        string[] versionDirectories = Directory.GetDirectories(robloxVersionsPath);

        if (versionDirectories.Length == 0)
        {
            MessageBox.Show("No Roblox version folders found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            DisableBlockingFeatures();
            return;
        }

        var sortedVersionDirectories = versionDirectories
            .Select(dir => new DirectoryInfo(dir))
            .OrderByDescending(dirInfo => dirInfo.LastWriteTime)
            .ToList();

        foreach (var dir in sortedVersionDirectories)
        {
            string exePath = Path.Combine(dir.FullName, "RobloxPlayerBeta.exe");
            if (File.Exists(exePath))
            {
                executablePath = exePath;
                ruleName = "Block " + Path.GetFileName(executablePath);
                NetworkManager.EnsureRuleExists(executablePath, ruleName);
                return;
            }
        }

        MessageBox.Show($"RobloxPlayerBeta.exe not found in any subfolders of:\n{robloxVersionsPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        DisableBlockingFeatures();
        return;
    }

    private void DisableBlockingFeatures()
    {
        KeyChangeButton.IsEnabled = false;
        AutoBlockerCheckBox.IsEnabled = false;
        StatusLabel.Content = "Status: Disabled";
    }

    private static string FindExecutablePath(string processName)
    {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length > 0)
        {
            return processes[0].MainModule.FileName;
        }
        else
        {
            MessageBox.Show($"Process {processName} is not running. Please select the executable.", "Process Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Filter = "Executable Files (*.exe)|*.exe"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
        }
        return null;
    }

    private void UpdateStatus()
    {
        if (string.IsNullOrEmpty(executablePath) || string.IsNullOrEmpty(ruleName))
        {
            StatusLabel.Content = "Status: Unknown";
            isBlocked = false;
            return;
        }

        if (NetworkManager.IsBlocked(executablePath, ruleName))
        {
            StatusLabel.Content = "Status: Blocked";
            isBlocked = true;
        }
        else
        {
            StatusLabel.Content = "Status: Unblocked";
            isBlocked = false;
        }
    }

    private void KeyChangeButton_Click(object sender, RoutedEventArgs e)
    {
        KeyCaptureWindow keyCaptureWindow = new KeyCaptureWindow();
        if (keyCaptureWindow.ShowDialog() == true)
        {
            selectedTrigger = keyCaptureWindow.CapturedInput;

            string triggerName = GetTriggerName(selectedTrigger);
            KeyChangeButton.Content = $"Trigger: {triggerName}";

            InitializeListener();
        }
    }

    private static string GetTriggerName(InputTrigger trigger)
    {
        if (trigger.Type == InputTrigger.TriggerType.Key)
        {
            Key key = KeyInterop.KeyFromVirtualKey(trigger.Code);
            return key.ToString();
        }
        else if (trigger.Type == InputTrigger.TriggerType.MouseButton)
        {
            return GetMouseButtonName(trigger.Code);
        }
        return "Unknown";
    }

    private static string GetMouseButtonName(int code)
    {
        return code switch
        {
            0x01 => "Left Mouse Button",
            0x02 => "Right Mouse Button",
            0x04 => "Middle Mouse Button",
            0x05 => "Mouse Button 4",
            0x06 => "Mouse Button 5",
            _ => "Unknown",
        };
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        keyboardListener?.Dispose();
        mouseButtonListener?.Dispose();
        mouseListener?.Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        keyboardListener?.Dispose();
        mouseButtonListener?.Dispose();
        mouseListener?.Dispose();
    }

    private void AutoBlockerCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        mouseListener = new MouseListener(targetProcessName, OnLeftMouseDown, OnLeftMouseUp);
        mouseListener.Start();
    }

    private void AutoBlockerCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        mouseListener?.Dispose();
        mouseListener = null;
    }

    private void OverlayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OverlayComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            overlayMode = selectedItem.Content.ToString();
        }
    }

    private void OnLeftMouseDown()
    {
        Dispatcher.Invoke(() =>
        {
            if (!isBlocked)
            {
                BlockProcessAndShowOverlay();
            }
        });
    }

    private void OnLeftMouseUp()
    {
        Dispatcher.Invoke(() =>
        {
            if (isBlocked)
            {
                UnblockProcessAndShowOverlay();
            }
        });
    }

    private async void OnKeyPressed()
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            if (isOperationInProgress)
                return;

            int durationMs = GetDurationInMilliseconds();
            if (durationMs < 0)
            {
                MessageBox.Show("Invalid duration. Please enter a valid number in milliseconds.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ExecuteBlockOperation(durationMs);
        });
    }

    private int GetDurationInMilliseconds()
    {
        if (string.IsNullOrWhiteSpace(DurationTextBox.Text))
            return 0;

        return int.TryParse(DurationTextBox.Text, out int durationMs) ? durationMs : -1;
    }

    private async Task ExecuteBlockOperation(int durationMs)
    {
        try
        {
            isOperationInProgress = true;
            DisableTriggerButton();

            BlockProcessAndShowOverlay();

            if (durationMs > 0)
                await Task.Delay(durationMs);

            UnblockProcessAndShowOverlay();
        }
        finally
        {
            EnableTriggerButton();
            isOperationInProgress = false;
        }
    }

    private void BlockProcessAndShowOverlay()
    {
        bool success = NetworkManager.SetProcessBlockState(executablePath, ruleName, true);
        if (success)
        {
            UpdateStatus();
            ShowOverlay(Colors.Green);
        }
        else
        {
            MessageBox.Show("Failed to block the network.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnblockProcessAndShowOverlay()
    {
        bool success = NetworkManager.SetProcessBlockState(executablePath, ruleName, false);
        if (success)
        {
            UpdateStatus();
            ShowOverlay(Colors.Red);
        }
        else
        {
            MessageBox.Show("Failed to unblock the network.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowOverlay(Color color)
    {
        OverlayWindow overlay = new OverlayWindow(color, overlayMode);
        overlay.Show();
    }

    private void DisableTriggerButton()
    {
        KeyChangeButton.IsEnabled = false;
    }

    private void EnableTriggerButton()
    {
        KeyChangeButton.IsEnabled = true;
    }
}
