using System.ComponentModel;
using System.Reflection;

namespace Elect.Loader.Common
{
	/// <summary>
	/// Базовый класс для объектов, реализующих INotifyPropertyChanged.
	/// </summary>
	public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
	{
		///<summary>
		/// Occurs when a property value changes.
		///</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Вызов обработчиков события PropertyChanged.
		/// </summary>
		/// <param name="eventArgs">аргументы события.</param>
		protected internal void raisePropertyChangedEvent(PropertyChangedEventArgs eventArgs)
		{
			XNotifyPropertyChanged.RaisePropertyChangedEvent(this, PropertyChanged, eventArgs);
		}

		/// <summary>
		/// Вызов обработчиков события PropertyChanged.
		/// </summary>
		/// <param name="propertyName">наименование свойства.</param>
		protected void raisePropertyChangedEvent(string propertyName)
		{
			XNotifyPropertyChanged.RaisePropertyChangedEvent(this, PropertyChanged, propertyName);
		}

		/*
		/// <summary>
		/// Вызов обработчиков события PropertyChanged.
		/// </summary>
		/// <param name="expr">Ламда выражение доступа к свойству.</param>
		protected void raisePropertyChangedEvent(Expression<Func<Object>> expr)
		{
			var propertyName = ReflectionUtils.GetPropertyName(expr);
			XNotifyPropertyChanged.RaisePropertyChangedEvent(this, PropertyChanged, propertyName);
		}
		*/

		/// <summary>
		/// Вызов обработчиков события PropertyChanged.
		/// </summary>
		/// <param name="setter">сеттер изменяемого свойства.</param>
		protected void raisePropertyChangedEvent(MethodBase setter)
		{
			XNotifyPropertyChanged.RaisePropertyChangedEvent(this, PropertyChanged, setter);
		}

		/// <summary>
		/// Получение экземпляра PropertyChangedEventArgs по наименованию свойства.
		/// </summary>
		/// <param name="propertyName">наименование свойства.</param>
		/// <returns>экземпляр PropertyChangedEventArgs.</returns>
		public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
		{
			return XNotifyPropertyChanged.GetPropertyChangedEventArgs(propertyName);
		}
	}
}
