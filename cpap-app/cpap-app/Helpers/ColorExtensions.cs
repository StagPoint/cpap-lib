using System.Drawing;

using FluentAvalonia.UI.Media;

namespace cpap_app.Helpers;

public static class ColorExtensions
{
	public static string ToHex( this System.Drawing.Color c )
		=> $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

	public static string ToRGB( this System.Drawing.Color c )
		=> $"RGB({c.R},{c.G},{c.B})";
	
	public static string ToHex( this Avalonia.Media.Color c )
		=> $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

	public static string ToRGB( this Avalonia.Media.Color c )
		=> $"RGB({c.R},{c.G},{c.B})";
	
	public static Color2 ToColor2( this System.Drawing.Color color )
	{
		return new Color2( color.R, color.G, color.B, color.A );
	}

	public static Color ToDrawingColor( this Color2 color )
	{
		return Color.FromArgb( color.A, color.R, color.G, color.B );
	}
	
	public static System.Drawing.Color ToDrawingColor( this Avalonia.Media.Color color )
	{
		return System.Drawing.Color.FromArgb( color.A, color.R, color.G, color.B );
	}

	public static Avalonia.Media.Color ToAvaloniaColor( this System.Drawing.Color color )
	{
		return Avalonia.Media.Color.FromArgb( color.A, color.R, color.G, color.B );
	}

	public static System.Drawing.Color ToColor( this ScottPlot.SharedColor color )
	{
		return Color.FromArgb( color.A, color.R, color.G, color.B );
	}

	public static Avalonia.Media.Color MultiplyAlpha( this Avalonia.Media.Color color, float multiplier )
	{
		return new Avalonia.Media.Color( (byte)(color.A * multiplier), color.R, color.G, color.B );
	}

	public static System.Drawing.Color MultiplyAlpha( this System.Drawing.Color color, float multiplier )
	{
		return Color.FromArgb( (int)(color.A * multiplier), color.R, color.G, color.B );
	}

	public static System.Drawing.Color SetAlpha( this System.Drawing.Color color, int alpha )
	{
		return Color.FromArgb( alpha, color.R, color.G, color.B );
	}
}
