using Rectangle = System.Windows.Shapes.Rectangle;
using Color = System.Windows.Media.Color;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;

namespace RobloxLagswitch;

public enum OverlayMode
{
    Outline,
    Dot
}

public partial class OverlayWindow : Window
{
    private readonly OverlayMode overlayMode;

    public OverlayWindow(Color overlayColor, string overlayModeStr)
    {
        InitializeComponent();
        if (!Enum.TryParse(overlayModeStr, out overlayMode))
        {
            overlayMode = OverlayMode.Outline;
        }
        InitializeOverlay(overlayColor);
        CloseAfterDelay();
    }

    private void InitializeOverlay(Color overlayColor)
    {
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        if (overlayMode == OverlayMode.Outline)
        {
            Rectangle rect = new Rectangle
            {
                Stroke = new SolidColorBrush(overlayColor),
                StrokeThickness = 10,
                Width = this.Width,
                Height = this.Height
            };
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            OverlayCanvas.Children.Add(rect);
        }
        else if (overlayMode == OverlayMode.Dot)
        {
            Ellipse dot = new Ellipse
            {
                Fill = new SolidColorBrush(overlayColor),
                Width = 50,
                Height = 50
            };
            Canvas.SetLeft(dot, (this.Width - dot.Width) / 2);
            Canvas.SetTop(dot, (this.Height - dot.Height) / 2);
            OverlayCanvas.Children.Add(dot);
        }
    }

    private async void CloseAfterDelay()
    {
        await Task.Delay(2000);
        Close();
    }
}
