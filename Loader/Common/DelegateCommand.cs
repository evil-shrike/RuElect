using System;
using System.Windows.Input;

namespace Elect.Loader.Common
{
	public class DelegateCommand: ICommand
	{
		private Action m_action;
		private Action<Object> m_actionParam;
		public DelegateCommand(Action action)
		{
			m_action = action;
		}

/*		public DelegateCommand(Func<object> action)
		{
			m_action = () => action();
		}*/

		public DelegateCommand(Action<object> action)
		{
			m_actionParam = action;
		}

		public void Execute(object parameter)
		{
			if (m_action != null)
				m_action();
			else
				m_actionParam(parameter);
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public event EventHandler CanExecuteChanged;
	}
}