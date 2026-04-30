using System.Globalization;
using Microsoft.Maui.Controls;

namespace LockPaper.Ui.Misc.Converters;

public sealed class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Brush brush)
        {
            return brush;
        }

        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        if (value is string colorText && !string.IsNullOrWhiteSpace(colorText))
        {
            return new SolidColorBrush(Color.FromArgb(colorText));
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
