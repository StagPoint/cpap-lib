using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

using cpap_app.Events;

using cpap_db;

using cpaplib;

namespace cpap_app.Controls;

public partial class AnnotationListView : UserControl
{
	public AnnotationListView()
	{
		InitializeComponent();
		
		AddHandler( TappedEvent, Item_OnTapped );
		AnnotationList.AddAnnotationListChangedHandler( this, OnAnnotationListChanged );
	}
	
	private static void OnAnnotationListChanged( object? sender, RoutedEventArgs e )
	{
		throw new NotImplementedException();
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

