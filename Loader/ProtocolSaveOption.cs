using System.Threading;

namespace Elect.Loader
{
	public class ProtocolSaveOption
	{
		public UnknownRegionActions UnknownRegionAction;

		public UnknownComissionActions UnknownComissionAction;

		public IDownloadNotifier DownloadNotifier;

		public CancellationToken CancellationToken;
	}
}