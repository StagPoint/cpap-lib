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
			SleepStage.Focus( NavigationMethod.Tab );
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
			viewModel.ValidationErrors.Add( "End time cannot be before Start time" );
		}

		viewModel.SetValidationStatus( viewModel.ValidationErrors.Count == 0 );
	}
	
	private void StartTime_OnLostFocus( object? sender, RoutedEventArgs e )
	{
		Validate();
	}
	
	private void EndTime_OnLostFocus( object? sender, RoutedEventArgs e )
	{
		Validate();
	}
	
	private void StartDate_OnLostFocus( object? sender, RoutedEventArgs e )
	{
		Validate();
	}
	
	private void EndDate_OnLostFocus( object? sender, RoutedEventArgs e )
	{
		Validate();
	}
}

