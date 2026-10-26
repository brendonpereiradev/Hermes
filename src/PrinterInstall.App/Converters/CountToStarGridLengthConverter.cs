using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrinterInstall.App.Converters;

/// <summary>
/// Converte uma contagem em largura proporcional de coluna (N*), para barras divididas por resultado.
/// </summary>
public sealed class CountToStarGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 0 ? new GridLength(count, GridUnitType.Star) : new GridLength(0);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
