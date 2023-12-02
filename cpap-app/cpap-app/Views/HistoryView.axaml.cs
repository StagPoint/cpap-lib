using System;
using System.Linq;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;

using cpap_app.Controls;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using FluentAvalonia.Core;

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
	}

	#endregion

	#region Base class overrides

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		DataContext = BuildDataContext();
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		var ignoredSignals = new string[]
		{
			SignalNames.EPAP, 
			SignalNames.MaskPressureLow, 
			SignalNames.MaskPressure, 
			SignalNames.AHI, 
			SignalNames.SleepStages,
			SignalNames.Movement,
		};

		var sql = "SELECT DISTINCT signal.Name, signal.UnitOfMeasurement FROM signal";
		
		using var store = StorageService.Connect();
		StorageService.CreateMapping<SignalNamesAndUnits>();

		var signalNamesAndUnits = store.Query<SignalNamesAndUnits>( sql );

		var chartConfigs = SignalChartConfigurationStore.GetSignalConfigurations();
		foreach( var config in chartConfigs )
		{
			if( !config.IsVisible || config.AxisMinValue < 0 || ignoredSignals.Contains( config.SignalName ) )
			{
				continue;
			}

			var units = signalNamesAndUnits.FirstOrDefault( x => x.Name == config.SignalName )?.UnitOfMeasurement ?? "";

			var graph = new SignalStatisticGraph()
			{
				Title      = config.Title,
				SignalName = config.SignalName,
				MinValue   = config.AxisMinValue ?? double.MinValue,
				MaxValue   = config.AxisMaxValue ?? double.MinValue,
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
	
	private static HistoryViewModel BuildDataContext()
	{
		using var store = StorageService.Connect();

		var start = DateTime.Today.AddDays( -180 );
		var end   = DateTime.Today;

		var profileID  = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dayMapping = StorageService.GetMapping<DailyReport>();

		var dayQuery = $@"
			SELECT * 
			FROM [{dayMapping.TableName}] 
			WHERE [{dayMapping.ForeignKey.ColumnName}] = ? AND [{nameof( DailyReport.ReportDate )}] BETWEEN ? AND ? 
			ORDER BY [{dayMapping.TableName}].[{dayMapping.PrimaryKey.ColumnName}]";

		// Only load the part of the DailyReports that is going to be relevant to the consumer
		// (skipping Signal and Settings data, for instance)
		var days = store.Query<DailyReport>( dayQuery, profileID, start, end );
		foreach( var day in days )
		{
			day.Events     = store.SelectByForeignKey<ReportedEvent>( day.ID );
			day.Statistics = store.SelectByForeignKey<SignalStatistics>( day.ID );
			day.Sessions   = store.SelectByForeignKey<Session>( day.ID );

			day.Events.Sort();
			day.Sessions.Sort();
		}

		days.Sort();

		var viewModel = new HistoryViewModel()
		{
			Start = days.Count > 0 ? DateHelper.Max( start, days[ 0 ].ReportDate.Date ) : start,
			End   = end,
			Days  = days
		};

		return viewModel;
	}

	#endregion
	
	#region Nested types

	private class SignalNamesAndUnits
	{
		public string Name              { get; set; } = string.Empty;
		public string UnitOfMeasurement { get; set; } = string.Empty;
	}
	
	#endregion 
}

