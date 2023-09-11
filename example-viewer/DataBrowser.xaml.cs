using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using cpaplib;

using example_viewer.Controls;
using example_viewer.Helpers;

using ScottPlot;
using ScottPlot.Control;

using Orientation = ScottPlot.Orientation;

namespace example_viewer;

public partial class DataBrowser
{
	private ResMedDataLoader _data = null;
	private string _dataPath = String.Empty;

	private DailyReport _selectedDay = null;

	private List<WpfPlot> _charts = new();

	public DataBrowser( string dataPath )
	{
		InitializeComponent();

		// Save the path for when the OnPageLoaded handler executes 
		_dataPath = dataPath;

		this.Loaded += OnPageLoaded;

		calendar.SelectedDate       = DateTime.MinValue;
		calendar.IsTodayHighlighted = false;
		calendar.SelectedDateChanged += CalendarOnSelectedDateChanged;

		scrollStatistics.Visibility = Visibility.Hidden;
		
		this.SizeChanged += OnSizeChanged;
	}
	
	private void OnPageLoaded( object sender, RoutedEventArgs e )
	{
		var startTime = Environment.TickCount;
		
		_data = new ResMedDataLoader();
		_data.LoadFromFolder( _dataPath );

		var elapsed = Environment.TickCount - startTime;
		Debug.WriteLine( $"Time to load CPAP data ({_data.Days.Count} days): {elapsed/1000.0f:F3} seconds" );

		// It shouldn't be possible to load this page without a valid path, but if it happened anyways
		// go back to the Welcome screen.
		if( _data.Days.Count == 0 )
		{
			NavigationService!.Navigate( new WelcomeNotice() );
			NavigationService.RemoveBackEntry();
			return;
		}
			
		var selectedDay = _data.Days.LastOrDefault();
		if( selectedDay != null )
		{
			calendar.SelectedDate = selectedDay.ReportDate.Date;
		}
		
		scrollGraphs.PreviewMouseWheel += ScrollGraphsOnPreviewMouseWheel;
	}
	
	private void CalendarOnSelectedDateChanged( object sender, SelectionChangedEventArgs e )
	{
		foreach( var day in _data.Days )
		{
			if( day.ReportDate.Date == calendar.SelectedDate )
			{
				LoadDay( day );
				return;
			}
		}

		_selectedDay = null;
		
		scrollStatistics.Visibility   = Visibility.Hidden;
		pnlCharts.Visibility          = Visibility.Hidden;
		pnlNoDataAvailable.Visibility = Visibility.Visible;
	}
	
	private void LoadDay( DailyReport day )
	{
		_selectedDay = day;
		
		scrollStatistics.Visibility   = Visibility.Visible;
		pnlCharts.Visibility          = Visibility.Visible;
		pnlNoDataAvailable.Visibility = Visibility.Hidden;
		
		DataContext                         = day;
		MachineID.DataContext               = _data.MachineID;
		RespiratoryEventSummary.DataContext = day.EventSummary;
		StatisticsSummary.DataContext       = day.Statistics;
		MachineSettings.DataContext         = day.Settings;

		SessionList.Children.Clear();
		SessionList.RowDefinitions.Clear();
		foreach( var session in day.Sessions )
		{
			AddToSessionList( session );
		}
		
		int[] annotationTypesSeen = new int[ 256 ];

		foreach( var annotation in day.Events )
		{
			var x = (annotation.StartTime - day.RecordingStartTime).TotalSeconds;
			
			var annotationType = (int)annotation.Type;
			var eventColor     = DataColors.GetMarkerColor( 9 + annotationType );
			var durationColor  = eventColor.SetAlpha( 64 );
		
			string label = annotationTypesSeen[ annotationType ] == 0 ? annotation.Description : null;
			annotationTypesSeen[ annotationType ] = 1;

			var children = this.FindVisualChildren<SignalChart>();
			foreach( var child in children )
			{
				if( child.FlagTypes == null || !child.FlagTypes.Contains( annotation.Type ) )
				{
					continue;
				}
				
				child.Chart.Plot.AddVerticalLine( x, eventColor, 1, LineStyle.Solid );
				// graphBreathing.Plot.AddTooltip( annotation.Description, x, -100 );

				if( annotation.Duration > 0 )
				{
					child.Chart.Plot.AddHorizontalSpan( x - annotation.Duration, x, durationColor );
				}
			}
		}
		
	}

