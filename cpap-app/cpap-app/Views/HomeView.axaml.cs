using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class HomeView : UserControl
{
	public HomeView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( DataContext ) )
		{
			// Currently this only happens when an import completes, so just load the latest date stored. 
			DailyScore.LoadLastAvailableDate();
		}
	}

	private void BtnImportCPAP_OnClick( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new RoutedEventArgs( MainView.ImportCpapRequestedEvent ) );
	}

	private void BtnImportOximetry_Click( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new RoutedEventArgs( MainView.ImportOximetryRequestedEvent ) );
	}
}
