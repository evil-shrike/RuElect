using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Elect.Common;
using Elect.DomainObjects;

namespace Elect.Loader
{
	public abstract class ImportViewModelBase: NotifyPropertyChangedBase
	{
		private readonly IIlogger m_logger;
		private bool m_isLoading;

		protected ImportViewModelBase(IIlogger logger)
		{
			m_logger = logger;
			StopCommand = new DelegateCommand(onStop);
			ClearLogCommand = new DelegateCommand(logger.Clear);
		}

		public ICommand StopCommand { get; set; }

		public ICommand ClearLogCommand { get; set; }

		public Repository Repository { get; set; }

		protected Task TaskLoading;

		protected CancellationTokenSource Tcs;

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
			log("Cancelling");
			TaskLoading.ContinueWith((t) => log("Canceled"));
			Tcs.Cancel();
		}

		protected void log(string msg)
		{
			// taking into account unit tests
			if (Application.Current != null && Application.Current.Dispatcher != null)
			{
				Action action = () => m_logger.Log(msg);
				Application.Current.Dispatcher.Invoke(action);
			}
			else
			{
				m_logger.Log(msg);
			}
		}
	}

	public class KartaitogovViewModel : ImportViewModelBase
	{
		private string m_fileName;

		public KartaitogovViewModel(IIlogger logger)
			: base(logger)
		{
			DownloadImagesCommand  = new DelegateCommand(() => downloadImages());
		}

		public string FileName
		{
			get { return m_fileName; }
			set
			{
				m_fileName = value;
				raisePropertyChangedEvent("FileName");
			}
		}

		public ICommand DownloadImagesCommand { get; set; }
		public IDownloader Downloader { get; set; }

		internal Task downloadImages()
		{
			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						IsLoading = true;
						byte[] webpage = Downloader.Download("http://www.kartaitogov.ru/diff");
						log("Downloading of page http://www.kartaitogov.ru/diff completed. Size=" + webpage.Length);

						string webpageContent = Encoding.UTF8.GetString(webpage);
						Regex re = new Regex(@"http://files.kartaitogov.ru/p/([^/]+)/([^/]+)/(?:[^/""]+/)*(.+\.jpg)",
						                     RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
						var matches = re.Matches(webpageContent);
						foreach (Match match in matches)
						{
							if (match.Success)
							{
								var groups = match.Groups;
								string uri = match.Value;
								string region = groups[1].Value;
								string comission = groups[2].Value;
								string fileName = groups[3].Value;
								var dirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"KartaitogovImages", region, comission);
								Directory.CreateDirectory(dirPath );
								var fileBytes = Downloader.Download(uri);
								File.WriteAllBytes(Path.Combine(dirPath, fileName), fileBytes);
								log("Downloaded protocol image " + fileName + " (size=" + fileBytes.Length + ")");
								//var bld = new StringBuilder();
								//foreach (Group group in match.Groups)
								//{
								//    bld.Append(group.Value).Append(", ");
								//}
								//bld.Length -= 2;
								//log(match.Value + " : " + bld);
							}
						}
					}).ContinueWith(
					(t)=>
						{
							IsLoading = false;
						});
			return TaskLoading;
		}
	}

	public class RuelectViewModel : ImportViewModelBase
	{
		private string m_fileName;
		private bool m_bDownloadImage;

		public RuelectViewModel(IIlogger logger): base(logger)
		{
			//LoadRegionsCommand = new DelegateCommand(onLoadRegions);
			CheckRegionsCommand = new DelegateCommand(onCheckRegions);
			LoadResultsCommand = new DelegateCommand(() => loadProtocols());
		}

		public string FileName
		{
			get { return m_fileName; }
			set
			{
				m_fileName = value;
				raisePropertyChangedEvent("FileName");
			}
		}

		public bool DownloadImage
		{
			get { return m_bDownloadImage; }
			set
			{
				m_bDownloadImage = value;
				if (value)
					Repository.Downloader = new FileDownloader();
				else
					Repository.Downloader = null;
				raisePropertyChangedEvent("DownloadImage");
			}
		}

		//public ICommand LoadRegionsCommand { get; set; }
		public ICommand CheckRegionsCommand { get; set; }
		public ICommand LoadResultsCommand { get; set; }

/*
		private void onLoadRegions()
		{
			if (!ensureFileNameSpecified()) return;
			ensureRegionsLoaded();

			var loader = new RuelectCsvLoader(FileName);
			string[] regions = null;
			try
			{
				regions = loader.LoadRegions();
				log("Extracted " + regions.Length + " regions");
			}
			catch (InvalidDataException ex)
			{
				log(ex.ToString());
				//MessageBox.Show(ex.ToString());
				return;
			}
			finally
			{
				loader.Dispose();
			}

			var createCount = m_repository.UpdateRegions(regions);
			log("Created " + createCount + " regions");
		}
*/

		private void onCheckRegions()
		{
			if (!ensureFileNameSpecified()) return;
			Repository.Initialize();

			var loader = new RuelectCsvLoader(FileName);
			try
			{
				loader.CheckRegions(Repository);
				log("All regions from csv-file exist in DB");
			}
			catch (InvalidDataException ex)
			{
				log(ex.ToString());
				//MessageBox.Show(ex.ToString());
			}
			finally
			{
				loader.Dispose();
			}
		}

		internal Task loadProtocols()
		{
			Tcs = new CancellationTokenSource();
			
			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						if (!ensureFileNameSpecified()) return;
						IsLoading = true;
						Repository.Initialize();

						ResultProvider provider;
						bool isNew = false;
						string providerName = Path.GetFileName(FileName);
						if (!Repository.TryLoadProvider(providerName, out provider))
						{
							var providerId = Repository.CreateProvider(providerName, true);
							provider = new ResultProvider {Id = providerId, Name = providerName};
							log("Create a new provider: name=" + providerName + ", id=" + providerId);
							isNew = true;
						}


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
						provider.Poll = poll;

						if (Tcs.IsCancellationRequested)
							return;

						var loader = new RuelectCsvLoader(FileName);
						try
						{
							IEnumerable<PollProtocol> results = loader.LoadResults(Repository);
							int countTotal = 0;
							int countNew = 0;
							int countUpdated = 0;
							foreach (PollProtocol protocol in results)
							{
								if (Tcs.IsCancellationRequested)
									return;
								Debug.Assert(protocol.Region != null);
								protocol.Provider = provider;
								PollProtocol existingProtocol = null;
								bool needSave = true;
								if (!isNew)
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
									Repository.SaveProtocol(protocol);
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
							log(String.Format("All protocols loaded. Total: {0}, new: {1}, updated: {2}", countTotal, countNew, countUpdated));
						}
						finally
						{
							loader.Dispose();
						}
					}, TaskCreationOptions.LongRunning)
				.ContinueWith((t) =>
				              	{
				              		IsLoading = false;
				              		LastError = t.Exception != null ? t.Exception.InnerException : null;
				              		if (t.Exception != null)
				              			log(t.Exception.InnerException.Message);
				              	});
			return TaskLoading;
		}

		private bool ensureFileNameSpecified()
		{
			if (String.IsNullOrWhiteSpace(FileName))
			{
				log("File name should be specified");
				return false;
			}
			return true;
		}
	}
}