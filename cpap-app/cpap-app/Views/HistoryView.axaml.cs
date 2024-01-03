using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;

using cpap_app.Configuration;
using cpap_app.Controls;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using QuestPDF.Helpers;

namespace cpap_app.Views;

public partial class HistoryView : UserControl
{
	#region Private fields

	private List<HistoryGraphBase> _charts      = new();
	private DispatcherTimer?       _renderTimer = null;

	#endregion

	#region Constructor

	public HistoryView()
	{
		InitializeComponent();

		AddHandler( GraphEvents.DisplayedRangeChangedEvent, OnGraphDisplayedRangeChanged );
		AddHandler( TimeSelection.TimeSelectedEvent, OnDaySelected  );
	}

	#endregion

	#region Base class overrides

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is not HistoryViewModel )
			{
				DataContext = BuildDataContext();
			}
		}
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DateRangeCombo.SelectedIndex = 0;
		CurrentDateSelection.Text    = $"{DateTime.Today:D}";
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		var ignoredSignals = new[]
		{
			SignalNames.FlowRate,
			SignalNames.EPAP, 
			SignalNames.MaskPressureLow, 
			SignalNames.MaskPressure, 
			SignalNames.AHI, 
			SignalNames.SleepStages,
			SignalNames.Movement,
		};

		const string sql = "SELECT DISTINCT signal.Name, signal.UnitOfMeasurement, signal.MinValue, signal.MaxValue FROM signal";
		
		using var store = StorageService.Connect();
		StorageService.CreateMapping<SignalNamesAndUnits>();

		var signalNamesAndUnits = store.Query<SignalNamesAndUnits>( sql );

		var chartConfigs = SignalChartConfigurationStore.GetSignalConfigurations();
		foreach( var config in chartConfigs )
		{
			if( !config.IsVisible || !config.ShowInTrends || ignoredSignals.Contains( config.SignalName ) )
			{
				continue;
			}

			var signalDefaults = signalNamesAndUnits.FirstOrDefault( x => x.Name == config.SignalName );
			Debug.Assert( signalDefaults != null, nameof( signalDefaults ) + " != null" );

			var units = signalDefaults.UnitOfMeasurement ?? "";

			double? minValue = config.AxisMinValue;
			double? maxValue = config.AxisMaxValue;

			switch( config.ScalingMode )
			{
				case AxisScalingMode.Defaults:
					minValue = signalDefaults.MinValue;
					maxValue = signalDefaults.MaxValue;
					break;
				case AxisScalingMode.AutoFit:
					minValue = null;
					maxValue = null;
					break;
			}

			var graph = new SignalStatisticGraph()
			{
				Title      = config.Title,
				SignalName = config.SignalName,
				MinValue   = minValue,
				MaxValue   = maxValue,
				Units      = units,
			};

			Graphs.Children.Add( graph );
		}
		
		foreach( var control in Graphs.Children )
		{
			if( control is HistoryGraphBase graph )
			{
				_charts.Add( graph );
			}
		}
	}

	#endregion

	#region Event handlers

	private void OnDaySelected( object? sender, DateTimeRoutedEventArgs e )
	{
		CurrentDateSelection.Text = $"{e.DateTime:D}";
	}
	
	private void DateRangeCombo_SelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		if( sender is not ComboBox combo )
		{
			return;
		}
		
		if( combo.SelectedItem is ComboBoxItem { Tag: string value } )
		{
			using var store = StorageService.Connect();

			var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;
			if( profileID == -1 )
			{
				return;
			}
			
			var lastAvailableDate = store.GetMostRecentStoredDate( profileID );
			if( lastAvailableDate <= DateHelper.UnixEpoch )
			{
				return;
			}

			// If a set number of days is defined, show only that number of days (from the last available date)
			if( int.TryParse( value, out int amount ) )
			{
				RangeStart.SelectedDate = lastAvailableDate.AddDays( -amount );
				RangeEnd.SelectedDate   = lastAvailableDate;

				DataContext = BuildDataContext();
			}
			else if( string.Equals( value, "all", StringComparison.OrdinalIgnoreCase ) )
			{
				var allDates = store.GetStoredDates( profileID );
				RangeStart.SelectedDate = allDates[ 0 ];
				RangeEnd.SelectedDate   = allDates[ ^1 ];

				DataContext = BuildDataContext();
			}
		}
	}
	
	private void RefreshDateRange_OnClick( object? sender, RoutedEventArgs e )
	{
		DateRangeCombo.SelectedIndex = DateRangeCombo.ItemCount - 1;
		DataContext                  = BuildDataContext();
	}
	
	private void OnGraphDisplayedRangeChanged( object? sender, DateTimeRangeRoutedEventArgs e )
	{
		foreach( var graph in _charts )
		{
			if( !ReferenceEquals( e.Source, graph ) )
			{
				graph.UpdateVisibleRange( e.StartTime, e.EndTime );
			}
		}
		
		ResetRenderTimer();
	}

	#endregion

	#region Private functions

	private void ResetRenderTimer()
	{
		_renderTimer ??= new DispatcherTimer( TimeSpan.FromSeconds( 0.25 ), DispatcherPriority.Default, ( _, _ ) =>
		{
			_renderTimer!.Stop();
			
			foreach( var control in _charts )
			{
				control.RenderGraph( true );
			}
		} );

		_renderTimer.Stop();
		_renderTimer.Start();
	}
	
	private HistoryViewModel BuildDataContext()
	{
		var start     = RangeStart.SelectedDate ?? DateTime.Today.AddDays( -90 );
		var end       = RangeEnd.SelectedDate ?? DateTime.Today;
		var profileID = UserProfileStore.GetActiveUserProfile().UserProfileID;

		return HistoryViewModel.GetHistory( profileID, start, end );
	}

	#endregion
	
	#region Nested types

	private class SignalNamesAndUnits
	{
		public string Name              { get; set; } = string.Empty;
		public string UnitOfMeasurement { get; set; } = string.Empty;
		public double MinValue          { get; set; }
		public double MaxValue          { get; set; }
	}
	
	#endregion
	
	#region Printing 
	
	private void PrintReport_OnClick( object? sender, RoutedEventArgs e )
	{
		if( sender is Button button )
		{
			button.ContextFlyout!.ShowAt( button );
		}
	}
	
	private void PrintToPDF( object? sender, RoutedEventArgs e )
	{
		throw new NotImplementedException();
	}
	
	private void PrintToJPG( object? sender, RoutedEventArgs e )
	{
		throw new NotImplementedException();
	}

	private void PrintToPreviewer( object? sender, RoutedEventArgs e )
	{
		var chart = _charts[ 0 ];
		var size  = new PixelSize( (int)PageSizes.Letter.Width * 4, (int)chart.Bounds.Height );
		
		var image = _charts[ 0 ].PrintToBitmap( size, new Vector( 128, 128 ) );
	}

	#endregion
}

