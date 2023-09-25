using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using cpap_app.ViewModels;

using cpaplib;

namespace cpap_app.Views;

public partial class DailyEventsView : UserControl
{
	public DailyEventsView()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name.Equals( nameof( DataContext ), StringComparison.Ordinal ) )
		{
			if( change.NewValue is DailyReport day )
			{
				DataContext = new DailyEventsViewModel( day );
			}
		}
	}
}

