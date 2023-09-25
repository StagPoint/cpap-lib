using System.Drawing;

namespace cpap_app.Helpers;

public static class ColorExtensions
{
	public static System.Drawing.Color ToDrawingColor( this Avalonia.Media.Color color )
	{
		return System.Drawing.Color.FromArgb( color.A, color.R, color.G, color.B );
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
