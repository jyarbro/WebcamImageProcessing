using KIP.Helpers;
using KIP5.ImageProcessors;
using KIP5.Interfaces;
using KIP5.Services;
using System.ComponentModel;
using System.Windows.Media;

namespace KIP5.ViewModels {
	class MainWindowViewModel : Observable {
		public ImageSource CameraRaw { get; }

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
			var sensorReader = new SensorReader();
			sensorReader.StatusChanged += StatusTextChanged;

			var cameraRaw = new CameraRaw(sensorReader);
			CameraRaw = cameraRaw.OutputImage;
		}

		void StatusTextChanged(object sender, PropertyChangedEventArgs args) {
			if (sender is IStatusTracker && args.PropertyName == nameof(IStatusTracker.StatusText))
				StatusText = ((IStatusTracker) sender).StatusText;
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}