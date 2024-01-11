using System;

using Avalonia.Controls;
using Avalonia.Input;

namespace cpap_app.Controls;

public partial class AnnotationEditor : UserControl
{
	public event EventHandler? CloseButtonPressed;
	
	public AnnotationEditor()
	{
		InitializeComponent();

		Notes.KeyUp += ( sender, args ) =>
		{
			if( args.Key is Key.Return or Key.Enter )
			{
				if( (args.KeyModifiers & KeyModifiers.Control) != 0 )
				{
					CloseButtonPressed?.Invoke( this, EventArgs.Empty );
					args.Handled = true;
				}
			}
		};
	}

	private void DateTime_OnTextChanged( object? sender, TextChangedEventArgs e )
	{
		if( e.Source is MaskedTextBox textbox )
		{
			if( textbox.Tag is not Border border )
			{
				return;
			}
			
			if( TimeSpan.TryParse( textbox.Text, out TimeSpan result ) )
			{
				border.Classes.Remove( "ValidationError" );
			}
			else
			{
				if( !border.Classes.Contains( "ValidationError" ) )
				{
					border.Classes.Add( "ValidationError" );
				}
			}
		}
	}
}

