using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.Helpers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v9.Core.ImageProcessors;

public class ColorCameraProcessor(
		ILogger<ColorCameraProcessor> logger,
		IFrameRateHandler frameRateHandler
	) : ImageProcessor(
		logger,
		frameRateHandler
	) {

	bool AcquiringFrame;
	MediaCapture? MediaCapture;
	MediaFrameReader? FrameReader;

	public async override Task InitializeAsync(MediaCapture mediaCapture) {
		MediaCapture = mediaCapture;
		FrameReader = await FrameReaderLoader.GetFrameReaderAsync(mediaCapture, MediaFrameSourceKind.Color);
		FrameReader.FrameArrived += FrameArrived;

		var status = await FrameReader.StartAsync();

		if (status == MediaFrameReaderStartStatus.Success) {
			Logger.LogTrace($"Started MediaFrameReader.");
		}
		else {
			Logger.LogError($"Unable to start MediaFrameReader. Error: {status}");
		}
	}

	public override SoftwareBitmap? ConvertFrame(VideoMediaFrame frame) {
		try {
			// XAML requires Bgra8 with premultiplied alpha. The frame was sending BitmapAlphaMode.Straight
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
		Logger.LogTrace($"Disposed {nameof(ColorCameraProcessor)}.");
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

		FrameRateHandler.Increment(frameStopWatch.ElapsedMilliseconds);

		AcquiringFrame = false;
	}
}
