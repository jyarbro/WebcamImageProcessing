using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIP2.Models;

namespace KIP2.ViewModels {
	public class VisualSensorViewModel : ViewModelBase {
		public double FramesPerSecond {
			get { return _FramesPerSecond; }
			set { SetProperty(ref _FramesPerSecond, value); }
		}
		double _FramesPerSecond = 0;

		public double FrameLag {
			get { return _FrameLag; }
			set { SetProperty(ref _FrameLag, value); }
		}
		double _FrameLag = 0;

		public WriteableBitmap OutputImage {
			get { return _OutputImage; }
			set { SetProperty(ref _OutputImage, value); }
		}
		WriteableBitmap _OutputImage;

		public VisualSensorManager VisualSensorManager { get; set; }

		public VisualSensorViewModel() {
			OutputImage = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);

			VisualSensorManager = new VisualSensorManager {
				FilteredImage = OutputImage
			};

			VisualSensorManager.UpdateFrameRate += UpdateFrameRate;
		}

		private void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}