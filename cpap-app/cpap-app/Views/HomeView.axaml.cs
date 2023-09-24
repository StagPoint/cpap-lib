using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

using cpap_app.ViewModels;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Views;

public partial class HomeView : UserControl
{
	public HomeView()
	{
		InitializeComponent();
	}

	private void BtnImport_OnClick( object? sender, RoutedEventArgs e )
	{
		RaiseEvent( new RoutedEventArgs( MainView.ImportRequestEvent ) );
	}
}
