using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Elect.Loader
{
	public partial class App : Application
	{
		private const string EventLogSourceName = "Elect Protocol Loader";

		private string m_connectionString; // = "Data Source=.;Initial Catalog=elect;Integrated Security=True";

		protected override void OnStartup(StartupEventArgs e)
		{
			AppDomain.CurrentDomain.UnhandledException += onAppDomainUnhandledException;

			base.OnStartup(e);

			m_connectionString = ConfigurationManager.ConnectionStrings["default"].ConnectionString;

			var window = new MainWindow();
			var mainViewModel = new MainViewModel();
			ILogger logger = mainViewModel;
			var repository = new Repository(m_connectionString, logger );
			var vmRuelect = new RuelectViewModel(logger)
								{
									Repository = repository,
									Downloader = new FileDownloader()
								};
			var vmKartaitogov = new KartaitogovViewModel(logger)
									{
										Repository = repository,
										Downloader = new FileDownloader()
									};
			var vmAnalyze = new AnalyzeViewModel(logger)
			                	{
									Repository = repository
			                	};
			window.DataContext = mainViewModel;
			((TabItem)window.tabs.Items[0]).DataContext = vmRuelect;
			((TabItem)window.tabs.Items[1]).DataContext = vmKartaitogov;
			((TabItem)window.tabs.Items[2]).DataContext = vmAnalyze;
			
			TaskScheduler.UnobservedTaskException += (s, ea) =>
			                                         	{
															logger.LogError("UnobservedTaskException: " + ea.Exception.GetBaseException().ToString());
															// prevent app crashing:
															ea.SetObserved();
			                                         	};

			Application.Current.MainWindow = window;
			runInitialChecks(logger);
			window.Show();
		}

		private void runInitialChecks(ILogger logger)
		{
			Task.Factory.StartNew(
				() =>
					{
						using (var con = new SqlConnection(m_connectionString))
						{
							try
							{
								con.Open();
								con.Close();
							}
							catch (Exception ex)
							{
								logger.Log("Cannot open DB, please check connection string in Loader.exe.config. Exception: " + ex);
							}
						}
					});
		}

		private void onAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;
			if (ex == null)
				Environment.Exit(1);
			handleUnhandledException(ex);
		}

		private void handleUnhandledException(Exception ex)
		{
			// #1: log to system EventLog
			Exception ex2;
			if (tryFindLoaderException(ex, out ex2))
				ex = ex2;
			writeToEventLog(ex);

			// #3: show "last message" to user
			MessageBox.Show("UnhandledException: " + ex, "Critical error happened",
							MessageBoxButton.OK, MessageBoxImage.Stop);
		}

		private static bool tryFindLoaderException(Exception ex, out Exception loaderEx)
		{
			loaderEx = null;
			while (ex.InnerException != null)
			{
				ex = ex.InnerException;
			}
			ReflectionTypeLoadException typeLoadEx;
			if ((typeLoadEx = ex as ReflectionTypeLoadException) != null && typeLoadEx.LoaderExceptions.Length > 0)
				loaderEx = typeLoadEx.LoaderExceptions[0];

			return loaderEx != null;
		}

		private void writeToEventLog(Exception ex)
		{
			try
			{
				if (!EventLog.SourceExists(EventLogSourceName))
					EventLog.CreateEventSource(EventLogSourceName, "Application");

				var myLog = new EventLog { Source = EventLogSourceName };
				myLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
			}
			catch (Exception ex2)
			{
				Trace.TraceError("Program::AppDomain_OnUnhandledException::An error occured during writing in Windows EventLog: " + ex2);
			}
		}

	}
}
