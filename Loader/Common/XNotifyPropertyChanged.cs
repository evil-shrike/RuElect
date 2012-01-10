using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Elect.Loader.Common
{
	/// <summary>
	/// ����� �������� ����������� ��������������� ������ ��������� �������
	/// </summary>
	public static class XNotifyPropertyChanged
	{
		/// <summary>
		/// ������� ����������� PropertyChangedEventArgs
		/// ���� - ������������ ��������
		/// </summary>
		private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> s_dictionary =
			new ConcurrentDictionary<string, PropertyChangedEventArgs>();

		/// <summary>
		/// ��������� ���������� PropertyChangedEventArgs �� ������������ ��������
		/// </summary>
		/// <param name="propertyName">������������ ��������</param>
		/// <returns>��������� PropertyChangedEventArgs</returns>
		public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
		{
			PropertyChangedEventArgs eventArgs = s_dictionary
				.GetOrAdd(propertyName, (name) => new PropertyChangedEventArgs(name));
			return eventArgs;
		}

		/// <summary>
		/// ����� ������������ ������� PropertyChanged
		/// </summary>
		/// <param name="sender">�������� �������</param>
		/// <param name="handler">����������</param>
		/// <param name="eventArgs">��������� �������</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler,  PropertyChangedEventArgs eventArgs)
		{
			if (handler != null)
				handler(sender, eventArgs);
		}

		/// <summary>
		/// ����� ������������ ������� PropertyChanged
		/// </summary>
		/// <param name="sender">�������� �������</param>
		/// <param name="handler">����������</param>
		/// <param name="setter">������ ����������� ��������</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler, MethodBase setter)
		{
			// ��� ������� �������� ��� ��������� ���� ���������� ������ �����
			// set_<�����������>. 4 ��� ����� �������� "set_"
			// ������� ��� 4 ������� �� ������ ������������ ������� ������� ��� ��������
			const int SETTER_PREFIX_LEN = 4;
			if (handler != null)
				handler(sender, GetPropertyChangedEventArgs(setter.Name.Substring(SETTER_PREFIX_LEN)));
		}

		/// <summary>
		/// ����� ������������ ������� PropertyChanged
		/// </summary>
		/// <param name="sender">�������� �������</param>
		/// <param name="handler">����������</param>
		/// <param name="propertyName">������������ ��������</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler, string propertyName)
		{
			if (handler != null)
				handler(sender, GetPropertyChangedEventArgs(propertyName));
		}
	}
}