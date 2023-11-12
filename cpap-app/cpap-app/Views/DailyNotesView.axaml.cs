using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

using cpap_app.Events;
using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class DailyNotesView : UserControl
{
	#region Private fields 
	
	private DispatcherTimer? _saveTimer  = null;
	private int              _lastLoaded = -1;
	
	#endregion 
	
	#region Constructor 

	public DailyNotesView()
	{
		InitializeComponent();
	}
	
	#endregion 
	
	#region Base class overrides 

	protected override void OnUnloaded( RoutedEventArgs e )
	{
		base.OnUnloaded( e );

		if( _saveTimer is { IsEnabled: true } )
		{
			_saveTimer.Stop();
		}
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			_lastLoaded = Environment.TickCount;

			if( change.NewValue is DailyReportViewModel viewModel )
			{ 
				viewModel.AnnotationsChanged += OnAnnotationsChanged;
			}

			// Because we have to play stupid games with DataContext to refresh the Annotations List,
			// we also have to manually refresh its DataContext to match the parent control. 
			if( !ReferenceEquals( Annotations.DataContext, change.NewValue ) )
			{
				Annotations.DataContext = change.NewValue;
			}
		}
	}
	
	#endregion 

	#region Event handlers 
	
	private void OnAnnotationsChanged( object? sender, AnnotationListEventArgs e )
	{
		// TODO: This code is awful. Look for a way to update a "Header grid plus ItemsRepeater grid rows" layout without doing this. 
		Annotations.DataContext = null;
		Annotations.DataContext = DataContext;
	}

	private void Notes_OnTextChanged( object? sender, TextChangedEventArgs e )
	{
		if( Environment.TickCount - _lastLoaded < 500 )
		{
			// Ignore OnTextChanged events that occur from simply loading the control with the notes.
			// The time chosen is a bit arbitrary, but probably safe given that the user would very 
			// nearly have to be intentionally trying to get to the notes tab as fast as possible 
			// in order to change the text in this time frame, and would then have to navigate away
			// in order to lose any changes.
			return;
		}
		
		_saveTimer?.Stop();

		_saveTimer ??= new DispatcherTimer( TimeSpan.FromSeconds( 0.5 ), DispatcherPriority.Background, ( _, _ ) =>
		{
			SaveNotes();

			_saveTimer!.Stop();
		} );

		_saveTimer.Start();
	}

	#endregion
	
	#region Private functions 
	
	private void SaveNotes()
	{
		if( DataContext is DailyReportViewModel day )
		{
			day.SaveNotes( Notes.Text! );
		}
	}
	
	#endregion 
}

