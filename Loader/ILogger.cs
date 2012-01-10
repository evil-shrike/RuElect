using Elect.Loader.Common;

namespace Elect.Loader
{
	public interface ILogger
	{
		void Log(LogItem item);
		ILogItemProgress LogProgress();
	}

	public static class LoggerExtensions
	{
		public static void Log(this ILogger logger, string msg)
		{
			logger.Log(new LogItem() {Message = msg, Severity = LogItemSeverity.Info});
		}

		public static void LogError(this ILogger logger, string msg)
		{
			logger.Log(new LogItem() { Message = msg, Severity = LogItemSeverity.Error });
		}

		public static void LogWarning(this ILogger logger, string msg)
		{
			logger.Log(new LogItem() { Message = msg, Severity = LogItemSeverity.Warn });
		}
	}

	public enum LogItemSeverity
	{
		Info = 0,
		Warn,
		Error
	}

	public class LogItem : NotifyPropertyChangedBase
	{
		private string m_message;
		private LogItemSeverity m_severity;

		public string Message
		{
			get { return m_message; }
			set
			{
				m_message = value;
				raisePropertyChangedEvent("Message");
			}
		}

		public LogItemSeverity Severity
		{
			get { return m_severity; }
			set
			{
				m_severity = value;
				raisePropertyChangedEvent("Severity");
			}
		}
	}

	public interface ILogItemProgress
	{
		void SetProgress(long received, long total);

		void Complete();
	}
}