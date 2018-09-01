using KIP7.Helpers;
using System;
using System.Threading;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace KIP7.ImageProcessors.ColorCamera {
	public class ColorCameraProcessor {
		Image ImageElement;
		SoftwareBitmapSource ImageSource;
		SoftwareBitmap BackBuffer;
		bool TaskIsRunning = false;

		public ColorCameraProcessor(Image imageElement) {
			ImageElement = imageElement;
			ImageSource = new SoftwareBitmapSource();
			ImageElement.Source = ImageSource;
		}

		public void ProcessFrame(MediaFrameReference frame) {
			if (frame is null)
				return;

			var softwareBitmap = FrameConverter.ConvertToDisplayableImage(frame.VideoMediaFrame);

			if (softwareBitmap is null)
				return;

			softwareBitmap = Interlocked.Exchange(ref BackBuffer, softwareBitmap);

			// UI thread always reset BackBuffer before using it.  Unused bitmap should be disposed.
			softwareBitmap?.Dispose();

			var task = ImageElement.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
				async () => {
					if (TaskIsRunning)
						return;

					TaskIsRunning = true;

					// Keep draining frames from the backbuffer until the backbuffer is empty.
					SoftwareBitmap latestBitmap;

					while ((latestBitmap = Interlocked.Exchange(ref BackBuffer, null)) != null) {
						await ImageSource.SetBitmapAsync(latestBitmap);
						latestBitmap.Dispose();
					}

					TaskIsRunning = false;
				});
		}
	}
}
