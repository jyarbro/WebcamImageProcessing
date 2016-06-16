using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIP3.Helpers;
using KIP3.Models;

namespace KIP3.Views {
	public class MainWindowViewModel : Observable {
		public string StatusText {
			get { return _StatusText ?? (_StatusText = string.Empty); }
			set { SetProperty(ref _StatusText, value); }
		}
		string _StatusText;

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

		public StreamManager StreamManager { get; set; }

		public MainWindowViewModel() {
			OutputImage = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);

			StreamManager = new StreamManager {
				FilteredImage = OutputImage
			};

			StreamManager.Load();
			StreamManager.UpdateFrameRate += UpdateFrameRate;
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}