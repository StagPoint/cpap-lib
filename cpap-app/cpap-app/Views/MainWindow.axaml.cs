using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;

using FluentAvalonia.UI.Windowing;

using ReactiveUI;

namespace cpap_app.Views;

public partial class MainWindow : AppWindow
{
	public MainWindow()
	{
		InitializeComponent();

		#if DEBUG
		this.AttachDevTools();
		#endif
	}
}
