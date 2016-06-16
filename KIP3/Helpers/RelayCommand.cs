using System;
using System.Windows.Input;

namespace KIP3.Helpers {
	public class RelayCommand : ICommand {
		public event EventHandler CanExecuteChanged {
			add {
				CommandManager.RequerySuggested += value;
				_CanExecuteChanged += value;
			}

			remove {
				CommandManager.RequerySuggested -= value;
				_CanExecuteChanged -= value;
			}
		}
		event EventHandler _CanExecuteChanged;

		Action _executeAction;
		bool _canExecute;

		public RelayCommand() { }

		public RelayCommand(Action executeAction) : this(executeAction, true) { }

		public RelayCommand(Action executeAction, bool canExecute) {
			_executeAction = executeAction;
			_canExecute = canExecute;
		}

		public void OnCanExecuteChanged() {
			EventHandler handler = _CanExecuteChanged;

			if (handler != null)
				handler.Invoke(this, EventArgs.Empty);
		}

		public virtual bool CanExecute(object parameter) {
			return _canExecute;
		}

		public virtual void Execute(object parameter) {
			_executeAction();
		}
	}


	public class RelayCommand<T> : RelayCommand {
		Action<T> _executeAction;
		Predicate<T> _canExecute;
	
		public RelayCommand(Action<T> executeAction) : this(executeAction, DefaultCanExecute) { }

		public RelayCommand(Action<T> executeAction, Predicate<T> canExecute) {
			_executeAction = executeAction;
			_canExecute = canExecute;
		}

		public override bool CanExecute(object parameter) {
			return _canExecute != null && _canExecute((T)parameter);
		}

		public override void Execute(object parameter) {
			_executeAction((T)parameter);
		}

		static bool DefaultCanExecute(T parameter) {
			return true;
		}
	}
}