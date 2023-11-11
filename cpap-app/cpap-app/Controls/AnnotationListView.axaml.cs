using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Events;
using cpap_app.ViewModels;

using cpap_db;

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
			if( change.NewValue is DailyReportViewModel vm )
			{
				_day                  =  vm;
				vm.AnnotationsChanged += VmOnAnnotationsChanged;
			}
			else
			{
				_day = null;
			}
		}
	}
	
	private void VmOnAnnotationsChanged( object? sender, AnnotationListEventArgs e )
	{
		Debug.WriteLine( $"Annotations list changed: {e.Change}" );
	}

	private void Item_OnTapped( object? sender, TappedEventArgs e )
	{
		Debug.WriteLine( sender  );
	}

	private void Delete_OnTapped( object? sender, RoutedEventArgs routedEventArgs )
	{
		if( routedEventArgs.Source is MenuItem { Tag: Annotation annotation } )
		{
			if( DataContext is DailyReport day )
			{
				using var db = StorageService.Connect();
				if( db.Delete( annotation ) )
				{
					day.Annotations.Remove( annotation );

					// TODO: This code is awful. Look for a way to update a "Header grid plus ItemsRepeater grid rows" layout without doing this. 
					DataContext = null;
					DataContext = day;
				}
			}
		}
	}
}

