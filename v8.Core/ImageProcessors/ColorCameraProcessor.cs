using System.Diagnostics;
using v8.Core.Helpers;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v8.Core.ImageProcessors;

public class ColorCameraProcessor : ImageProcessor {
	bool AcquiringFrame;
	MediaCapture MediaCapture;
	MediaFrameReader FrameReader;

	public ColorCameraProcessor(
		ILogger logger,
		IFrameRateManager frameRateManager
	) : base(
		logger,
		frameRateManager
	) { }

	public async override Task InitializeAsync(MediaCapture mediaCapture) {
		MediaCapture = mediaCapture;
		FrameReader = await FrameReaderLoader.GetFrameReaderAsync(mediaCapture, MediaFrameSourceKind.Color);
		FrameReader.FrameArrived += FrameArrived;

		var status = await FrameReader.StartAsync();

		if (status == MediaFrameReaderStartStatus.Success) {
			Logger.Log($"Started MediaFrameReader.");
		}
		else {
			Logger.Log($"Unable to start MediaFrameReader. Error: {status}");
		}
	}

	public override SoftwareBitmap ConvertFrame(VideoMediaFrame frame) {
		try {
			// XAML requires Bgra8 with premultiplied alpha.
			return SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
		}
		catch (ObjectDisposedException) { }

		return null;
	}

	public async override Task DisposeAsync() {
		FrameReader.FrameArrived -= FrameArrived;
		await FrameReader.StopAsync();
		FrameReader.Dispose();
		MediaCapture.Dispose();
		Logger.Log($"Disposed {nameof(ColorCameraProcessor)}.");
	}

	void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
		if (AcquiringFrame) {
			return;
		}

		AcquiringFrame = true;

		var frameStopWatch = Stopwatch.StartNew();

		using (var frame = sender.TryAcquireLatestFrame()) {
			ProcessFrame(frame);
		}

		frameStopWatch.Stop();

		FrameRateManager.Increment(frameStopWatch.ElapsedMilliseconds);

		AcquiringFrame = false;
	}
}
