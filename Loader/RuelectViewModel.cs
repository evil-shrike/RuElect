using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public class RuelectViewModel : ImportViewModelBase
	{
		private string m_providerName;
		private string m_fileName;
		private bool m_bDownloadImage;

		/// <summary>
		/// Only for design-time support. DO NOT USE
		/// </summary>
		public RuelectViewModel(): base(null)
		{}

		public RuelectViewModel(ILogger logger): base(logger)
		{
			//LoadRegionsCommand = new DelegateCommand(onLoadRegions);
			CheckRegionsCommand = new DelegateCommand(onCheckRegions);
			LoadResultsCommand = new DelegateCommand(() => loadProtocols());
			ProviderName = "ruelect";
		}

		public string ProviderName
		{
			get { return m_providerName; }
			set
			{
				m_providerName = value;
				raisePropertyChangedEvent("FileName");
			}
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

						bool isNewProvider;
						ResultProvider provider = Repository.GetOrCreateProvider(Path.GetFileName(FileName), out isNewProvider);

						var poll = ensurePollCreated();
						provider.Poll = poll;

						if (Tcs.IsCancellationRequested)
							return;

						var loader = new RuelectCsvLoader(FileName);
						loadProtocols(loader, provider, isNewProvider);
					}, TaskCreationOptions.LongRunning)
				.ContinueWith(tearDown);
			
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