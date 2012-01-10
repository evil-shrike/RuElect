using System.Net;
using Croc.XFW3.Utils;

namespace Elect.Loader
{
	public interface IDownloader
	{
		byte[] Download(string uri);
	}

	public class FileDownloader : IDownloader
	{
		public byte[] Download(string uri)
		{
			WebRequest req = WebRequest.Create(uri);
			WebResponse res = req.GetResponse();
			return StreamUtil.ReadFully(res.GetResponseStream());
		}
	}
}