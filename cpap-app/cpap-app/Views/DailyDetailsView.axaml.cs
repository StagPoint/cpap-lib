using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyDetailsView : UserControl
{
	public DailyDetailsView()
	{
		InitializeComponent();
		
		// For some reason that eludes me at the moment, if DataContext is not set to *something* (even null),
		// assigning a new DailyDetailsView instance to a NavigationView.Context throw an InvalidCastException
		DataContext = new DailyReport();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( !change.Property.Name.Equals( nameof( DataContext ) ) )
		{
			return;
		}

		Debug.WriteLine( $"Property Changed: {change.Property.Name}" );
	}
}

