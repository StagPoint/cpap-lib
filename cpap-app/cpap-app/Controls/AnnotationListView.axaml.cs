using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Controls;

public partial class AnnotationListView : UserControl
{
	private DailyReportViewModel? _day = null;
	
	public AnnotationListView()
	{
		InitializeComponent();
		
		AddHandler( TappedEvent, Item_OnTapped );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			_day = change.NewValue as DailyReportViewModel;
		}
	}
	
	private void Item_OnTapped( object? sender, TappedEventArgs e )
	{
		Debug.WriteLine( sender  );
	}

	private void Delete_OnTapped( object? sender, RoutedEventArgs routedEventArgs )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );
		
		if( routedEventArgs.Source is MenuItem { Tag: Annotation annotation } )
		{
			_day.DeleteAnnotation( annotation );
		}
	}
	
	private void Row_OnDoubleTapped( object? sender, TappedEventArgs e )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );

		if( e.Source is not Control { Tag: Annotation annotation } )
		{
			throw new InvalidCastException( $"Expected a {nameof( Control )}.{nameof( Control.Tag )} property that was assigned a {nameof( Annotation )} value" );
		}

		ShowAnnotationOnGraph( annotation );
		_day.EditAnnotation( annotation );
	}
	
	private void Edit_OnTapped( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( _day != null, nameof( _day ) + " != null" );

		if( e.Source is not Control { Tag: Annotation annotation } )
		{
			throw new InvalidCastException( $"Expected a {nameof( Control )}.{nameof( Control.Tag )} property that was assigned a {nameof( Annotation )} value" );
		}

		ShowAnnotationOnGraph( annotation );
		_day.EditAnnotation( annotation );
	}

	private void Row_OnTapped( object? sender, TappedEventArgs e )
	{
		if( e.Source is not Control { Tag: Annotation annotation } )
		{
			throw new InvalidCastException( $"Expected a {nameof( Control )}.{nameof( Control.Tag )} property that was assigned a {nameof( Annotation )} value" );
		}

		ShowAnnotationOnGraph( annotation );
	}
	
	private void ShowAnnotationOnGraph( Annotation annotation )
	{
		// Ensure that we show at least the surrounding four minutes
		var startTime = annotation.StartTime.AddMinutes( -2 );
		var endTime   = annotation.EndTime.AddMinutes( 2 );

		RaiseEvent( new DateTimeRangeRoutedEventArgs
		{
			RoutedEvent = TimeSelection.TimeRangeSelectedEvent,
			Source      = this,
			StartTime   = startTime,
			EndTime     = endTime
		} );

		RaiseEvent( new SignalSelectionArgs
		{
			RoutedEvent = SignalSelection.SignalSelectedEvent,
			SignalName  = annotation.Signal,
			Source      = this,
		} );
	}
}

