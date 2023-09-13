using System.Linq;
using System.Windows;
using System.Windows.Media;

using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;

namespace example_viewer.Controls;

public class CustomChartStyle : ScottPlot.Styles.Default
{
	public override Color FrameColor            { get; }
	public override Color AxisLabelColor        { get; }
	public override Color DataBackgroundColor   { get; }
	public override Color FigureBackgroundColor { get; }
	public override Color GridLineColor         { get; }
	public override Color TickLabelColor        { get; }
	public override Color TickMajorColor        { get; }
	public override Color TickMinorColor        { get; }
	public override Color TitleFontColor        { get; }
		
	public override string TickLabelFontName { get; }
	public override string AxisLabelFontName { get; }
	public override string TitleFontName     { get; }
	
	public CustomChartStyle( FrameworkElement theme )
	{
		var foreColor       = ((SolidColorBrush)theme.FindResource( "SystemControlForegroundBaseHighBrush" )).Color.ToDrawingColor();
		var midColor        = ((SolidColorBrush)theme.FindResource( "SystemControlBackgroundBaseLowBrush" )).Color.ToDrawingColor();
		var backgroundColor = ((SolidColorBrush)theme.FindResource( "SystemControlBackgroundAltHighBrush" )).Color.ToDrawingColor();
		var fontName        = ((System.Windows.Media.FontFamily)theme.FindResource( "ContentControlThemeFontFamily" )).FamilyNames.Values!.First();

		FigureBackgroundColor = Color.Transparent;
		DataBackgroundColor   = backgroundColor;
			
		FrameColor     = foreColor;
		AxisLabelColor = foreColor;
		TitleFontColor = foreColor;
		TickLabelColor = foreColor;

		GridLineColor  = midColor.MultiplyAlpha( 0.8f );
		TickMajorColor = midColor;
		TickMinorColor = midColor;

		TickLabelFontName = fontName;
		AxisLabelFontName = fontName;
		TitleFontName     = fontName;
	}
		
}
