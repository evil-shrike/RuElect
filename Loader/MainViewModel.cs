using System.Collections.ObjectModel;
using Elect.Common;

namespace Elect.Loader
{
	public class MainViewModel : NotifyPropertyChangedBase, IIlogger
	{
		public MainViewModel()
		{
			Log = new ObservableCollection<string>();

		}
		public ObservableCollection<string> Log { get; set; }
		
		void IIlogger.Log(string msg)
		{
			Log.Add(msg);
		}

		void IIlogger.Clear()
		{
			Log.Clear();
		}
	}
}