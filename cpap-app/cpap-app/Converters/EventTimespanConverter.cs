using System;
using System.Globalization;

using Avalonia.Data.Converters;

using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.Core;

namespace cpap_app.Converters;

public class EventTimespanConverter : IValueConverter
{

	public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		if( value is EventTypeSummary summary )
		{
			if( summary.Type == EventType.LargeLeak || EventTypes.Breathing.Contains( summary.Type ) )
			{
				return $"{summary.PercentTime:P2}";
			}

			return $@"{summary.TotalTime:hh\:mm\:ss}";
		}

		return value?.ToString();
	}

	public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
	{
		throw new NotSupportedException( $"Two-way binding is not supported for {nameof( EventTimespanConverter )}" );
	}
}
