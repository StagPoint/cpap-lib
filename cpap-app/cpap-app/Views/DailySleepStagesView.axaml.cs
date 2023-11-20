using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailySleepStagesView : UserControl
{
	private SleepStagesViewModel? _sleepStages;
	
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

		if( change.NewValue is DailyReport day )
		{
			_sleepStages = new SleepStagesViewModel( day );
		}

		OuterContainer.DataContext = _sleepStages;
	}
	
	private void DeleteData_OnClick( object? sender, RoutedEventArgs e )
	{
		throw new System.NotImplementedException();
	}
}

