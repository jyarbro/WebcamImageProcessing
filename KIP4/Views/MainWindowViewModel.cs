using KIP.Helpers;
using KIP4.Services;
using System.ComponentModel;
using System.Windows.Media;

namespace KIP4.Views {
	class MainWindowViewModel : Observable {
		public SensorService SensorService { get; }
		public ImageSource ImageSource { get; }

		public string StatusText {
			get => _StatusText;
			set => SetProperty(ref _StatusText, value);
		}
		string _StatusText = string.Empty;

		public double FramesPerSecond {
			get => _FramesPerSecond;
			set => SetProperty(ref _FramesPerSecond, value);
		}
		double _FramesPerSecond = 0;

		public double FrameLag {
			get => _FrameLag;
			set => SetProperty(ref _FrameLag, value);
		}
		double _FrameLag = 0;

		public MainWindowViewModel() {
			SensorService = SensorService.Create();
			ImageSource = SensorService.OutputImage;

			SensorService.PropertyChanged += StreamManager_PropertyChanged;
			SensorService.UpdateFrameRate += UpdateFrameRate;
		}

		void StreamManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == nameof(SensorService.StatusText))
				StatusText = ((SensorService) sender).StatusText;
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}