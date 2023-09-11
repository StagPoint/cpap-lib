using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	public static readonly DependencyProperty TitleProperty      = DependencyProperty.Register( nameof( Title ),      typeof( string ),               typeof( SignalChart ) );
	public static readonly DependencyProperty TitleStyleProperty = DependencyProperty.Register( nameof( TitleStyle ), typeof( System.Windows.Style ), typeof( SignalChart ) );
	public static readonly DependencyProperty PlotColorProperty  = DependencyProperty.Register( nameof( PlotColor ),  typeof( Brush ),                typeof( SignalChart ) );
	public static readonly DependencyProperty FlagTypesProperty  = DependencyProperty.Register( nameof( FlagTypes ),  typeof( EventType[] ),          typeof( SignalChart ) );

	private VLine       _mouseTrackLine     = null;
	private MarkerPlot  _currentValueMarker = null;
	private DailyReport _day                = null;

	private static Dictionary<string, List<SignalChart>> _chartGroups = new();

	public EventType[] FlagTypes
	{
		get => (EventType[])GetValue( FlagTypesProperty );
		set => SetValue( FlagTypesProperty, value );
	}

	public string GroupName
	{
		get => (string)GetValue( GroupNameProperty );
		set => SetValue( GroupNameProperty, value );
	}

	public Brush PlotColor
	{
		get => (Brush) GetValue(PlotColorProperty);
		set => SetValue(PlotColorProperty, value);
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
	public string Title
	{
		get => (string)GetValue( TitleProperty );
		set => SetValue( TitleProperty, value );
	}

	/// <summary>
	/// Gets or sets the Style used to render the chart's label
	/// </summary>
	public Style TitleStyle
	{
		get => (Style)GetValue( TitleStyleProperty );
		set => SetValue( TitleStyleProperty, value );
	}

	public SignalChart()
	{
		InitializeComponent();

		this.Unloaded += OnUnloaded;
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		ChartLabel.Text  = Title;
		ChartLabel.Style = TitleStyle;

		InitializeChartProperties( Chart );
	}

	protected override void OnInitialized( EventArgs e )
	{
		base.OnInitialized( e );

		CurrentValue.Text = "";

		Chart.MouseMove    += ChartOnMouseMove;
		Chart.RightClicked -= Chart.DefaultRightClickEvent;
		Chart.AxesChanged  += ChartOnAxesChanged;

		AddToGroupList();
	}
	
	private void OnUnloaded( object sender, RoutedEventArgs e )
	{
		RemoveFromGroupList();
	}

	private void AddToGroupList()
	{
		var list = GetGroupList();
		if( list == null )
		{
			list = new List<SignalChart>();
			_chartGroups.Add( GroupName, list );
		}

		list.Add( this );
	}

	private void RemoveFromGroupList()
	{
		var list = GetGroupList();
		if( list != null )
		{
			list.Remove( this );

			if( list.Count == 0 )
			{
				_chartGroups.Remove( GroupName );
			}
		}
	}

	private List<SignalChart> GetGroupList()
	{
		if( _chartGroups.TryGetValue( GroupName, out List<SignalChart> groupList ) )
		{
			return groupList;
		}

		return null;
	}

	private void ChartOnAxesChanged( object sender, RoutedEventArgs e )
	{
		var newAxisLimits = Chart.Plot.GetAxisLimits();

		foreach( var loop in GetGroupList() )
		{
			if( loop == this )
			{
				continue;
			}

			var chart = loop.Chart;

			// disable events briefly to avoid an infinite loop
			chart.Configuration.AxesChangedEventEnabled = false;
			{
				var currentAxisLimits  = chart.Plot.GetAxisLimits();
				var modifiedAxisLimits = new AxisLimits( newAxisLimits.XMin, newAxisLimits.XMax, currentAxisLimits.YMin, currentAxisLimits.YMax );

				chart.Plot.SetAxisLimits( modifiedAxisLimits );
				chart.Render();
			}
			chart.Configuration.AxesChangedEventEnabled = true;
		}
	}

	private void ChartOnMouseMove( object sender, MouseEventArgs e )
	{
		// Returns mouse coordinates as grid coordinates, taking pan and zoom into account
		(double mouseCoordX, double mouseCoordY) = Chart.GetMouseCoordinates();

		// Synchronize the update of the vertical indicator in all charts in the group
		foreach( var chart in GetGroupList() )
		{
			chart.UpdateSelectedTime( mouseCoordX );
		}
	}

	private void UpdateSelectedTime( double time )
	{
		_mouseTrackLine.X     = time;
		_currentValueMarker.X = time;
		CurrentValue.Text     = "";

		// Converting the "Number of seconds offset from the start of the chart" back to a DateTime makes it 
		// much easier to locate which Session this time refers to, and to then calculate an offset into that
		// session.
		var asDateTime = _day.RecordingStartTime.AddSeconds( time );

		foreach( var session in _day.Sessions )
		{
			// Check to see if the time overlaps with a session
			if( session.StartTime <= asDateTime && session.EndTime >= asDateTime )
			{
				var signal = session.GetSignalByName( SignalName );
				if( signal == null )
				{
					continue;
				}

                // Signal start times may be slightly different than session start times, so need to check 
                // the signal itself also 
                if ( signal.StartTime <= asDateTime && signal.EndTime >= asDateTime )
				{
					// The offset calculation can still result in an "off by one", so need to ensure that it's 
					// within limits also
					var offset = (int)(asDateTime.Subtract( session.StartTime ).TotalSeconds * signal.FrequencyInHz);
					if( offset < signal.Samples.Count )
					{
						var value  = signal.Samples[ offset ];

						CurrentValue.Text = $"{asDateTime:T}        {Title}: {value:N2} {signal.UnitOfMeasurement}";
						_currentValueMarker.Y = value;

						break;
					}
				}
			}
		}
		
		Chart.Refresh();
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

				_day = day;

				ChartSignal( Chart, day, SignalName );
				
				_mouseTrackLine = Chart.Plot.AddVerticalLine( 0, Color.Silver, 2f, LineStyle.Dot );
				_mouseTrackLine.PositionLabel           = false;
				_mouseTrackLine.PositionLabelAxis       = Chart.Plot.XAxis;
				_mouseTrackLine.DragEnabled             = false;
				_mouseTrackLine.PositionLabel           = false;
				_mouseTrackLine.PositionLabelBackground = Color.FromArgb( 32, 32, 32 );
				_mouseTrackLine.PositionFormatter       = x => DateTime.FromFileTime( (long)x ).ToString( "hh:mm:ss tt" );

				// TODO: The "Current Value" marker dot is currently not visible. 
				_currentValueMarker           = Chart.Plot.AddMarker( -1, -1, MarkerShape.filledCircle, 8, Color.White, null );
				_currentValueMarker.IsVisible = false;
			}
		}
	}

	private void InitializeChartProperties( WpfPlot chart )
	{
		var chartStyle = new CustomChartStyle( this );
		var plot       = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "8888.8", chartStyle.TickLabelFontName, 12 );

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
		plot.XAxis2.Layout( 8, 1, 1 );

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

		// Need to keep track of the first session added to the chart so that we can set that 
		// section's Label (for the chart legend). Otherwise, it will be duplicated for each 
		// session. 
		bool firstSessionAdded = true;

		// Keeping track of the chart's index in the group is an easy way to assign automatic
		// chart colors. 
		var chartIndex = GetGroupList().IndexOf( this );

		foreach( var session in day.Sessions )
		{
			var signal = session.GetSignalByName( signalName );

			minValue = Math.Min( minValue, signal.MinValue * signalScale );
			maxValue = Math.Max( maxValue, signal.MaxValue * signalScale );

			offset  = (signal.StartTime - day.RecordingStartTime).TotalSeconds;
			endTime = (signal.EndTime - day.RecordingStartTime).TotalSeconds;

			var chartColor = DataColors.GetDataColor( chartIndex );

			// If a custom color has been chosen, use that instead
			var plotColorSource = DependencyPropertyHelper.GetValueSource( this, PlotColorProperty );
			if( PlotColor is SolidColorBrush && plotColorSource.BaseValueSource != BaseValueSource.Default )
			{
				chartColor = ((SolidColorBrush)PlotColor).Color.ToDrawingColor();
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
		else
		{
			var range           = maxValue - minValue;
			var automaticLabels = new double[] { minValue, minValue + range * 0.25, minValue + range * 0.5, minValue + range * 0.75, maxValue };
			var labels          = new string[ 5 ];
			
			for( int i = 0; i < labels.Length; i++ )
			{
				labels[ i ] = automaticLabels[ i ].ToString( "F1" );
			}
			
			chart.Plot.YAxis.ManualTickPositions( automaticLabels, labels );
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

