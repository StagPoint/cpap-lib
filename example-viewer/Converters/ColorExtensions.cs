using System.Security.AccessControl;

using ScottPlot;

namespace example_viewer;

public static class ColorExtensions
{
	public static System.Drawing.Color ToPlotColor( this System.Windows.Media.Color color )
	{
		return System.Drawing.Color.FromArgb( color.A, color.R, color.G, color.B );
	}
}
