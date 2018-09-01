using KIP7.Helpers;
using System;
using System.Threading;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace KIP7.ImageProcessors.ColorCamera {
	public class ColorCameraProcessor : ImageProcessor {
		SoftwareBitmap BackBuffer;
		bool TaskIsRunning = false;

		public ColorCameraProcessor(Image imageElement) : base(imageElement) { }

		public override void ProcessFrame(MediaFrameReference frame) {
			if (frame is null)
				return;

			SwapBuffer(frame.VideoMediaFrame);

			var task = ImageElement.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, SwapActiveImage);
		}

		public void SwapBuffer(VideoMediaFrame videoMediaFrame) {
			var softwareBitmap = FrameConverter.ConvertToDisplayableImage(videoMediaFrame);

			if (softwareBitmap is null)
				return;

			softwareBitmap = Interlocked.Exchange(ref BackBuffer, softwareBitmap);

			softwareBitmap?.Dispose();
		}

		public async void SwapActiveImage() {
			if (TaskIsRunning)
				return;

			TaskIsRunning = true;

			SoftwareBitmap latestBitmap;

			// Keep draining frames from the backbuffer until the backbuffer is empty.
			while ((latestBitmap = Interlocked.Exchange(ref BackBuffer, null)) != null) {
				await ImageSource.SetBitmapAsync(latestBitmap);
				latestBitmap.Dispose();
			}

			TaskIsRunning = false;
		}
	}
}
