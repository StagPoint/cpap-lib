using System;
using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using cpap_app.Events;

namespace cpap_app.Views;

public partial class SessionDetailsView : UserControl
{
	public event EventHandler<SignalSelectionArgs>?   OnSignalSelected;
	public event EventHandler<ReportedEventTypeArgs>? OnEventTypeSelected;
	
	public SessionDetailsView()
	{
		InitializeComponent();
	}

	private void EventType_OnSelected( object? sender, ReportedEventTypeArgs e )
	{
		OnEventTypeSelected?.Invoke( this, e );
	}
	
	private void Signal_OnSelected( object? sender, SignalSelectionArgs e )
	{
		OnSignalSelected?.Invoke( this, e );
	}
}

