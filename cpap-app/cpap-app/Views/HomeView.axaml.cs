using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

namespace cpap_app.Views;

public partial class HomeView : UserControl
{
	public UserProfile ActiveUserProfile { get; set; }
	
	public HomeView()
	{
		InitializeComponent();
		
		ActiveUserProfile = UserProfileStore.GetLastUserProfile();
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );
		
		DailyScore.DataContext = null;
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
		RaiseEvent( new MainView.ImportRequestEventArgs( MainView.ImportCpapRequestedEvent ) );
	}

	private void BtnImportOximetry_Click( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new MainView.ImportRequestEventArgs( MainView.ImportOximetryRequestedEvent ) );
	}
}
