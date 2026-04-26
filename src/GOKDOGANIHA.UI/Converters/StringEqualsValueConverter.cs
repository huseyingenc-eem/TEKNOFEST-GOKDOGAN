using System;
using System.Globalization;
using System.Windows.Data;

namespace GOKDOGANIHA.UI.Converters;

/// <summary>
/// String property'yi `ConverterParameter`'daki değerle karşılaştırır:
/// - Convert: bound string == parameter ise true (RadioButton.IsChecked için)
/// - ConvertBack: true gelirse parameter'i geri yazar (radio seçimi → property)
///
/// Kullanım: tile provider radio listesi gibi "string enum" senaryoları için.
/// MapPanel layers popover'da kullanılır.
/// </summary>
public sealed class StringEqualsValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // RadioButton false döndüğünde mevcut değeri koru — Binding.DoNothing.
        // True dönerse parameter'i source'a yaz (selected provider).
        if (value is bool b && b) return parameter?.ToString() ?? string.Empty;
        return Binding.DoNothing;
    }
}
