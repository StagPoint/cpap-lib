using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cpap_app.Views;

public partial class OxygenEventsView : UserControl
{
	public static readonly StyledProperty<bool> IsFooterVisibleProperty = AvaloniaProperty.Register<DataDistributionView, bool>( nameof( IsFooterVisible ) );

	public bool IsFooterVisible
	{
		get => GetValue( IsFooterVisibleProperty );
		set => SetValue( IsFooterVisibleProperty, value );
	}

	public OxygenEventsView()
	{
		InitializeComponent();
	}
}

