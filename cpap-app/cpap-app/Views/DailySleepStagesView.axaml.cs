using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class DailySleepStagesView : UserControl
{
	public DailySleepStagesView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name != nameof( DataContext ) )
		{
			return;
		}
		
		StagesSummary.DataContext = new SleepStagesViewModel();
	}
	
	private void AddNew_OnClick( object? sender, RoutedEventArgs e )
	{
		
	}
}

