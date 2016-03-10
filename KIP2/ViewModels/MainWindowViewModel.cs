namespace KIP2.ViewModels {
	public class MainWindowViewModel : ViewModelBase {
		public string StatusText {
			get {
				return _StatusText ?? (_StatusText = string.Empty);
			}
			set {
				if (_StatusText == value)
					return;

				_StatusText = value;
				OnPropertyChanged();
			}
		}
		string _StatusText;

		public ViewModelBase ContentViewModel { get; set; }

		public MainWindowViewModel() {
			ContentViewModel = new VisualSensorViewModel();
		}
	}
}