	private void ScrollGraphsOnPreviewMouseWheel( object sender, MouseWheelEventArgs e )
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		bool         isShiftDown  = Keyboard.IsKeyDown( Key.LeftShift ) || Keyboard.IsKeyDown( Key.RightShift );
		bool         isControlDown  = Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl );

		if( isShiftDown )
		{
			// Apparently scrolling to the current vertical offset causes the scroll panel to consider the job done,
			// and the event gets passed down to the chart which will then zoom in or out. 
			scrollViewer.ScrollToVerticalOffset( scrollViewer.VerticalOffset );
		}
		else 
		{
			// manually scroll the window then mark the event as handled so any chart under the mouse does not zoom
			double scrollOffset = scrollViewer.VerticalOffset - (e.Delta * .25);
			scrollViewer.ScrollToVerticalOffset(scrollOffset);
			
			e.Handled = true;
		}
	}

	private void AddToSessionList( MaskSession session )
	{
		var row = new RowDefinition();
		SessionList.RowDefinitions.Add( row );
		int rowIndex = SessionList.RowDefinitions.Count - 1;

		var text = new TextBlock() { Text = $"{session.StartTime:d}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 0 );

		text           =  new TextBlock() { Text = $"{session.StartTime:t} - {session.EndTime:t}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 2 );

		text           =  new TextBlock() { Text = $"{session.Duration:g}" };
		text.MouseDown += SessionsList_RowOnMouseDown;
		text.Tag       =  session;
		
		SessionList.Children.Add( text );
		Grid.SetRow( text, rowIndex );
		Grid.SetColumn( text, 4 );
	}

	private void SessionsList_RowOnMouseDown( object sender, MouseButtonEventArgs e )
	{
		var session   = (MaskSession)((TextBlock)sender).Tag;
		var startTime = (session.StartTime - _selectedDay.RecordingStartTime).TotalSeconds;
		var endTime   = (session.EndTime - _selectedDay.RecordingStartTime).TotalSeconds;

		// TODO: Shouldn't be accessing the SignalChart.Chart property directly
		// Note: Only need to zoom the first chart found, any chart, and it will handle synchronization with the rest. 
		var graph = this.FindVisualChildren<SignalChart>().FirstOrDefault();
		if( graph != null )
		{
			graph.Chart.Plot.SetAxisLimitsX( startTime, endTime);
			graph.Chart.Refresh();
		}
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

	private void InitializeChartProperties( WpfPlot chart )
	{
		_charts.Add( chart );
		
		var chartStyle = new CustomChartStyle( this );
		var plot       = chart.Plot;
		
		// Measure enough space for a vertical axis label, padding, and the longest anticipated tick label 
		var maximumLabelWidth = MeasureText( "8888.8", chartStyle.TickLabelFontName, (float)12 );

		chart.RightClicked -= chart.DefaultRightClickEvent;
		chart.AxesChanged += ChartOnAxesChanged;
		//chart.Configuration.ScrollWheelZoom =  false;

		chart.Configuration.Quality                                      = QualityMode.High;
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
		legend.Orientation  = Orientation.Horizontal;
		legend.OutlineColor = chartStyle.TickMajorColor;
		legend.FillColor    = chartStyle.DataBackgroundColor;
		legend.FontColor    = chartStyle.TitleFontColor;

		chart.Configuration.LockVerticalAxis = true;
		
		chart.Refresh();
	}
	
	private void ChartOnAxesChanged( object sender, RoutedEventArgs e )
	{
		WpfPlot changedPlot = (WpfPlot)sender;

		var newAxisLimits = changedPlot.Plot.GetAxisLimits();

		foreach( var chart in _charts )
		{
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

	private void OnSizeChanged( object sender, SizeChangedEventArgs e )
	{
		var position = scrollStatistics.TransformToAncestor( this ).Transform( new Point( 0, 0 ) );
		scrollStatistics.Height = e.NewSize.Height - position.Y;
	}
	
	private void btnPrevDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( _selectedDay != null )
		{
			_selectedDay = _data.Days.LastOrDefault( x => x.ReportDate < _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _data.Days.First();
		}

		LoadDay( _selectedDay );
	}
	
	private void btnNextDay_OnClick( object sender, RoutedEventArgs e )
	{
		if( _selectedDay != null )
		{
			_selectedDay = _data.Days.FirstOrDefault( x => x.ReportDate > _selectedDay.ReportDate );
		}

		if( _selectedDay == null )
		{
			_selectedDay = _data.Days.Last();
		}
		
		LoadDay( _selectedDay );
	}
}

