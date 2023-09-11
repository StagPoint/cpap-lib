using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using cpaplib;

using ScottPlot;
using ScottPlot.Plottable;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Style = System.Windows.Style;

namespace example_viewer.Controls;

public partial class SignalChart
{
	public static readonly DependencyProperty SignalNameProperty = DependencyProperty.Register( nameof( SignalName ), typeof( string ),               typeof( SignalChart ) );
	public static readonly DependencyProperty GroupNameProperty  = DependencyProperty.Register( nameof( GroupName ),  typeof( string ),               typeof( SignalChart ) );
	public static readonly DependencyProperty LabelProperty      = DependencyProperty.Register( nameof( Label ),      typeof( string ),               typeof( SignalChart ) );
	public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register( nameof( LabelStyle ), typeof( System.Windows.Style ), typeof( SignalChart ) );
	public static readonly DependencyProperty PlotColorProperty  = DependencyProperty.Register( nameof( PlotColor ),  typeof( SolidColorBrush ),           typeof( SignalChart ) );

	private VLine _mouseTrackLine = null;

	private static Dictionary<string, List<SignalChart>> _chartGroups = new();

	public string GroupName
	{
		get => (string)GetValue( GroupNameProperty );
		set => SetValue( GroupNameProperty, value );
	}
	
	public SolidColorBrush PlotColor
	{
		get => (SolidColorBrush)GetValue( PlotColorProperty );
		set => SetValue( PlotColorProperty, value );
	}

	/// <summary>
	/// The name of the Signal that will be displayed in the chart
	/// </summary>
	public string SignalName
	{
		get => (string)GetValue( SignalNameProperty );
		set => SetValue( SignalNameProperty, value );
	}

	/// <summary>
	/// Gets or sets the text that will be displayed in the chart's label
	/// </summary>
	public string Label
	{
		get => (string)GetValue( LabelProperty );
		set => SetValue( LabelProperty, value );
	}

	/// <summary>
	/// Gets or sets the Style used to render the chart's label
	/// </summary>
	public Style LabelStyle
	{
		get => (Style)GetValue( LabelStyleProperty );
		set => SetValue( LabelStyleProperty, value );
	}

	public SignalChart()
	{
		InitializeComponent();

		this.Unloaded += OnUnloaded;
	}
	
	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		ChartLabel.Text  = Label;
		ChartLabel.Style = LabelStyle;

