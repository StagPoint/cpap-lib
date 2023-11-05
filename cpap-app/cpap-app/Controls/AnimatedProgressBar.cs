using Avalonia;
using Avalonia.Data;

namespace cpap_app.Controls;

public class AnimatedProgressBar : AvaloniaObject
{
	public static readonly AttachedProperty<double> TargetValueProperty = AvaloniaProperty.RegisterAttached<AnimatedProgressBar, double>(
		"TargetValue", typeof( AnimatedProgressBar ), 100, false, BindingMode.OneTime );

	public static void SetTargetValue( AvaloniaObject element, object parameter )
	{
		element.SetValue( TargetValueProperty, parameter );
	}

	public static object GetTargetValue( AvaloniaObject element )
	{
		return element.GetValue( TargetValueProperty );
	}
}
