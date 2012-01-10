namespace Elect.Loader
{
	public interface IDownloadNotifier
	{
		void ReportStart();
		void ReportProgress(long received, long total);
		void ReportFinish();
	}

	public class DownloadNotifier : IDownloadNotifier
	{
		private readonly ILogger m_logger;
		private ILogItemProgress m_logItem;

		public DownloadNotifier(ILogger logger)
		{
			m_logger = logger;
		}

		public void ReportStart()
		{
			m_logItem = m_logger.LogProgress();
		}

		public void ReportProgress(long received, long total)
		{
			if (m_logItem != null)
				m_logItem.SetProgress(received, total);
		}

		public void ReportFinish()
		{
			if (m_logItem != null)
				m_logItem.Complete();
		}
	}
}