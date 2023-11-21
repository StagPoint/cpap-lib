using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using cpaplib;

namespace cpap_app.ViewModels;

public class SleepStagesViewModel
{
	public DailyReport Day { get; init; }
	
	public bool        IsEmpty { get => Sessions.Count == 0; }
	
	public TimeSpan TotalTime       { get; private set; }
	public TimeSpan TimeAsleep      { get; private set; }
	public TimeSpan TimeAwake       { get; private set; }
	public double   SleepEfficiency { get; private set; }

	public ObservableCollection<SleepStageSummaryItemViewModel> StageSummaries { get; set; }
	public ObservableCollection<Session>                        Sessions       { get; set; } = new();

	private SleepStagesViewModel()
	{
		StageSummaries = new ObservableCollection<SleepStageSummaryItemViewModel>
		{
			new SleepStageSummaryItemViewModel() { Label = "Awake" },
			new SleepStageSummaryItemViewModel() { Label = "Rem" },
			new SleepStageSummaryItemViewModel() { Label = "Light" },
			new SleepStageSummaryItemViewModel() { Label = "Deep" },
		};
	}

	public SleepStagesViewModel( DailyReport day ) 
		: this()
	{
		Day = day;
		
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
	
	private void CalculateSummaryInfo()
	{
		Dictionary<SleepStage, double> timeInStage = new Dictionary<SleepStage, double>();

		double totalTime  = 0;
		double timeAwake  = 0;
		double timeAsleep = 0;
		
		foreach( var session in Sessions )
		{
			totalTime += session.Duration.TotalMinutes;

			var signal   = session.GetSignalByName( SignalNames.SleepStages );
			var interval = 1.0 / (60 * signal.FrequencyInHz);
			
			foreach( var value in signal.Samples )
			{
				var stage = (SleepStage)value;
				
				if( stage > SleepStage.Awake )
				{
					timeAsleep += interval;
				}
				else if( stage > SleepStage.INVALID )
				{
					timeAwake += interval;
				}
				
				if( !timeInStage.TryAdd( stage, interval ) )
				{
					timeInStage[ stage ] += interval;
				}
			}
		}

		UpdateSummary( StageSummaries[ 0 ], SleepStage.Awake );
		UpdateSummary( StageSummaries[ 1 ], SleepStage.REM );
		UpdateSummary( StageSummaries[ 2 ], SleepStage.Light );
		UpdateSummary( StageSummaries[ 3 ], SleepStage.Deep );

		TotalTime       = TimeSpan.FromMinutes( (int)Math.Ceiling( totalTime ) );
		TimeAsleep      = TimeSpan.FromMinutes( (int)Math.Ceiling( timeAsleep ) );
		TimeAwake       = TimeSpan.FromMinutes( (int)Math.Ceiling( timeAwake ) );
		SleepEfficiency = TimeAsleep / TotalTime;

		void UpdateSummary( SleepStageSummaryItemViewModel summary, SleepStage stage )
		{
			if( !timeInStage.TryGetValue( stage, out double time ) )
			{
				summary.TimeInStage = TimeSpan.Zero;
				summary.Percentage  = 0;
			}
			else
			{
				summary.TimeInStage = TimeSpan.FromMinutes( (int)time );
				summary.Percentage  = (int)(time / totalTime * 100);
			}
		}
	}
}

public enum SleepStage
{
	INVALID = 0,
	Awake   = 1,
	REM     = 2,
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
