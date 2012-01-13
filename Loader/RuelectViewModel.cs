using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Elect.DomainObjects;
using Elect.Loader.Common;
//using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

namespace Elect.Loader
{
	public class RuelectViewModel : ImportViewModelBase
	{
		private const string DefaultProviderName = "ruelect";
		private string m_providerName;
		private string m_fileName;
		private bool m_bDownloadImage;
		private bool m_bDownloadCsv;

		/// <summary>
		/// Only for design-time support. DO NOT USE
		/// </summary>
		public RuelectViewModel(): base(null)
		{}

		public RuelectViewModel(ILogger logger): base(logger)
		{
			//LoadRegionsCommand = new DelegateCommand(onLoadRegions);
			CheckRegionsCommand = new DelegateCommand(() => checkRegions());
			LoadResultsCommand = new DelegateCommand(() => loadProtocols());
			ProviderName = DefaultProviderName;
			FileName = Properties.Settings.Default.RuelectImportFilePath;
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

		public Boolean DownloadCsv
		{
			get { return m_bDownloadCsv; }
			set
			{
				m_bDownloadCsv = value;
				raisePropertyChangedEvent("DownloadCsv");
			}
		}

		public string FileName
		{
			get { return m_fileName; }
			set
			{
				m_fileName = value;
				raisePropertyChangedEvent("FileName");
				Properties.Settings.Default.RuelectImportFilePath = value;
			}
		}

		public IDownloader Downloader { get; set; }

		public bool DownloadImage
		{
			get { return m_bDownloadImage; }
			set
			{
				m_bDownloadImage = value;
				if (value)
					Repository.Downloader = Downloader;
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

		private Task checkRegions()
		{
			Tcs = new CancellationTokenSource();

			TaskLoading = Task.Factory.StartNew(
				() =>
					{
						if (!ensureFileNameSpecified()) return;
						Repository.Initialize();
						if (DownloadCsv)
						{
							FileName = downloadAndSaveCsv();
							if (String.IsNullOrEmpty(FileName))
								return;
						}
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
					}, TaskCreationOptions.LongRunning)
				.ContinueWith(tearDown);

			return TaskLoading;
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
						if (DownloadCsv)
						{
							FileName = downloadAndSaveCsv();
							if (String.IsNullOrEmpty(FileName ))
								return;
						}
						bool isNewProvider;
						string providerName = String.IsNullOrWhiteSpace(ProviderName) ? DefaultProviderName : ProviderName;
						ResultProvider provider = Repository.GetOrCreateProvider(providerName , out isNewProvider);

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

		private string downloadAndSaveCsv()
		{
			const string CsvProtocolsUri = "http://ruelect.com/ruelect_protokol.zip";

			Task<byte[]> taskDownload = Downloader.Download(CsvProtocolsUri, new DownloadNotifier(Logger), Tcs.Token);
			try
			{
				taskDownload.Wait();
			}
			catch (Exception ex)
			{
				Tcs.Token.ThrowIfCancellationRequested();
				throw;
			}
			var fileContent = taskDownload.Result;
			var dlg = new SaveFileDialog();
			dlg.FileName = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ruelect_protokol.zip");
			if (dlg.ShowDialog().GetValueOrDefault(false))
			{
				using (Stream stream = dlg.OpenFile())
				{
					stream.Write(fileContent, 0, fileContent.Length);
					stream.Seek(0, SeekOrigin.Begin);
					// decompress
					string baseFolder = Path.GetDirectoryName(dlg.FileName);
					using (var decompressor = new SharpZipLibDecompressor())
					{
						foreach (string fileName in decompressor.GetFileNames(stream))
						{
							string targetPath = Path.Combine(baseFolder, fileName);
							using (var csvArchiveStream = decompressor.GetFile(stream, fileName))
							using (var csvFileStream = new FileStream(targetPath, FileMode.Create))
								StreamUtil.CopyFully(csvArchiveStream, csvFileStream);
							return targetPath;
						}
					}
				}
			}
			return null;
		}

		private bool ensureFileNameSpecified()
		{
			if (String.IsNullOrWhiteSpace(FileName) && !DownloadCsv)
			{
				log("File name should be specified or 'Download automatically' option should be checked");
				return false;
			}
			return true;
		}
	}

	public class SharpZipLibDecompressor : IDisposable
	{
		private const Int32 DEFAULT_BUFFER_SIZE = 0x2000;

		/// <summary>
		/// Размер разархивируемого файла в байтах, при превышении которого файл разархивируется
		/// во временную папку.
		/// </summary>
		private const Int32 EXTRACT_TO_FILE_THRESHOLD = 0x4000;	// 16 КБ

		/// <summary>
		/// Расширение временных файлов.
		/// </summary>
		private const String TEMP_FILE_EXT = ".TMP";

		public IList<String> GetFileNames(Stream archiveStream)
		{
			ensureArchiveStreamCanReadAndSeek(archiveStream);

			var fileNames = new List<String>();

			long oldPosition = archiveStream.Position;
			archiveStream.Position = 0;
			try
			{
				using (var zipInputStream = new ZipInputStream(archiveStream) { IsStreamOwner = false }) // мы не закрываем поток
				{
					ZipEntry zipEntry;
					while ((zipEntry = zipInputStream.GetNextEntry()) != null)
					{
						if (zipEntry.IsFile)
							fileNames.Add(zipEntry.Name);
					}
				}
			}
			finally
			{
				archiveStream.Position = oldPosition;
			}

			return fileNames.ToArray();
		}

		public Stream GetFile(Stream archiveStream, String fileName)
		{
			ensureArchiveStreamCanReadAndSeek(archiveStream);
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName");

			long oldPosition = archiveStream.Position;
			archiveStream.Position = 0;

			try
			{
				using (var zipInputStream = new ZipInputStream(archiveStream) { IsStreamOwner = false }) // мы не закрываем поток
				{
					ZipEntry zipEntry;
					while ((zipEntry = zipInputStream.GetNextEntry()) != null)
					{
						if (!zipEntry.IsFile)
							continue;

						if (Path.IsPathRooted(zipEntry.Name))
						{
							if (Path.GetFileName(zipEntry.Name) != fileName)
								continue;
						}
						else
						{
							if (zipEntry.Name != fileName)
								continue;
						}

						if (!zipEntry.CanDecompress)
							throw new InvalidOperationException("Can not decompress file");

						if (!zipEntry.IsCompressionMethodSupported())
							throw new InvalidOperationException("Can not decompress file: compression method not supported");

						return getDecompressedStream(zipInputStream, zipEntry);
					}
				}
			}
			finally
			{
				archiveStream.Position = oldPosition;
			}

			return null;
		}

		private void ensureArchiveStreamCanReadAndSeek(Stream archiveStream)
		{
			if (archiveStream == null)
			throw new ArgumentNullException("archiveStream");

			if (!archiveStream.CanRead)
				throw new ArgumentException("Can not read from archiveStream", "archiveStream");

			if (!archiveStream.CanSeek)
				throw new ArgumentException("archiveStream does not support seek operation", "archiveStream");
		}

		private Stream getDecompressedStream(ZipInputStream zipInputStream, ZipEntry zipEntry)
		{
			Debug.Assert(zipInputStream.CanRead);
			Debug.Assert(zipEntry.IsFile);

			Stream streamToExtractTo = getStreamToExtractTo(zipEntry);
			Debug.Assert(streamToExtractTo.CanSeek);

			StreamUtil.CopyFully(zipInputStream, streamToExtractTo, new byte[DEFAULT_BUFFER_SIZE]);

			streamToExtractTo.Position = 0;

			return streamToExtractTo;
		}

		private Stream getStreamToExtractTo(ZipEntry zipEntry)
		{
			if (zipEntry.Size <= EXTRACT_TO_FILE_THRESHOLD)
				return new MemoryStream((Int32)zipEntry.Size);

			string tempFilePath = getTempFilePath();
			return new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		}

		private String getTempFilePath()
		{
			string tempFolderForArchive = getOrCreateTempFolderForArchive();
			string tempFilePath = Path.Combine(tempFolderForArchive, Guid.NewGuid().ToString("N") + TEMP_FILE_EXT);

			return tempFilePath;
		}

		private String getOrCreateTempFolderForArchive()
		{
			string tempFolderPath = getInstanceTempFolder();
			if (!Directory.Exists(tempFolderPath))
				Directory.CreateDirectory(tempFolderPath);

			return tempFolderPath;
		}

		private String getInstanceTempFolder()
		{
			return Path.Combine(Path.GetTempPath(), "~" + typeof(SharpZipLibDecompressor).Name + "." + GetHashCode());
		}

		public void Dispose()
		{
			string instanceTempFolder = getInstanceTempFolder();
			if (Directory.Exists(instanceTempFolder))
			{
				try
				{
					Directory.Delete(instanceTempFolder, true);
				}
				catch (Exception)
				{
					// не удалось удалить временный каталог, проигнорируем
				}
			}
		}
	}

}