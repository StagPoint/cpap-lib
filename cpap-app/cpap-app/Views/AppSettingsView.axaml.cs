using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

using cpap_app.ViewModels;

namespace cpap_app.Views;

public partial class AppSettingsView : UserControl
{
	public AppSettingsView()
	{
		InitializeComponent();

		DataContext = new AppSettingsViewModel();

		LoadDependenciesList();
	}
	
	private void LoadDependenciesList()
	{
		var dependencyList = new List<DependencyInfoItem>();

		using var assetStream = AssetLoader.Open( new Uri( $"avares://{Assembly.GetExecutingAssembly().FullName}/Assets/Dependencies.csv" ) );
		using var reader      = new StreamReader( assetStream, Encoding.Default, leaveOpen: true );

		while( !reader.EndOfStream )
		{
			var line = reader.ReadLine();
			if( string.IsNullOrEmpty( line ) )
			{
				continue;
			}

			var parts = line.Split( ',' );

			dependencyList.Add( new DependencyInfoItem( parts[ 0 ], parts[ 1 ], parts[ 2 ] ) );
		}

		Dependencies.ItemsSource = dependencyList;
	}

	private void LaunchRepoLinkItemClick( object sender, RoutedEventArgs e )
	{
		var uri = new Uri( "https://github.com/StagPoint/cpap-lib" );

		Process.Start( new ProcessStartInfo( uri.ToString() ) { UseShellExecute = true, Verb = "open" } );
	}

}

public class DependencyInfoItem
{
	public string Name    { get; set; }
	public string License { get; set; }
	public string Website { get; set; }

	public DependencyInfoItem( string name, string license, string website )
	{
		Name    = name;
		License = license;
		Website = website;
	}
}
