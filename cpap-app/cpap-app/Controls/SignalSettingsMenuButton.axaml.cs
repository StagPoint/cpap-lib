using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

using cpap_app.Configuration;
using cpap_app.Events;
using cpap_app.Helpers;
using cpap_app.ViewModels;
using cpap_app.Views;

using cpap_db;

using cpaplib;

using FluentAvalonia.UI.Controls;

namespace cpap_app.Controls;

public partial class SignalSettingsMenuButton : UserControl
{
	#region Events 
	
	public static readonly RoutedEvent<ChartConfigurationChangedEventArgs> ChartConfigurationChangedEvent = 
		RoutedEvent.Register<SignalSettingsMenuButton, ChartConfigurationChangedEventArgs>( nameof( ChartConfigurationChanged ), RoutingStrategies.Bubble );

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

	public List<SignalMenuItem> Visualizations = new();
	
	#endregion 
	
	#region Private fields

	private SignalChartConfiguration? _chartConfiguration;
	private ColorPickerFlyout?        _flyout;

	private static long            _lastEventCacheReloadTime = -1;
	private static List<EventType> _cachedUserEventTypes         = new List<EventType>();

	#endregion 
	
	public SignalSettingsMenuButton()
	{
		InitializeComponent();
	}
	
	private void MenuItemClickHandler( object? sender, RoutedEventArgs e )
	{
		if( sender is MenuItem { Tag: Action action } )
		{
			action();
		}
	}

	protected override void OnApplyTemplate( TemplateAppliedEventArgs e )
	{
		base.OnApplyTemplate( e );

		#if DEBUG
		
		mnuVisualizations.IsVisible = true;
		DebugSeparator.IsVisible = true;
		
		if( Visualizations.Count > 0 )
		{
			foreach( var item in Visualizations )
			{
				if( item.Header.Equals( "-", StringComparison.Ordinal ) )
				{
					mnuVisualizations.Items.Add( new Separator() );
				}
				else
				{
					var menuItem = new MenuItem()
					{
						Header = item.Header,
						Tag    = item.Command,
					};

					menuItem.AddHandler( MenuItem.ClickEvent, MenuItemClickHandler );

					mnuVisualizations.Items.Add( menuItem );
				}
			}
		}

		#endif
	}

	private void RaiseChangedEvent( string propertyName )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		RaiseEvent( new ChartConfigurationChangedEventArgs
		{
			RoutedEvent        = ChartConfigurationChangedEvent,
			PropertyName       = propertyName,
			ChartConfiguration = ChartConfiguration
		} );
	}

	protected override void OnPropertyChanged( AvaloniaPropertyChangedEventArgs change )
	{
		base.OnPropertyChanged( change );

		if( change.Property.Name == nameof( ChartConfiguration ) && change.NewValue is SignalChartConfiguration config )
		{
			DataContext = config;

			NumberAxisMinValue.Value     = config.AxisMinValue ?? 0;
			NumberAxisMinValue.IsEnabled = config.ScalingMode == AxisScalingMode.Override;

			NumberAxisMaxValue.Value     = config.AxisMaxValue ?? 4000;
			NumberAxisMaxValue.IsEnabled = config.ScalingMode == AxisScalingMode.Override;
			
			UpdatePinMenu( config );
			UpdatePinButton( config );
			UpdateFillMenu( config );
			UpdateEventMenu( config );
		}
	}
	
	private void UpdateEventMenu( SignalChartConfiguration config )
	{
		EventOverlays.Items.Clear();

		var allEventTypes = GetEventTypes();
		
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

				RaiseChangedEvent( nameof( SignalChartConfiguration.DisplayedEvents ) );
			};

			EventOverlays.Items.Add( new CheckMarkMenuItem() { DataContext = item } );
		}
	}

	private static List<EventType> GetEventTypes()
	{
		if( Environment.TickCount - _lastEventCacheReloadTime < 30000 )
		{
			return _cachedUserEventTypes;
		}
		
		_cachedUserEventTypes     = EventTypes.RespiratoryDisturbance.Concat( EventTypes.OxygenSaturation.Concat( EventTypes.Pulse ) ).ToList();
		_lastEventCacheReloadTime = Environment.TickCount;
		
		var storedEventTypes = StorageService.Connect().GetStoredEventTypes( UserProfileStore.GetActiveUserProfile().UserProfileID );

		_cachedUserEventTypes.AddRange( storedEventTypes.Where( x => !_cachedUserEventTypes.Contains( x ) ) );

		return _cachedUserEventTypes;
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
				
				RaiseChangedEvent( nameof( SignalChartConfiguration.PlotColor ) );
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
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		ChartConfiguration.IsPinned = !ChartConfiguration.IsPinned;

		UpdatePinMenu( ChartConfiguration );
		UpdatePinButton( ChartConfiguration );

		RaiseChangedEvent( nameof( SignalChartConfiguration.IsPinned ) );
	}
	
	private void FillBelow_OnClick( object? sender, RoutedEventArgs e )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		ChartConfiguration.FillBelow = !(ChartConfiguration.FillBelow ?? false);
		
		UpdateFillMenu( ChartConfiguration );

		RaiseChangedEvent( nameof( SignalChartConfiguration.FillBelow ) );
	}
	
	private void ComboScalingMode_OnSelectionChanged( object? sender, SelectionChangedEventArgs e )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );

		// Stupidly, the SelectedIndex value gets set to -1 when the control unloads. Who thought *that* was a good idea for a bindable control?
		if( ComboScalingMode.SelectedIndex < 0 )
		{
			return;
		}
		
		var value = (AxisScalingMode)ComboScalingMode.SelectedIndex;

		if( value == ChartConfiguration.ScalingMode )
		{
			return;
		}

		ChartConfiguration.ScalingMode = value;

		NumberAxisMinValue.IsEnabled = value == AxisScalingMode.Override;
		NumberAxisMaxValue.IsEnabled = value == AxisScalingMode.Override;
		
		RaiseChangedEvent( nameof( SignalChartConfiguration.ScalingMode ) );
	}
	
	private void AxisScalingValue_OnValueChanged( NumberBox sender, NumberBoxValueChangedEventArgs args )
	{
		Debug.Assert( ChartConfiguration != null, nameof( ChartConfiguration ) + " != null" );
		
		var propertyName = sender == NumberAxisMinValue 
			? nameof( ChartConfiguration.AxisMinValue ) 
			: nameof( ChartConfiguration.AxisMaxValue );
		
		// Data binding might be disabled due to annoying quirks of the NumberBox control that haven't been fixed yet
		if( sender == NumberAxisMinValue )
		{
			ChartConfiguration.AxisMinValue = args.NewValue;
		}
		else
		{
			ChartConfiguration.AxisMaxValue = args.NewValue;
		}
		
		RaiseChangedEvent( propertyName );
	}
	
	private async void ShowHelp_OnClick( object? sender, RoutedEventArgs e )
	{
		var dialog = new TaskDialog()
		{
			Title = $"Hot Keys and Mouse Actions",
			Buttons = { TaskDialogButton.CloseButton },
			XamlRoot = (Visual)VisualRoot!,
			Content  = new SignalGraphHotkeysView(),
			MaxWidth = 800,
		};
		
		var dialogResult = await dialog.ShowAsync();
	}
}

public class SignalMenuItem
{
	public string Header  { get; set; }
	public Action Command { get; set; }

	public SignalMenuItem( string header, Action command )
	{
		Header  = header;
		Command = command;
	}
}