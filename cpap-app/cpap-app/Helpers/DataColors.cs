using System;

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace cpap_app.Helpers;

public class DataColors
{
	public static readonly uint[] LightThemeColors = new[]
	{
		0xff1192e9, 0xff005f5d, 0xff9f1853, 0xfffa4e56, 
		0xff6929c5, 0xff197f38, 0xff002d9d, 0xffee5398, 0xffb08600, 
		0xff520609, 0xff009d9a, 0xff01274a, 0xff8c3702, 0xffa66efe
	};

	public static readonly uint[] DarkThemeColors = new[]
	{
		0xff33b1fd, 0xff41bebb, 0xffff7eb5, 0xfffa4e54,
		0xff893ffc, 0xff6fdc8c, 0xff4689ff, 0xffd02770, 0xffd3a107,
		0xfffff2f2, 0xff09bdb9, 0xffbbe6fe, 0xffba4e00, 0xffd4bcff
	};

	private static uint[] _markerColors = new uint[]
	{
		//Color.Chartreuse, Color.Orange, Color.Yellow, Color.Aqua, Color.Fuchsia, Color.BurlyWood
			
		0xff003f5c,
		0xff2f4b7c,
		0xff665191,
		0xffa05195,
		0xffd45087,
		0xfff95d6a,
		0xffff7c43,
		0xffffa600,
	};

	public static Color GetDarkThemeColor( int index )
	{
		return Color.FromUInt32( DarkThemeColors[ index % DarkThemeColors.Length ] );
	}

	public static Color GetLightThemeColor( int index )
	{
		return Color.FromUInt32( LightThemeColors[ index % LightThemeColors.Length ] );
	}

	public static Color GetDataColor( int index )
	{
		var isDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		var colors      = isDarkTheme ? DarkThemeColors : LightThemeColors;
		
		return Color.FromUInt32( colors[ index % colors.Length ] );
	}

	public static Color GetMarkerColor( int index )
	{
		//var color = Color.FromUInt32( _markerColors[ index % _markerColors.Length ] );
		
		var isDarkTheme  = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
		var colorPalette = _markerColors; // isDarkTheme ? DarkThemeColors : LightThemeColors;

		// TODO: Marker colors look better on both themes when they are darker? Consider finding a new marker color palette. 
		{
			var avaloniaColor = Avalonia.Media.Color.FromUInt32( colorPalette[ index % colorPalette.Length ] );
			var hsl           = avaloniaColor.ToHsl();
			var brighter      = new HslColor( hsl.A, hsl.H, hsl.S, Math.Max( hsl.L * 0.75f, 0.33f ) );
			var result        = brighter.ToRgb();

			return result;
		}

		var color = Color.FromUInt32( colorPalette[ index % colorPalette.Length ] );
	
		return color;
	}
}
