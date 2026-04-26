using System;
using System.Globalization;
using System.Windows.Data;

namespace GOKDOGANIHA.UI.Converters;

/// <summary>
/// İki string'i case-insensitive karşılaştıran multi-binding converter.
/// CommandsPanel'da "ButonTag == FlightMode" testi için kullanılır.
/// SOLID/DRY: tek converter, 6 mod butonu paylaşır.
/// </summary>
public sealed class StringEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2) return false;
        var a = values[0]?.ToString();
        var b = values[1]?.ToString();
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
