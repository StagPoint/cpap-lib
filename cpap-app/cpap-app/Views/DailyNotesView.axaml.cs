using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using cpap_db;

using cpaplib;

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
		}
	}

	#endregion 

	#region Event handlers 
	
	private void Notes_OnTextChanged( object? sender, TextChangedEventArgs e )
	{
		if( Environment.TickCount - _lastLoaded < 1000 )
		{
			// Ignore OnTextChanged events that occur from simply loading the control with the notes.
			// The time chosen is a bit arbitrary, but probably safe given that the user would very 
			// nearly have to be intentionally trying to get to the notes tab as fast as possible 
			// in order to change the text in less than a second. 
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
		if( DataContext is DailyReport day )
		{
			using var db = StorageService.Connect();
			db.Update( day );
		}
	}
	
	#endregion 
}

