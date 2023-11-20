using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using cpaplib;

using DynamicData;

using Google.Fitness.Data;

namespace cpap_app.ViewModels;

public class SleepStagesViewModel
{
	public bool IsEmpty { get => Sessions.Count == 0; }

	public ObservableCollection<SleepStageSummaryItemViewModel> StageSummaries { get; set; }
	public ObservableCollection<Session>                        Sessions       { get; set; } = new();

	public SleepStagesViewModel()
	{
		StageSummaries = new ObservableCollection<SleepStageSummaryItemViewModel>
		{
			new SleepStageSummaryItemViewModel() { Label = "Awake", Percentage = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 53 ) },
			new SleepStageSummaryItemViewModel() { Label = "Rem", Percentage   = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 59 ) },
			new SleepStageSummaryItemViewModel() { Label = "Light", Percentage = Random.Shared.NextInt64( 99 ), TimeInStage = new TimeSpan( 3, 50, 0 ) },
			new SleepStageSummaryItemViewModel() { Label = "Deep", Percentage  = Random.Shared.NextInt64( 99 ), TimeInStage = TimeSpan.FromMinutes( 32 ) },
		};
	}

	public SleepStagesViewModel( DailyReport day ) 
		: this()
	{
		var sessions = day.Sessions.Where( x => x.SourceType == SourceType.HealthAPI );
		foreach( var session in sessions )
		{
			if( session.GetSignalByName( SignalNames.SleepStages ) != null )
			{
				Sessions.Add( session );
			}
		}
		
		CalculateSummaryInfo();
	}
	
	// public void AddPeriod( SleepStagePeriodViewModel period )
	// {
	// 	// Can't sort an ObservableCollection, so gotta extract the contents, sort them, and re-add them. 
	// 	var list = Periods.ToList();
	// 	list.Add( period );
	// 	list.Sort();
 //
	// 	Periods.Clear();
	// 	Periods.AddRange( list );
	// 	
	// 	CalculateSummaryInfo();
	// }
	//
	// public void SavePeriod( SleepStagePeriodViewModel viewModel )
	// {
 //        // Forces the collection to raise changed events 
	// 	int index = Periods.IndexOf( viewModel );
	// 	Periods[ index ] = viewModel;
	// 	
	// 	CalculateSummaryInfo();
	// }
	//
	// public void RemovePeriod( SleepStagePeriodViewModel item )
	// {
	// 	Periods.Remove( item );
	// 	CalculateSummaryInfo();
	// }
	
	private void CalculateSummaryInfo()
	{
		Dictionary<SleepStage, double> timeInStage = new Dictionary<SleepStage, double>();

		double totalTime = 0;
		
		foreach( var session in Sessions )
		{
			totalTime += session.Duration.TotalMinutes;

			var signal   = session.GetSignalByName( SignalNames.SleepStages );
			var interval = 1.0 / (60 * signal.FrequencyInHz);
			
			foreach( var value in signal.Samples )
			{
				var stage = (SleepStage)value;
				
				if( !timeInStage.TryAdd( stage, interval ) )
				{
					timeInStage[ stage ] += interval;
				}
			}
		}

		UpdateSummary( StageSummaries[ 0 ], SleepStage.Awake );
		UpdateSummary( StageSummaries[ 1 ], SleepStage.Rem );
		UpdateSummary( StageSummaries[ 2 ], SleepStage.Light );
		UpdateSummary( StageSummaries[ 3 ], SleepStage.Deep );

		void UpdateSummary( SleepStageSummaryItemViewModel summary, SleepStage stage )
		{
			if( !timeInStage.TryGetValue( stage, out double time ) )
			{
				summary.TimeInStage = TimeSpan.Zero;
				summary.Percentage  = 0;
			}
			else
			{
				summary.TimeInStage = TimeSpan.FromMinutes( time );
				summary.Percentage  = (int)(time / totalTime * 100);
			}
		}
	}
}

public enum SleepStage
{
	INVALID = 0,
	Awake   = 1,
	Rem     = 2,
	Light   = 3,
	Deep    = 4,
}

public class SleepStagePeriodViewModel : IComparable<SleepStagePeriodViewModel>, INotifyPropertyChanged
{
	public event EventHandler<bool>? ValidationStatusChanged;

	public SleepStage Stage     { get; set; }
	public DateTime   StartDate { get; set; }
	public DateTime   EndDate   { get; set; }
	public DateTime   StartTime { get; set; }
	public DateTime   EndTime   { get; set; }

	public TimeSpan Duration { get => EndTime - StartTime; }

	public List<string> ValidationErrors = new();
	
	public void SetValidationStatus( bool isValid )
	{
		ValidationStatusChanged?.Invoke( null, isValid );
	}
	
	#region IComparable interface implementation 
	
	public int CompareTo( SleepStagePeriodViewModel? other )
	{
		return StartTime.CompareTo( other?.StartTime );
	}
	
	#endregion 
	
	#region INotifyPropertyChanged interface implementation

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
	{
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
	}

	protected bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
	{
		if( EqualityComparer<T>.Default.Equals( field, value ) )
		{
			return false;
		}

		field = value;

		OnPropertyChanged( propertyName );

		return true;
	}

	#endregion
}

public class SleepStageSummaryItemViewModel : INotifyPropertyChanged
{
	#region Public properties 
	
	public string Label
	{
		get => _label;
		set => SetField( ref _label, value );
	}
	
	public TimeSpan TimeInStage 
	{
		get => _timeInStage;
		set => SetField( ref _timeInStage, value );
	}

	public double   Percentage  
	{
		get => _percentage;
		set => SetField( ref _percentage, value );
	}
	
	#endregion 
	
	#region Private fields

	private string   _label = "Awake";
	private TimeSpan _timeInStage;
	private double   _percentage;

	#endregion 
	
	#region INotifyPropertyChanged interface implementation

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
	{
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
	}

	protected bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
	{
		if( EqualityComparer<T>.Default.Equals( field, value ) )
		{
			return false;
		}

		field = value;

		OnPropertyChanged( propertyName );

		return true;
	}

	#endregion
}
