
using System.Drawing;

using ModernWpf;

namespace cpapviewer;

public class DataColors
{
	private static uint[] _lightThemeColors = new[]
	{
		0xff1192e9, 0xff005f5d, 0xff9f1853, 0xfffa4e56, 
		0xff6929c5, 0xff197f38, 0xff002d9d, 0xffee5398, 0xffb08600, 
		0xff520609, 0xff009d9a, 0xff01274a, 0xff8c3702, 0xffa66efe
	};

	private static uint[] _darkThemeColors = new[]
	{
		0xff33b1fd, 0xff41bebb, 0xffff7eb5, 0xfffa4e54,
		0xff893ffc, 0xff6fdc8c, 0xff4689ff, 0xffd02770, 0xffd3a107,
		0xfffff2f2, 0xff09bdb9, 0xffbbe6fe, 0xffba4e00, 0xffd4bcff
	};

	private static Color[] _markerColors = new Color[]
	{
		Color.Chartreuse, Color.Orange, Color.Yellow, Color.Aqua, Color.Fuchsia, Color.BurlyWood
			
		// 0xff003f5c,
		// 0xff2f4b7c,
		// 0xff665191,
		// 0xffa05195,
		// 0xffd45087,
		// 0xfff95d6a,
		// 0xffff7c43,
		// 0xffffa600,
	};

	public static Color GetDataColor( int index )
	{
		var colors = (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark) ? _darkThemeColors : _lightThemeColors;
		
		return Color.FromArgb( unchecked( (int)colors[ index % colors.Length ] ) );
	}

	public static Color GetMarkerColor( int index )
	{
		var palette = ScottPlot.Palette.Category20.Colors;
		
		var color = palette[ index % palette.Length ].ToColor();

		if( ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark )
		{
			color = ColorTransforms.TransformBrightness( color, ColorTransforms.ColorTransformMode.Hsb, 1.1 );
		}

		return color;
	}
}
 