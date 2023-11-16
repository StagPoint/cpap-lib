using System;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class AddSleepStageView : UserControl
{
	public AddSleepStageView()
	{
		InitializeComponent();
	}

	protected override void OnLoaded( RoutedEventArgs e )
	{
		base.OnLoaded( e );
		
		Dispatcher.UIThread.Post( () =>
		{
			SleepStageList.Focus( NavigationMethod.Tab );
			Validate();
		}, DispatcherPriority.Background );
	}

	protected override void OnUnloaded( RoutedEventArgs e )
	{
		base.OnUnloaded( e );

		Validate();
	}

	private void Validate()
	{
		if( DataContext is not SleepStagePeriodViewModel viewModel )
		{
			return;
		}

		viewModel.ValidationErrors.Clear();
		
		if( DateTime.TryParse( StartTime.Text, out DateTime startTime ) )
		{
			viewModel.StartTime = viewModel.StartDate.Date.Add( startTime.TimeOfDay );
			StartTime.Classes.Remove( "ValidationError" );
		}
		else
		{
			StartTime.Classes.Add( "ValidationError" );
			viewModel.ValidationErrors.Add( $"Incorrect format for {nameof( viewModel.StartTime )}" );
		}
		
		if( DateTime.TryParse( EndTime.Text, out DateTime endTime ) )
		{
			viewModel.EndTime = viewModel.EndDate.Date.Add( endTime.TimeOfDay );
			EndTime.Classes.Remove( "ValidationError" );
		}
		else
		{
			EndTime.Classes.Add( "ValidationError" );
			viewModel.ValidationErrors.Add( $"Incorrect format for {nameof( viewModel.EndTime )}" );
		}

		if( viewModel.EndTime <= viewModel.StartTime )
		{
			EndTime.Classes.Add( "ValidationError" );
			viewModel.ValidationErrors.Add( "End time cannot be before Start time" );
		}

		viewModel.SetValidationStatus( viewModel.ValidationErrors.Count == 0 );
	}
	
	private void InputElement_OnLostFocus( object? sender, RoutedEventArgs e )
	{
		Validate();
	}
	
	private void SleepStage_OnKeyDown( object? sender, KeyEventArgs e )
	{
		if( e.KeyModifiers != KeyModifiers.None )
		{
			return;
		}

		var chr = e.Key.ToString();
		SleepStageList.SelectedIndex = e.Key switch
		{
			Key.A => (int)SleepStage.Awake,
			Key.R => (int)SleepStage.Rem,
			Key.L => (int)SleepStage.Light,
			Key.D => (int)SleepStage.Deep,
			_     => SleepStageList.SelectedIndex
		};
	}
}

