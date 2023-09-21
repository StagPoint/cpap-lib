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

		DataContext = new DailyReport();
	}
}

