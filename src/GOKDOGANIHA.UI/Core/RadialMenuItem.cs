using System.Windows.Input;
using System.Windows.Media;
using Material.Icons;

namespace GOKDOGANIHA.UI.Core;

public class RadialMenuItem
{
    public MaterialIconKind IconKind { get; set; }
    public string? Label { get; set; }
    public IconButtonVariant Variant { get; set; } = IconButtonVariant.Primary;
    public Brush? IconBrush { get; set; }
    public ICommand? Command { get; set; }
}
