using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

using FluentAvalonia.UI.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace cpap_app.Helpers;

public class InputDialog
{
	public static async Task<string?> InputString( TopLevel owner, string title, string prompt, string defaultValue = "", string watermark = "", int maxLength = 255, bool wrap = false )
	{
		var input = new TextBox
		{
			Text                = defaultValue,
			Focusable           = true,
			MinWidth            = 300,
			Width               = 300,
			MaxLength           = maxLength,
			Watermark           = watermark,
			HorizontalAlignment = HorizontalAlignment.Left,
			TextWrapping        = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
		};

		var dialog = new ContentDialog
		{
			Title             = title,
			PrimaryButtonText = "Accept",
			CloseButtonText   = "Cancel",
			DefaultButton     = ContentDialogButton.Primary,
			Content = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Children =
				{
					new TextBlock
						{ Text = prompt },
					input
				}
			},
		};

		input.KeyDown += ( sender, args ) =>
		{
			if( args.KeyModifiers != KeyModifiers.None || args.Handled )
			{
				return;
			}

			if( args.Key is Key.Return or Key.Escape )
			{
				dialog.Hide( args.Key == Key.Return ? ContentDialogResult.Primary : ContentDialogResult.Secondary );
			}
		};

		dialog.Opened += ( sender, args ) =>
		{
			input.Focus();
		};

		var result = await dialog.ShowAsync( owner );

		return result != ContentDialogResult.Primary ? null : input.Text;
	}
	
	public static async Task<int?> InputInteger( TopLevel owner, string title, string prompt, int defaultValue = 0, int? minValue = null, int? maxValue = null )
	{
		var result = await InputDouble( owner, title, prompt, defaultValue, minValue, maxValue );

		return result == null ? null : (int)result;
	}
	
	public static async Task<double?> InputDouble( TopLevel owner, string title, string prompt, double defaultValue = 0, double? minValue = null, double? maxValue = null )
	{
		var input = new NumberBox
		{
			AcceptsExpression = true,
			Focusable         = true,
			Minimum           = minValue ?? double.MinValue,
			Maximum           = maxValue ?? double.MaxValue,
			Value             = defaultValue,
			MinWidth          = 200,
			Width             = 200,
		};

		var dialog = new ContentDialog
		{
			Title             = title,
			PrimaryButtonText = "Accept",
			CloseButtonText   = "Cancel",
			DefaultButton     = ContentDialogButton.Primary,
			Content = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Children =
				{
					new TextBlock
						{ Text = prompt },
					input
				}
			},
		};

		input.KeyDown += ( sender, args ) =>
		{
			if( args.KeyModifiers != KeyModifiers.None || args.Handled )
			{
				return;
			}
			
			if( args.Key is Key.Return or Key.Escape )
			{
				dialog.Hide( args.Key == Key.Return ? ContentDialogResult.Primary : ContentDialogResult.Secondary );
			}
		};

		dialog.Opened += ( sender, args ) =>
		{
			Dispatcher.UIThread.Post( () =>
			{
				Control? textBoxOfNumeric = input.GetTemplateChildren().FirstOrDefault( x => x is TextBox );  
				textBoxOfNumeric?.Focus(); 
			} );
		};

		var result = await dialog.ShowAsync( owner );

		if( result != ContentDialogResult.Primary || double.IsNaN( input.Value ) )
		{
			return null;
		}

		return input.Value;
	}
	
	public static async Task<bool> GetConfirmation( Window owner, Icon icon, string title, string message )
	{
		var msgBox       = MessageBoxManager.GetMessageBoxStandard( title, message, ButtonEnum.YesNo, icon );
		var dialogResult = await msgBox.ShowWindowDialogAsync( owner );

		return (dialogResult == ButtonResult.Yes);
	}
}