		InitializeChartProperties( Chart );
	}

	protected override void OnInitialized( EventArgs e )
	{
		base.OnInitialized( e );

		CurrentValue.Text = "";
		
		Chart.MouseMove += ChartOnMouseMove;

		if( !_chartGroups.TryGetValue( this.GroupName, out var groupList ) )
		{
			groupList                 = new List<SignalChart>();
			_chartGroups[ GroupName ] = groupList;
		}

		groupList.Add( this );
	}
	
	private void OnUnloaded( object sender, RoutedEventArgs e )
	{
		if( _chartGroups.TryGetValue( this.GroupName, out List<SignalChart> groupList ) )
		{
			groupList.Remove( this );
		}
	}

	private void ChartOnMouseMove( object sender, MouseEventArgs e )
	{
		var chart = sender as WpfPlot;
		if( chart == null )
		{
			return;
		}

		// Returns mouse coordinates as grid coordinates, taking pan and zoom into account
		(double mouseCoordX, double mouseCoordY) = chart.GetMouseCoordinates();

		// Synchronize the update of the vertical indicator in all charts in the group
		if( _chartGroups.TryGetValue( GroupName, out List<SignalChart> groupList ) )
		{
			foreach( var loop in groupList )
			{
				if( loop._mouseTrackLine != null )
				{
					loop._mouseTrackLine.X = mouseCoordX;
					loop.Chart.Refresh();
				}
			}
		}
	}

	protected override void OnPropertyChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnPropertyChanged( e );

		if( e.Property.Name == nameof( DataContext ) )
		{
			if( DataContext is DailyReport day )
			{
				if( string.IsNullOrEmpty( SignalName ) )
				{
					throw new NullReferenceException( "No Signal name was provided" );
				}

				ChartSignal( Chart, day, SignalName );
				
				_mouseTrackLine = Chart.Plot.AddVerticalLine( 0, Color.Silver, 2f, LineStyle.Dot );
				_mouseTrackLine.PositionLabel           = false;
				_mouseTrackLine.PositionLabelAxis       = Chart.Plot.XAxis;
				_mouseTrackLine.DragEnabled             = false;
				_mouseTrackLine.PositionLabel           = false;
				_mouseTrackLine.PositionLabelBackground = Color.FromArgb( 32, 32, 32 );
				_mouseTrackLine.PositionFormatter       = x => DateTime.FromFileTime( (long)x ).ToString( "hh:mm:ss tt" );
			}
		}
	}

	private void ChartOnAxesChanged( object sender, RoutedEventArgs e )
	{
	}

	private void InitializeChartProperties( WpfPlot chart )
	{
		var chartStyle = new CustomChartStyle( this );
		var plot       = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "8888.8", chartStyle.TickLabelFontName, 12 );

		chart.RightClicked -= chart.DefaultRightClickEvent;
		chart.AxesChanged += ChartOnAxesChanged;
		//chart.Configuration.ScrollWheelZoom =  false;

		chart.Configuration.Quality                                      = ScottPlot.Control.QualityMode.High;
		chart.Configuration.QualityConfiguration.BenchmarkToggle         = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.AutoAxis                = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDragged = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseInteractiveDropped = RenderType.HighQuality;
		chart.Configuration.QualityConfiguration.MouseWheelScrolled      = RenderType.HighQuality;

		plot.Style( chartStyle );
		//plot.LeftAxis.Label( label );
		plot.Layout( 0, 0, 0, 0 );
		
		plot.XAxis.MinimumTickSpacing( 1f );
		plot.XAxis.SetZoomInLimit( 60 ); // Make smallest zoom window possible be 1 minute 
		plot.XAxis.Layout( padding: 0 );
		plot.XAxis.AxisTicks.MajorTickLength = 15;
		plot.XAxis.AxisTicks.MinorTickLength = 5;
		plot.XAxis2.Layout( 0, 1, 1 );

		plot.YAxis.TickDensity( 1f );
		plot.YAxis.Layout( 0, maximumLabelWidth, maximumLabelWidth );
		plot.YAxis2.Layout( 0, 5, 5 );

		var legend = plot.Legend();
		legend.Location     = Alignment.UpperRight;
		legend.Orientation  = ScottPlot.Orientation.Horizontal;
		legend.OutlineColor = chartStyle.TickMajorColor;
		legend.FillColor    = chartStyle.DataBackgroundColor;
		legend.FontColor    = chartStyle.TitleFontColor;

		chart.Configuration.LockVerticalAxis = true;
		
		chart.Refresh();
	}
	
	private void ChartSignal( WpfPlot chart, DailyReport day, string signalName, float signalScale = 1f, float? axisMinValue = null, float? axisMaxValue = null, double[] manualLabels = null )
	{
		chart.Plot.Clear();

		var minValue = axisMinValue ?? double.MaxValue;
		var maxValue = axisMaxValue ?? double.MinValue;

		double offset  = 0;
		double endTime = 0;

		int  signalIndex       = -1;
		bool firstSessionAdded = true;

		foreach( var session in day.Sessions )
		{
			if( signalIndex == -1 )
			{
				signalIndex = session.Signals.FindIndex( x => x.Name.Equals( signalName, StringComparison.OrdinalIgnoreCase ) );
				if( signalIndex == -1 )
				{
					throw new KeyNotFoundException( $"Could not find a Signal named {signalName}" );
				}
			}

			var signal = session.Signals[ signalIndex ];

			minValue = Math.Min( minValue, signal.MinValue * signalScale );
			maxValue = Math.Max( maxValue, signal.MaxValue * signalScale );

			offset  = (signal.StartTime - day.RecordingStartTime).TotalSeconds;
			endTime = (signal.EndTime - day.RecordingStartTime).TotalSeconds;

			var chartColor = DataColors.GetDataColor( signalIndex );

			// If a custom color has been chosen, use that instead
			var plotColorSource = DependencyPropertyHelper.GetValueSource( this, PlotColorProperty );
			if( plotColorSource.BaseValueSource != BaseValueSource.Default )
			{
				chartColor = PlotColor.Color.ToDrawingColor();
			}

			var graph = chart.Plot.AddSignal( signal.Samples.ToArray(), signal.FrequencyInHz, chartColor, firstSessionAdded ? signal.Name : null );
			graph.OffsetX    = offset;
			graph.MarkerSize = 0;
			graph.ScaleY     = signalScale;

			firstSessionAdded = false;
		}

		chart.Plot.XAxis.TickLabelFormat( x => $"{day.RecordingStartTime.AddSeconds( x ):hh:mm:ss tt}" );

		// Set zoom and boundary limits
		chart.Plot.YAxis.SetBoundary( minValue, maxValue );
		chart.Plot.XAxis.SetBoundary( -1, day.Duration.TotalSeconds + 1 );
		chart.Plot.SetAxisLimits( -1, day.Duration.TotalSeconds + 1, minValue, maxValue );

		// If manual vertical axis tick positions were provided, set up the labels for them and force the chart
		// to show those instead of the automatically-generated tick positions. 
		if( manualLabels != null && manualLabels.Length > 0 )
		{
			var labels = new string[ manualLabels.Length ];
			for( int i = 0; i < labels.Length; i++ )
			{
				labels[ i ] = manualLabels[ i ].ToString( "F0" );
			}
			
			chart.Plot.YAxis.ManualTickPositions( manualLabels, labels );
		}

		chart.Refresh();
	}

	private float MeasureText( string text, string fontFamily, float emSize )
	{
		FormattedText formatted = new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			new Typeface( fontFamily ),
			emSize,
			Brushes.Black,
			VisualTreeHelper.GetDpi( this ).PixelsPerDip
		);

		return (float)Math.Ceiling( formatted.Width );
	}

}

