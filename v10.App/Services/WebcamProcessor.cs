using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v10.Contracts;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v10.Services;

public sealed class WebcamProcessor(
		ILogger<WebcamProcessor> logger,
		IFrameRateHandler frameRateHandler
	) : IAsyncDisposable {

	public IImageFilter? ImageFilter { get; set; }

	readonly ILogger Logger = logger;
	readonly IFrameRateHandler FrameRateHandler = frameRateHandler;

	SoftwareBitmapSource _ImageSource;
	DispatcherQueue _DispatcherQueue;
	MediaCapture _MediaCapture;
	MediaFrameReader? _FrameReader;
	SoftwareBitmap _FilteredFrame;
	SoftwareBitmap _IncomingFrame;
	bool _AcquiringFrame = false;

	public async Task InitializeAsync(
		SoftwareBitmapSource imageSource,
		MediaCapture mediaCapture,
		DispatcherQueue dispatcherQueue
	) {
		_ImageSource = imageSource;
		_DispatcherQueue = dispatcherQueue;
		_MediaCapture = mediaCapture;

		_FrameReader = await FrameReaderLoader.GetFrameReaderAsync(mediaCapture, MediaFrameSourceKind.Color);
		_FrameReader.FrameArrived += FrameArrivedEvent;

		var status = await _FrameReader.StartAsync();

		if (status == MediaFrameReaderStartStatus.Success) {
			Logger.LogTrace($"Started MediaFrameReader.");
		}
		else {
			Logger.LogError($"Unable to start MediaFrameReader. Error: {status}");
		}
	}

	void FrameArrivedEvent(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
		if (_AcquiringFrame) {
			return;
		}

		_AcquiringFrame = true;

		var frameStopWatch = Stopwatch.StartNew();

		using var frame = sender.TryAcquireLatestFrame();

		if (frame is null) {
			return;
		}

		// XAML requires Bgra8 with premultiplied alpha. The frame was sending BitmapAlphaMode.Straight
		_IncomingFrame = _FilteredFrame = SoftwareBitmap.Convert(frame.VideoMediaFrame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

		ImageFilter?.Apply(ref _IncomingFrame, ref _FilteredFrame);

		_DispatcherQueue?.TryEnqueue(async () => {
			try {
				await _ImageSource?.SetBitmapAsync(_FilteredFrame);
			}
			catch (TaskCanceledException) { }
			catch (COMException) { }
		});

		frameStopWatch.Stop();

		FrameRateHandler.Increment(frameStopWatch.ElapsedMilliseconds);

		_AcquiringFrame = false;
	}

	public async ValueTask DisposeAsync() {
		if (_FrameReader is not null) {
			_FrameReader.FrameArrived -= FrameArrivedEvent;
			await _FrameReader.StopAsync();
			_FrameReader.Dispose();
		}

		_FilteredFrame?.Dispose();
		_IncomingFrame?.Dispose();
		_MediaCapture?.Dispose();

		Logger.LogTrace($"Disposed {nameof(WebcamProcessor)}.");
	}
}
