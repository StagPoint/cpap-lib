using System;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;

using cpaplib;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Controls;

public partial class SignalSettingsMenuButton : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<ChartConfigurationChangedEventArgs> ChartConfigurationChangedEvent = RoutedEvent.Register<SignalSettingsMenuButton, ChartConfigurationChangedEventArgs>( nameof( ChartConfigurationChanged ), RoutingStrategies.Bubble );

	public static void AddChartConfigurationChangedHandler( IInputElement element, EventHandler<ChartConfigurationChangedEventArgs> handler )
	{
		element.AddHandler( ChartConfigurationChangedEvent, handler );
	}

	public event EventHandler<ChartConfigurationChangedEventArgs> ChartConfigurationChanged
	{
		add => AddHandler( ChartConfigurationChangedEvent, value );
		remove => RemoveHandler( ChartConfigurationChangedEvent, value );
	}
	
	#endregion 
	
	#region Public properties

	public static readonly DirectProperty<SignalSettingsMenuButton, SignalChartConfiguration?> ChartConfigurationProperty =
		AvaloniaProperty.RegisterDirect<SignalSettingsMenuButton, SignalChartConfiguration?>( nameof( ChartConfiguration ), o => o.ChartConfiguration );

	public SignalChartConfiguration? ChartConfiguration
	{
		get => _chartConfiguration;
		set => SetAndRaise( ChartConfigurationProperty, ref _chartConfiguration, value );
	}

	#endregion 
	
	#region Private fields

	private SignalChartConfiguration? _chartConfiguration;
	private ColorPickerFlyout?        _flyout;

	#endregion 
	
	public SignalSettingsMenuButton()
	{
		InitializeComponent();
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( ChartConfiguration ) && change.NewValue is SignalChartConfiguration config )
		{
			UpdatePinMenu( config );
			UpdatePinButton( config );
			UpdateFillMenu( config );
			UpdateEventMenu( config );
		}
	}
	private void UpdateEventMenu( SignalChartConfiguration config )
	{
		EventOverlays.Items.Clear();
		
		var allEventTypes = EventTypes.RespiratoryDisturbance.Concat( EventTypes.OxygenSaturation.Concat( EventTypes.Pulse ) );
		foreach( var eventType in allEventTypes )
		{
			var item = new CheckmarkMenuItemViewModel()
			{
				// TODO: The label of the event type should be retrieved from configuration data 
				Label = NiceNames.Format( eventType.ToString() ),

				Tag       = eventType,
				IsChecked = config.DisplayedEvents.Contains( eventType )
			};

			item.PropertyChanged += ( sender, args ) =>
			{
				if( !item.IsChecked )
					config.DisplayedEvents.Remove( eventType );
				else
					config.DisplayedEvents.Add( eventType );

				RaiseEvent( new ChartConfigurationChangedEventArgs()
				{
					RoutedEvent        = ChartConfigurationChangedEvent,
					PropertyName       = nameof( SignalChartConfiguration.DisplayedEvents ),
					ChartConfiguration = config,
				} );
			};

			EventOverlays.Items.Add( new CheckMarkMenuItem() { DataContext = item } );
		}
	}

	private void ConfigureSignalColor_OnClick( object? sender, RoutedEventArgs e )
	{
		if( ChartConfiguration == null )
		{
			return;
		}

		if( _flyout == null )
		{
			_flyout = new ColorPickerFlyout();
			
			_flyout.Confirmed += ( flyout, args ) =>
			{
				var color = _flyout.ColorPicker.Color.ToDrawingColor();
				ChartConfiguration.PlotColor = color;
				
				RaiseEvent( new ChartConfigurationChangedEventArgs()
				{
					RoutedEvent        = ChartConfigurationChangedEvent,
					PropertyName       = nameof( ChartConfiguration.PlotColor ),
					ChartConfiguration = ChartConfiguration
				});
			};
		}
		
		_flyout.ColorPicker.PreviousColor = ChartConfiguration.PlotColor.ToColor2();
		_flyout.ColorPicker.Color         = _flyout.ColorPicker.PreviousColor;

		_flyout.Placement                       = PlacementMode.Pointer;
		_flyout.ColorPicker.IsMoreButtonVisible = true;
		_flyout.ColorPicker.IsCompact           = false;
		_flyout.ColorPicker.IsAlphaEnabled      = false;
		_flyout.ColorPicker.UseSpectrum         = true;
		_flyout.ColorPicker.UseColorWheel       = true;
		_flyout.ColorPicker.UseColorTriangle    = false;

		// TODO: Retrieve a global color palette instead 
		var hexColors = new[]
		{
			0xffebac23, 0xffb80058, 0xff008cf9, 0xff006e00, 0xff00bbad,
			0xffd163e6, 0xffb24502, 0xffff9287, 0xff5954d6, 0xff00c6f8,
			0xff878500, 0xff00a76c,
			0xfff6da9c, 0xffff5caa, 0xff8accff, 0xff4bff4b, 0xff6efff4,
			0xffedc1f5, 0xfffeae7c, 0xffffc8c3, 0xffbdbbef, 0xffbdf2ff,
			0xfffffc43, 0xff65ffc8,
			0xffaaaaaa,
		};

		_flyout.ColorPicker.UseColorPalette     = true;
		_flyout.ColorPicker.PaletteColumnCount  = 16;
		_flyout.ColorPicker.CustomPaletteColors = hexColors.Select( Avalonia.Media.Color.FromUInt32 );
		
		_flyout.ShowAt( this );
	}

	private void UpdatePinMenu( SignalChartConfiguration? config )
	{
		if( config is { IsPinned: true } )
		{
			mnuPin.Symbol = Symbol.UnPin;
			txtPin.Text   = "Unpin";
		}
		else
		{
			mnuPin.Symbol = Symbol.Pin;
			txtPin.Text   = "Pin";
		}
	}

	private void UpdatePinButton( SignalChartConfiguration? config )
	{
		btnPinUnpin.Symbol = config is { IsPinned: true } ? Symbol.UnPin : Symbol.Pin;
	}

	private void UpdateFillMenu( SignalChartConfiguration? config )
	{
		if( config is { FillBelow: true } )
		{
			mnuFillBelow.Symbol    = Symbol.Checkmark;
			mnuFillBelow.IsVisible = true;
		}
		else
		{
			mnuFillBelow.IsVisible = false;
		}
	}
	
	private void OnPinClick( object? sender, RoutedEventArgs e )
	{
		if( ChartConfiguration == null )
		{
			return;
		}

		ChartConfiguration.IsPinned = !ChartConfiguration.IsPinned;

		UpdatePinMenu( ChartConfiguration );
		UpdatePinButton( ChartConfiguration );

		RaiseEvent( new ChartConfigurationChangedEventArgs()
		{
			RoutedEvent        = ChartConfigurationChangedEvent,
			PropertyName       = nameof( ChartConfiguration.IsPinned ),
			ChartConfiguration = ChartConfiguration
		} );
	}
	
	private void FillBelow_OnClick( object? sender, RoutedEventArgs e )
	{
		if( ChartConfiguration == null )
		{
			return;
		}

		ChartConfiguration.FillBelow = !(ChartConfiguration.FillBelow ?? false);
		
		UpdateFillMenu( ChartConfiguration );

		RaiseEvent( new ChartConfigurationChangedEventArgs()
		{
			RoutedEvent        = ChartConfigurationChangedEvent,
			PropertyName       = nameof( ChartConfiguration.FillBelow ),
			ChartConfiguration = ChartConfiguration
		} );
	}
}

