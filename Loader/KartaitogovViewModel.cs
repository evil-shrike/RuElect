using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Elect.DomainObjects;
using Elect.Loader.Common;

namespace Elect.Loader
{
	public class KartaitogovViewModel : ImportViewModelBase
	{
		private const string DefaultProviderName = "kartaitogov";
		private string m_providerName;
		private string m_imagesFolder;

		/// <summary>
		/// Only for design-time support. DO NOT USE
		/// </summary>
		public KartaitogovViewModel()
			: base(null)
		{}

		public KartaitogovViewModel(ILogger logger)
			: base(logger)
		{
			DownloadImagesCommand  = new DelegateCommand(() => downloadImagesAsync());
			ParseCommand = new DelegateCommand(() => parseResultsAsync());
			ProviderName = DefaultProviderName;
			ImagesFolder = Properties.Settings.Default.KartaitogovImageFolder;
			if (String.IsNullOrEmpty(ImagesFolder))
				ImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KartaitogovImages");
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

		public string ImagesFolder
		{
			get { return m_imagesFolder; }
			set
			{
				m_imagesFolder = value;
				raisePropertyChangedEvent("ImagesFolder");
				Properties.Settings.Default.KartaitogovImageFolder = value;
			}
		}

		public ICommand DownloadImagesCommand { get; set; }

		public ICommand ParseCommand { get; set; }

		public IDownloader Downloader { get; set; }

		internal Task parseResultsAsync()
		{
			Tcs = new CancellationTokenSource();

			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						IsLoading = true;
						Repository.Initialize();

						bool isNewProvider;
						string providerName = String.IsNullOrWhiteSpace(ProviderName) ? DefaultProviderName : ProviderName;
						ResultProvider provider = Repository.GetOrCreateProvider(providerName, out isNewProvider);

						var poll = ensurePollCreated();
						provider.Poll = poll;

						if (Tcs.IsCancellationRequested)
							return;

						string pageContent = downloadMainPage();

						if (Tcs.IsCancellationRequested)
							return;

						IDictionary<string, string> imageFilesMap = parseAndDownloadImages(pageContent);
						var loader = new KartaitogovWebLoader(pageContent, imageFilesMap, Logger);
						loadProtocols(loader, provider, isNewProvider);

					}).ContinueWith(
						(t) =>
						{
							IsLoading = false;
							LastError = t.Exception != null ? t.Exception.InnerException : null;
							if (t.Exception != null)
								log(t.Exception.GetBaseException().ToString());
						});
			return TaskLoading;
		}

		internal Task downloadImagesAsync()
		{
			Tcs = new CancellationTokenSource();

			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						if (String.IsNullOrEmpty(ImagesFolder))
						{
							log("You should specify folder for protocol images");
							return;
						}
						IsLoading = true;
						string pageContent = downloadMainPage();
						if (Tcs.IsCancellationRequested)
							return;

						parseAndDownloadImages(pageContent);
					})
				.ContinueWith(tearDown);

			return TaskLoading;
		}

		private string downloadMainPage()
		{
			Task<byte[]> taskDownloadPage = Downloader.Download("http://www.kartaitogov.ru/diff", new DownloadNotifier(Logger), Tcs.Token);
			try
			{
				taskDownloadPage.Wait();
			}
			catch (Exception ex)
			{
				Tcs.Token.ThrowIfCancellationRequested();
				throw;
			}
			byte[] pageContentBytes = taskDownloadPage.Result;
			log("Downloading of page http://www.kartaitogov.ru/diff has completed. Size=" + pageContentBytes.Length);
			string webpageContent = Encoding.UTF8.GetString(pageContentBytes);
			return webpageContent;
		}

		private IDictionary<string, string> parseAndDownloadImages(string webpageContent)
		{
			Regex re = new Regex(@"http://files.kartaitogov.ru/p/([^/]+)/([^/]+)/(?:[^/""]+/)*(.+\.jpg)",
					 RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
			var matches = re.Matches(webpageContent);
			var imageFilesMap = new Dictionary<string, string>();
			int countTotal = 0;
			int countNew = 0;
			log(String.Format("Start downloading all protocol images from http://www.kartaitogov.ru/diff. Files to process: {0}", matches.Count));
			foreach (Match match in matches)
			{
				var groups = match.Groups;
				string uri = match.Value;
				string region = groups[1].Value;
				string comission = groups[2].Value;
				string fileName = groups[3].Value;
				fileName = Uri.UnescapeDataString(fileName);
				var dirPath = Path.Combine(ImagesFolder, region, comission);
				var filePath = Path.Combine(dirPath, fileName);

				if (File.Exists(filePath))
				{
					log(String.Format("Image {0} has been skipped. (Already in: {1})", Uri.UnescapeDataString(uri), filePath));
				}
				else
				{
					Directory.CreateDirectory(dirPath);
					Task<byte[]> task = Downloader.Download(uri, new DownloadNotifier(Logger), Tcs.Token);
					try
					{
						task.Wait(Tcs.Token);
					}
					catch (Exception ex)
					{
						Tcs.Token.ThrowIfCancellationRequested();
						logWarning(String.Format("Error during downloading image {0}: {1}", Uri.UnescapeDataString(uri), ex));
						continue;
						//throw;
					}
					var fileBytes = task.Result;
					File.WriteAllBytes(filePath, fileBytes);
					log(String.Format("Downloaded image {0} (size:{1}) into {2} ", Uri.UnescapeDataString(uri), fileBytes.Length, filePath));
					countNew++;
				}
				imageFilesMap[uri] = filePath;
				countTotal++;
			}

			log("Downloading of protocol images has completed. Total files: " + countTotal + ", new: " + countNew);
			return imageFilesMap;
		}

	}
}