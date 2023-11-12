using System;
using System.ComponentModel.DataAnnotations;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace cpap_app.Controls;

public partial class AnnotationEditor : UserControl
{
	public AnnotationEditor()
	{
		InitializeComponent();
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

