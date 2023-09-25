using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

using ReactiveUI;

namespace cpap_app.Views;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		#if DEBUG
		this.AttachDevTools();
		#endif
	}
}
