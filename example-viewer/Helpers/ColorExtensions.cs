using System.Drawing;
using System.Security.AccessControl;

using ScottPlot;

namespace example_viewer;

public static class ColorExtensions
{
	public static System.Drawing.Color ToDrawingColor( this System.Windows.Media.Color color )
	{
		return System.Drawing.Color.FromArgb( color.A, color.R, color.G, color.B );
	}

	public static System.Drawing.Color MultiplyAlpha( this System.Drawing.Color color, float alpha )
	{
		return Color.FromArgb( (int)(color.A * alpha), color.R, color.G, color.B );
	}

	public static System.Drawing.Color SetAlpha( this System.Drawing.Color color, int alpha )
	{
		return Color.FromArgb( alpha, color.R, color.G, color.B );
	}
}
