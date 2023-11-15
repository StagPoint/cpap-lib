using Avalonia;
using Avalonia.Controls;

namespace cpap_app.Controls;

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

