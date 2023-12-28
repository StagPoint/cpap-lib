using System;
using System.Globalization;

using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

using cpap_app.Helpers;

using AvaloniaColor = Avalonia.Media.Color;
using DrawingColor = System.Drawing.Color;

namespace cpap_app.Converters;

public class AvaloniaColorConverter : IValueConverter
{
	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return ((DrawingColor)(value ?? DrawingColor.Red)).ToAvaloniaColor();
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		return ((AvaloniaColor)((value) ?? throw new InvalidOperationException())).ToDrawingColor();
	}
}
