using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Elect.Loader.Common
{
	/// <summary>
	/// Класс содержит статические вспомогательные методы генерации событий
	/// </summary>
	public static class XNotifyPropertyChanged
	{
		/// <summary>
		/// Словарь экземпляров PropertyChangedEventArgs
		/// ключ - наименование свойства
		/// </summary>
		private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> s_dictionary =
			new ConcurrentDictionary<string, PropertyChangedEventArgs>();

		/// <summary>
		/// Получение экземпляра PropertyChangedEventArgs по наименованию свойства
		/// </summary>
		/// <param name="propertyName">наименование свойства</param>
		/// <returns>экземпляр PropertyChangedEventArgs</returns>
		public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
		{
			PropertyChangedEventArgs eventArgs = s_dictionary
				.GetOrAdd(propertyName, (name) => new PropertyChangedEventArgs(name));
			return eventArgs;
		}

		/// <summary>
		/// Вызов обработчиков события PropertyChanged
		/// </summary>
		/// <param name="sender">источник события</param>
		/// <param name="handler">обработчик</param>
		/// <param name="eventArgs">аргументы события</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler,  PropertyChangedEventArgs eventArgs)
		{
			if (handler != null)
				handler(sender, eventArgs);
		}

		/// <summary>
		/// Вызов обработчиков события PropertyChanged
		/// </summary>
		/// <param name="sender">источник события</param>
		/// <param name="handler">обработчик</param>
		/// <param name="setter">сеттер изменяемого свойства</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler, MethodBase setter)
		{
			// Для каждого свойства при генерации кода компилятор создаёт метод
			// set_<ИмяСвойства>. 4 это длина префикса "set_"
			// откусив эти 4 символа от начала наименования сеттера получим имя свойства
			const int SETTER_PREFIX_LEN = 4;
			if (handler != null)
				handler(sender, GetPropertyChangedEventArgs(setter.Name.Substring(SETTER_PREFIX_LEN)));
		}

		/// <summary>
		/// Вызов обработчиков события PropertyChanged
		/// </summary>
		/// <param name="sender">источник события</param>
		/// <param name="handler">обработчик</param>
		/// <param name="propertyName">наименование свойства</param>
		public static void RaisePropertyChangedEvent(object sender, PropertyChangedEventHandler handler, string propertyName)
		{
			if (handler != null)
				handler(sender, GetPropertyChangedEventArgs(propertyName));
		}
	}
}