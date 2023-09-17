using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ModernWpf.Controls;

namespace cpapviewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			DataContext = this;
			
			var xcr = Application.Current.Resources.MergedDictionaries.OfType<XamlControlsResources>().FirstOrDefault();
			if (xcr != null)
			{
				xcr.UseCompactResources = true;
			}

			_mainFrame.Navigate( new WelcomeNotice() );
		}
		
		private void Import_Click( object sender, RoutedEventArgs e )
		{
			throw new NotImplementedException();
		}
		
		private void Exit_Click( object sender, RoutedEventArgs e )
		{
			Application.Current.Shutdown();
		}
	}
}
