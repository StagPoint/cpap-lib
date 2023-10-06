using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cpap_app.Views;

public partial class DataDistributionView : UserControl
{
	public static readonly StyledProperty<string> UnitOfMeasureProperty = AvaloniaProperty.Register<DataDistributionView, string>( nameof( UnitOfMeasure ) );

	public string UnitOfMeasure
	{
		get => GetValue( UnitOfMeasureProperty );
		set => SetValue( UnitOfMeasureProperty, value );
	}

	public DataDistributionView()
	{
		InitializeComponent();
	}
}

