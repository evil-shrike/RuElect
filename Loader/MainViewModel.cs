using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public class MainViewModel : NotifyPropertyChangedBase, ILogger
	{
		private bool m_isAutoscrollLog;

		public MainViewModel()
		{
			Log = new ObservableCollection<LogItem>();
			ClearLogCommand = new DelegateCommand(Log.Clear);
			ToggleAutoscrollCommand = new DelegateCommand(toggleAutoscroll);
			IsAutoscrollLog = true;

		}

		public ObservableCollection<LogItem> Log { get; set; }
		
		void ILogger.Log(LogItem item)
		{
			addLogItem(Log, item);
		}

		private static void addLogItem(ObservableCollection<LogItem> log, LogItem item)
		{
			// taking unit tests into account (Application.Current is null there)
			if (Application.Current != null && Application.Current.Dispatcher != null)
			{
				Action action = () => log.Add(item);
				Application.Current.Dispatcher.Invoke(action);
			}
			else
			{
				log.Add(item);
			}			
		}
		
		internal static void removeLogItem(ObservableCollection<LogItem> log, LogItem item)
		{
			// taking unit tests into account (Application.Current is null there)
			if (Application.Current != null && Application.Current.Dispatcher != null)
			{
				Action action = () => log.Remove(item);
				Application.Current.Dispatcher.Invoke(action);
			}
			else
			{
				log.Remove(item);
			}
		}

		ILogItemProgress ILogger.LogProgress()
		{
			var item = new LogItemProgress(Log) { Message = "Starting download..."};
			addLogItem(Log, item);
			return item;
		}

		public ICommand ClearLogCommand { get; set; }
		public ICommand ToggleAutoscrollCommand { get; private set; }

		public Boolean IsAutoscrollLog
		{
			get { return m_isAutoscrollLog; }
			set
			{
				m_isAutoscrollLog = value;
				raisePropertyChangedEvent("IsAutoscrollLog");
			}
		}

		private void toggleAutoscroll()
		{
			// TODO
		}
	}

	public class LogItemDataTemplateSelector : DataTemplateSelector
	{
		private DataTemplate m_progressDataTemplate;
		private DataTemplate m_baseDataTemplate;

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			var element = container as FrameworkElement;
			if (element == null)
				return null;

			if (item is ILogItemProgress)
			{
				if (m_progressDataTemplate == null)
					m_progressDataTemplate = element.FindResource("progressDataTemplate") as DataTemplate;
				return m_progressDataTemplate;
			}

			if (m_baseDataTemplate == null)
				m_baseDataTemplate = element.FindResource("baseDataTemplate") as DataTemplate;
			return m_baseDataTemplate;
		}
	}
	
	public class LogItemProgress : LogItem, ILogItemProgress
	{
		private readonly ObservableCollection<LogItem> m_logItems;

		public LogItemProgress(ObservableCollection<LogItem> logItems)
		{
			m_logItems = logItems;
			Progress = 0;
		}

		public void SetProgress(long received, long total)
		{
			Action action = () =>
			                	{
			                		Message = String.Format("downloaded: {0}/{1}", received, total);
									Progress = ((double)received / (double)total) * 100;
								};
			if (Application.Current != null && Application.Current.Dispatcher != null)
			{
				Application.Current.Dispatcher.Invoke(action);
			}
			else
			{
				action();
			}
		}

		private double m_progress;
		public double Progress
		{
			get { return m_progress; }
			set
			{
				m_progress = value;
				raisePropertyChangedEvent("Progress");
			}
		}

		public void Complete()
		{
			MainViewModel.removeLogItem(m_logItems, this);
		}
	}

	public class SeverityToFgColorConverter: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Brush brush;
			var severity = (LogItemSeverity) value;
			switch (severity)
			{
				case LogItemSeverity.Info:
					brush = new SolidColorBrush(Colors.Black);
					break;
				case LogItemSeverity.Warn:
					brush = new SolidColorBrush(Colors.Red);
					break;
				case LogItemSeverity.Error:
					brush = new SolidColorBrush(Colors.White);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return brush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
	public class SeverityToBgColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Brush brush;
			var severity = (LogItemSeverity)value;
			switch (severity)
			{
				case LogItemSeverity.Info:
				case LogItemSeverity.Warn:
					brush = new SolidColorBrush(Colors.Transparent);
					break;
				case LogItemSeverity.Error:
					brush = new SolidColorBrush(Colors.Red);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return brush;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}