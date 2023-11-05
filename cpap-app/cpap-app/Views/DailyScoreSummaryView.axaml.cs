using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyScoreSummaryView : UserControl
{
	public DailyScoreSummaryView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );

		LoadLastAvailableDate();
	}
	
	private void LoadLastAvailableDate()
	{
		using var db = StorageService.Connect();

		var profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var dates     = db.GetStoredDates( profileID );

		if( dates is { Count: > 0 } )
		{
			var date = dates[ ^1 ];
			LoadDate( db, profileID, date, dates );
		}
		else
		{
			IsVisible = false;
		}
	}

	private void LoadDate( StorageService db, int profileID, DateTime date, List<DateTime> dates )
	{
		var day      = db.LoadDailyReport( profileID, date );
		var prevDate = dates.Where( x => x < date ).Max();

		DailyReport? previousDay = (dates.Count > 1) ? db.LoadDailyReport( profileID, prevDate ) : null;

		DataContext = new DailyScoreSummaryViewModel( day, previousDay );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			if( change.NewValue is DailyScoreSummaryViewModel vm )
			{
				using var db        = StorageService.Connect();
				var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
				var       dates     = db.GetStoredDates( profileID );

				var progressBars = this.GetLogicalDescendants().Where( x => x is ProgressBar progressBar && progressBar.Classes.Contains( "Counter" ) ).ToList();
				foreach( var control in progressBars )
				{
					new Avalonia.Animation.Animation
					{
						Duration = TimeSpan.FromSeconds( change.OldValue == null ? 1.5 : 0.2 ),
						Delay    = TimeSpan.FromSeconds( 0.01 ),
						FillMode = FillMode.None,
						Easing   = new QuarticEaseInOut(),
						Children =
						{
							new KeyFrame
							{
								Setters =
								{
									new Setter( ProgressBar.ValueProperty, 0.0 ),
								},
								Cue = new Cue( 0d )
							},
						},
					}.RunAsync( (control as Animatable)! );
				}

				btnPrevDay.IsEnabled = false;
				btnNextDay.IsEnabled = false;

				Task.Run( () =>
				{
					Thread.Sleep( 200 );

					Dispatcher.UIThread.Post( () =>
					{
						btnPrevDay.IsEnabled = dates.Any( x => x < vm.Date );
						btnNextDay.IsEnabled = dates.Any( x => x > vm.Date );
					} );
				} );
			}
			else
			{
				LoadLastAvailableDate();
			}
		}
	}
	
	private void BtnPrevDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyScoreSummaryViewModel vm )
		{
			return;
		}
		
		using var db        = StorageService.Connect();
		var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var       dates     = db.GetStoredDates( profileID );

		var prevDate = dates.Where( x => x < vm.Date ).Max();

		LoadDate( db, profileID, prevDate, dates );
	}
	
	private void BtnNextDay_OnClick( object? sender, RoutedEventArgs e )
	{
		if( DataContext is not DailyScoreSummaryViewModel vm )
		{
			return;
		}
		
		using var db        = StorageService.Connect();
		var       profileID = UserProfileStore.GetLastUserProfile().UserProfileID;
		var       dates     = db.GetStoredDates( profileID );

		var nextDate = dates.Where( x => x > vm.Date ).Min();

		LoadDate( db, profileID, nextDate, dates );
	}
}

