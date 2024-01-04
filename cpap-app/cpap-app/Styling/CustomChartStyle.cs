using Avalonia.Media;

using cpap_app.Helpers;

using Color = System.Drawing.Color;

namespace cpap_app.Styling;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

public class CustomChartStyle : ScottPlot.Styles.Default
{
	#region Static fields

	public static readonly CustomChartStyle ChartPrintStyle = new CustomChartStyle( Color.Black, Color.White, Color.Black, Color.Gray.MultiplyAlpha( 0.5f ) );
	
	#endregion 
	
	#region Public properties 
	
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
	
	#endregion 
	
	#region Constructor

	public CustomChartStyle( Color foreground, Color background, Color borderColor, Color gridLineColor )
	{
		var fontName = FontManager.Current.DefaultFontFamily.Name;
		
		FigureBackgroundColor = background;
		DataBackgroundColor   = background;
			
		FrameColor     = borderColor;
		AxisLabelColor = foreground;
		TitleFontColor = foreground;
		TickLabelColor = foreground;

		GridLineColor  = gridLineColor.MultiplyAlpha( 0.35f );
		TickMajorColor = gridLineColor;
		TickMinorColor = gridLineColor;

		TickLabelFontName = fontName;
		AxisLabelFontName = fontName;
		TitleFontName     = fontName;
	}
	
	public CustomChartStyle( IBrush foreground, IBrush background, IBrush borderColor, IBrush gridLineColor )
	{
		var foreColor       = ((ISolidColorBrush)foreground).Color.ToDrawingColor();
		var border          = ((ISolidColorBrush)borderColor).Color.ToDrawingColor();
		var midColor        = ((ISolidColorBrush)gridLineColor).Color.ToDrawingColor().MultiplyAlpha( 0.5f );
		var backgroundColor = ((ISolidColorBrush)background).Color.ToDrawingColor();
		var fontName        = FontManager.Current.DefaultFontFamily.Name;

		FigureBackgroundColor = Color.Transparent;
		DataBackgroundColor   = backgroundColor;
			
		FrameColor     = border;
		AxisLabelColor = foreColor;
		TitleFontColor = foreColor;
		TickLabelColor = foreColor;

		GridLineColor  = midColor.MultiplyAlpha( 0.35f );
		TickMajorColor = midColor;
		TickMinorColor = midColor;

		TickLabelFontName = fontName;
		AxisLabelFontName = fontName;
		TitleFontName     = fontName;
	}
	
	#endregion 
}
