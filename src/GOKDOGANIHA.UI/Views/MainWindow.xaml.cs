using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GOKDOGANIHA.UI.ViewModels;

namespace GOKDOGANIHA.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as MainWindowViewModel)?.Dispose();
    }

    /// <summary>
    /// Map FAB butonuna tıklandığında popover'in placement'ını butonun pencere
    /// yarısına göre belirler — alt yarıdaysa yukarı (Top), üst yarıdaysa aşağı (Bottom).
    /// Buton ileride başka konuma taşınırsa kod kendiliğinden uyar.
    /// </summary>
    private void OnMapMenuClick(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton btn) return;
        var pos = btn.TransformToAncestor(this).Transform(new Point(0, 0));
        var halfHeight = ActualHeight / 2;
        if (pos.Y > halfHeight)
        {
            MapMenuPopup.Placement = PlacementMode.Top;
            MapMenuPopup.VerticalOffset = -6;
        }
        else
        {
            MapMenuPopup.Placement = PlacementMode.Bottom;
            MapMenuPopup.VerticalOffset = 6;
        }
    }
}
