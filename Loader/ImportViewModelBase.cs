using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public enum UnknownRegionActions
	{
		Create,
		Ignore,
		Stop
	}
	public enum UnknownComissionActions
	{
		Create,
		Ignore,
		Stop
	}

	public abstract class ImportViewModelBase: NotifyPropertyChangedBase
	{
		private readonly ILogger m_logger;
		private bool m_isLoading;
		private UnknownRegionActions m_unknownRegionAction;
		private UnknownComissionActions m_unknownComissionAction;

		protected ImportViewModelBase(ILogger logger)
		{
			m_logger = logger;
			StopCommand = new DelegateCommand(onStop);
		}

		public ICommand StopCommand { get; set; }

		public Repository Repository { get; set; }

		/// <summary>
		/// Any long running process
		/// </summary>
		protected Task TaskLoading;

		protected CancellationTokenSource Tcs;

		/// <summary>
		/// Any (first) exception which was occured inside TaskLoading's action.
		/// </summary>
		internal Exception LastError;

		public Boolean IsLoading
		{
			get { return m_isLoading; }
			set
			{
				m_isLoading = value;
				raisePropertyChangedEvent("IsLoading");
				raisePropertyChangedEvent("IsNotBusy");
			}
		}

		public Boolean IsNotBusy
		{
			get { return !IsLoading; }
		}

		private void onStop()
		{
			if (TaskLoading == null)
				return;

			log("Cancelling");
			TaskLoading.ContinueWith(
				t =>
					{
						if (t.Exception != null)
							log("Canceled with error: " + t.Exception.GetBaseException().Message);
						else
							log("Canceled");
					});
			Tcs.Cancel();
		}

		/// <summary>
		/// What to do in case we encounter not existed region.
		/// </summary>
		public UnknownRegionActions UnknownRegionAction
		{
			get { return m_unknownRegionAction; }
			set
			{
				m_unknownRegionAction = value;
				raisePropertyChangedEvent("UnknownRegionAction");
			}
		}

		/// <summary>
		/// What to do in case we encounter not existed comission.
		/// </summary>
		public UnknownComissionActions UnknownComissionAction
		{
			get { return m_unknownComissionAction; }
			set
			{
				m_unknownComissionAction = value;
				raisePropertyChangedEvent("UnknownComissionAction");
			}
		}

		#region Logging
		
		protected void logError(string msg)
		{
			m_logger.LogError(msg);
		}

		protected void logWarning(string msg)
		{
			m_logger.LogWarning(msg);
		}

		protected void log(string msg)
		{
			m_logger.Log(msg);
		}

		protected ILogger Logger
		{
			get { return m_logger; }
		}
		
		#endregion

		protected Poll ensurePollCreated()
		{
			var poll = new Poll
			           	{
			           		Name =
			           			"Выборы депутатов Государственной Думы Федерального Собрания Российской Федерации шестого созыва",
			           	};
			poll.Candidates = new List<Candidate>
			                  	{
			                  		new Candidate {Name = "Справедливая Россия", Poll = poll},
			                  		new Candidate {Name = "ЛДПР", Poll = poll},
			                  		new Candidate {Name = "Патриоты России", Poll = poll},
			                  		new Candidate {Name = "КПРФ", Poll = poll},
			                  		new Candidate {Name = "ЯБЛОКО", Poll = poll},
			                  		new Candidate {Name = "Единая Россия", Poll = poll},
			                  		new Candidate {Name = "Правое дело", Poll = poll}
			                  	};
			Repository.EnsurePollExists(poll);
			return poll;
		}

		protected void loadProtocols(IProtocolLoader loader, ResultProvider provider, bool isNewProvider)
		{
			try
			{
				IEnumerable<PollProtocol> results = loader.LoadProtocols(Repository);
				int countTotal = 0;
				int countNew = 0;
				int countUpdated = 0;
				foreach (PollProtocol protocol in results)
				{
					if (Tcs.IsCancellationRequested)
						return;
					Debug.Assert(protocol.Region != null);
					protocol.Provider = provider;
					saveProtocol(protocol, isNewProvider, ref countTotal, ref countNew, ref countUpdated);
				}
				log(String.Format("All protocols loaded. Total: {0}, new: {1}, updated: {2}", countTotal, countNew, countUpdated));
			}
			finally
			{
				loader.Dispose();
			}
		}

		protected void saveProtocol(PollProtocol protocol, bool isNewProvider, ref int countTotal, ref int countNew, ref int countUpdated)
		{
			PollProtocol existingProtocol = null;
			bool needSave = true;
			if (!isNewProvider)
			{
				existingProtocol = Repository.LoadProtocol(protocol);
			}
			if (existingProtocol != null)
			{
				needSave = !existingProtocol.EqualsTo(protocol);
				if (needSave)
					countUpdated++;
				protocol.Id = existingProtocol.Id;
			}
			else
				countNew++;

			if (needSave)
			{
				Repository.SaveProtocol(protocol,
										new ProtocolSaveOption
										{
											UnknownRegionAction = UnknownRegionAction,
											UnknownComissionAction = UnknownComissionAction,
											DownloadNotifier = new DownloadNotifier(Logger),
											CancellationToken = Tcs.Token
										});

				log(string.Format("#{0} protocol saved. Region: {1}, comission: {2}. Images: {3}",
								  countTotal, protocol.Region.Name, protocol.Comission, protocol.Images.Count));
			}
			else
			{
				log(string.Format("#{0} protocol skipped. Region: {1}, comission: {2}. Images: {3}",
								  countTotal, protocol.Region.Name, protocol.Comission, protocol.Images.Count));
			}
			countTotal++;
		}

		protected void tearDown(Task t)
		{
			IsLoading = false;
			try
			{
				LastError = t.Exception != null ? t.Exception.InnerException : null;
				if (t.Exception != null)
				{
					var originalEx = t.Exception.GetBaseException();
					if (!(originalEx is OperationCanceledException))
						log(originalEx.ToString());
				}
				
			}
			catch (Exception ex)
			{
				// we don't want an unobserved task exception so swallow the exception
				try
				{
					logError("Error in tear down logic: " + ex);
				}
				catch(Exception ex2)
				{
					Trace.WriteLine(ex2.ToString());
				}
			}
		}
	}
}