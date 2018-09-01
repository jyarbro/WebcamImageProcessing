using KIP7.FrameRate;
using KIP7.Logger;
using Microsoft.VisualStudio.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace KIP7.ImageProcessors {
	public abstract class ImageProcessor : IAsyncDisposable {
		public SoftwareBitmapSource ImageSource = new SoftwareBitmapSource();

		protected readonly ILogger Logger;
		protected readonly IFrameRateManager FrameRateManager;
		protected readonly CoreDispatcher Dispatcher;

		SoftwareBitmap BackBuffer;
		bool SwappingActiveImage = false;

		public ImageProcessor(
			ILogger logger,
			IFrameRateManager frameRateManager,
			CoreDispatcher dispatcher
		) {
			Logger = logger;
			FrameRateManager = frameRateManager;
			Dispatcher = dispatcher;
		}

		public async Task ProcessFrameAsync(MediaFrameReference frame) {
			if (frame is null)
				return;

			var softwareBitmap = await ConvertFrameAsync(frame.VideoMediaFrame);

			if (softwareBitmap is null)
				return;

			// Swap out the existing BackBuffer reference with the new one.
			softwareBitmap = Interlocked.Exchange(ref BackBuffer, softwareBitmap);

			// Dispose of the old BackBuffer data.
			softwareBitmap?.Dispose();

			await SwapActiveImageAsync();
		}

		public async Task SwapActiveImageAsync() {
			if (SwappingActiveImage)
				return;

			SwappingActiveImage = true;

			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
				SoftwareBitmap latestBitmap;

				// Keep draining frames from the backbuffer until the backbuffer is empty.
				while ((latestBitmap = Interlocked.Exchange(ref BackBuffer, null)) != null) {
					await ImageSource.SetBitmapAsync(latestBitmap);
					latestBitmap.Dispose();
				}
			});

			SwappingActiveImage = false;
		}

		public abstract Task InitializeAsync(MediaCapture mediaCapture);
		public abstract Task<SoftwareBitmap> ConvertFrameAsync(VideoMediaFrame videoMediaFrame);
		public abstract Task DisposeAsync();
	}
}
