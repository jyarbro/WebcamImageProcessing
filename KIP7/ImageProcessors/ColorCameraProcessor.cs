using KIP7.FrameRate;
using KIP7.Helpers;
using KIP7.Logger;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;

namespace KIP7.ImageProcessors {
	public class ColorCameraProcessor : ImageProcessor {
		bool AcquiringFrame;
		MediaCapture MediaCapture;
		MediaFrameReader FrameReader;

		public ColorCameraProcessor(
			ILogger logger,
			IFrameRateManager frameRateManager,
			CoreDispatcher dispatcher
		) : base(
			logger, 
			frameRateManager,
			dispatcher
		) { }

		public override async Task InitializeAsync(MediaCapture mediaCapture) {
			MediaCapture = mediaCapture;
			FrameReader = await FrameReaderLoader.GetFrameReaderAsync(mediaCapture, MediaFrameSourceKind.Color);
			FrameReader.FrameArrived += FrameArrived;

			var status = await FrameReader.StartAsync();

			if (status == MediaFrameReaderStartStatus.Success)
				Logger.Log($"Started MediaFrameReader.");
			else
				Logger.Log($"Unable to start MediaFrameReader. Error: {status}");
		}

		public override async Task<SoftwareBitmap> ConvertFrameAsync(VideoMediaFrame videoMediaFrame) {
			return await FrameConverter.ConvertToDisplayableImageAsync(videoMediaFrame);
		}

		public override async Task DisposeAsync() {
			FrameReader.FrameArrived -= FrameArrived;
			await FrameReader.StopAsync();
			FrameReader.Dispose();
			MediaCapture.Dispose();
			Logger.Log($"Disposed {nameof(ColorCameraProcessor)}.");
		}

		void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
			if (AcquiringFrame)
				return;

			AcquiringFrame = true;

			var frameStopWatch = Stopwatch.StartNew();

			using (var frame = sender.TryAcquireLatestFrame()) {
				var task = ProcessFrameAsync(frame);
			}

			frameStopWatch.Stop();

			FrameRateManager.Increment(frameStopWatch.ElapsedMilliseconds);

			AcquiringFrame = false;
		}
	}
}
