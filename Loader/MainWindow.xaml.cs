using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;


namespace Elect.Loader
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void onLinkNavigate(object sender, RequestNavigateEventArgs e)
		{
			if (e.Uri != null)
			{
				var psi = new ProcessStartInfo
				          	{
				          		FileName = e.Uri.ToString(), 
								UseShellExecute = true
				          	};

				try
				{
					Process.Start(psi);
				}
				//catch (Exception wex)
				//{
				//    MessageBox.Show(wex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				//}
				finally
				{
					e.Handled = true;
				}
			}     
		}
	}
}
