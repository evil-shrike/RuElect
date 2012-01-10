using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Elect.Loader
{
	public interface IDownloader
	{
		Task<byte[]> Download(string uri, IDownloadNotifier notifier, CancellationToken cancellationToken);
	}

	public class FileDownloader : IDownloader
	{
		public Task<byte[]> Download(string uri, IDownloadNotifier notifier, CancellationToken cancellationToken)
		{
			//var req = (HttpWebRequest)WebRequest.Create(uri);
			
			//var res = (HttpWebResponse)req.GetResponse();
			
			//return StreamUtil.ReadFully(res.GetResponseStream());

			var tcs = new TaskCompletionSource<byte[]>();
			var wc = new WebClient();

			if (notifier != null)
			{
				notifier.ReportStart();
				wc.DownloadProgressChanged +=
					(s, ea) =>
						{
							notifier.ReportProgress(ea.BytesReceived, ea.TotalBytesToReceive);
							if (cancellationToken.IsCancellationRequested)
								wc.CancelAsync();
						};
			}

			wc.DownloadDataCompleted +=
				(s, ea) =>
					{
						if (notifier != null)
							notifier.ReportFinish();
						if (ea.Error != null)
							tcs.SetException(ea.Error);
						else if (ea.Cancelled)
							tcs.SetCanceled();
						else
							tcs.SetResult(ea.Result);
					};
			wc.DownloadDataAsync(new Uri(uri));

			return tcs.Task;
		}
	}
}