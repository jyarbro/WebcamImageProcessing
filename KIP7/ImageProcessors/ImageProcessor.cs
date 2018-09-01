using Windows.Media.Capture.Frames;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace KIP7.ImageProcessors {
	public abstract class ImageProcessor {
		protected Image ImageElement;
		protected SoftwareBitmapSource ImageSource;

		public ImageProcessor(Image imageElement) {
			ImageElement = imageElement;
			ImageSource = new SoftwareBitmapSource();
			ImageElement.Source = ImageSource;
		}

		public abstract void ProcessFrame(MediaFrameReference frame);
	}
}
