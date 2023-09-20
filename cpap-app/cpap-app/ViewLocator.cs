using System;

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

using cpap_app.ViewModels;

namespace cpap_app;

public class ViewLocator : IDataTemplate
{
	public Control? Build( object? data )
	{
		if( data is null )
		{
			return null;
		}

		var name = data.GetType().FullName!.Replace( "ViewModel", "View" );
		var type = Type.GetType( name );

		if( type != null )
		{
			return (Control)Activator.CreateInstance( type )!;
		}

		return new TextBlock
		{
			Text = $"View not found: {name}", 
			FontSize = 48, 
			FontWeight = FontWeight.Bold, 
			HorizontalAlignment = HorizontalAlignment.Center, 
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	public bool Match( object? data )
	{
		return data is ViewModelBase;
	}
}
