using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.Helpers;
using v9.Core.ImageFilters;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v9.Core.ImageProcessors;

public class WebcamProcessor : IAsyncDisposable {
	const int CHUNK = 4;
	const int WIDTH = 640;
	const int HEIGHT = 480;
	const int STRIDE = WIDTH * CHUNK;
	const int PIXELS = WIDTH * HEIGHT * CHUNK;

	public SoftwareBitmapSource ImageSource = new();

	protected readonly ILogger Logger;
	protected readonly IFrameRateHandler FrameRateHandler;
	protected readonly DispatcherQueue DispatcherQueue;

	MediaCapture _MediaCapture;
	MediaFrameReader _FrameReader;
	SoftwareBitmap _FilteredFrame;
	SoftwareBitmap _ConvertedFrame;
	bool _SwappingActiveImage = false;
	bool _AcquiringFrame = false;

	//TEMP
	EdgeFilter _EdgeFilter = new EdgeFilter();

	public WebcamProcessor(
		ILogger logger,
		IFrameRateHandler frameRateHandler,
		DispatcherQueue dispatcherQueue
	) {
		Logger = logger;
		FrameRateHandler = frameRateHandler;
		DispatcherQueue = dispatcherQueue;
	}

	public async Task InitializeAsync(MediaCapture mediaCapture) {
		_MediaCapture = mediaCapture;
		_FrameReader = await FrameReaderLoader.GetFrameReaderAsync(mediaCapture, MediaFrameSourceKind.Color);
		_FrameReader.FrameArrived += FrameArrived;

		var status = await _FrameReader.StartAsync();

		if (status == MediaFrameReaderStartStatus.Success) {
			Logger.LogTrace($"Started MediaFrameReader.");
		}
		else {
			Logger.LogError($"Unable to start MediaFrameReader. Error: {status}");
		}
	}

	public void ProcessFrame(MediaFrameReference frame) {
		if (frame is null) {
			return;
		}

		// XAML requires Bgra8 with premultiplied alpha. The frame was sending BitmapAlphaMode.Straight
		_ConvertedFrame = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);







		// TODO JY *** Apply filters here ***
		//TEMP
		_EdgeFilter.Apply(ref _ConvertedFrame, ref _FilteredFrame);





		DispatcherQueue.TryEnqueue(async () => {
			await ImageSource.SetBitmapAsync(_FilteredFrame);
		});
	}

	public SoftwareBitmap ConvertFrame(VideoMediaFrame frame) {
		try {
			// XAML requires Bgra8 with premultiplied alpha. The frame was sending BitmapAlphaMode.Straight
			return SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
		}
		catch (ObjectDisposedException) { }

		return null;
	}

	void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
		if (_AcquiringFrame) {
			return;
		}

		_AcquiringFrame = true;

		var frameStopWatch = Stopwatch.StartNew();

		using (var frame = sender.TryAcquireLatestFrame()) {
			ProcessFrame(frame);
		}

		frameStopWatch.Stop();

		FrameRateHandler.Increment(frameStopWatch.ElapsedMilliseconds);

		_AcquiringFrame = false;
	}

	async Task DisposeAsync() {
		_FilteredFrame?.Dispose();
		_ConvertedFrame?.Dispose();

		_FrameReader.FrameArrived -= FrameArrived;
		await _FrameReader.StopAsync();
		_FrameReader.Dispose();

		_MediaCapture.Dispose();
		
		Logger.LogTrace($"Disposed {nameof(ColorCameraProcessor)}.");
	}

	ValueTask IAsyncDisposable.DisposeAsync() {
		throw new NotImplementedException();
	}
}
