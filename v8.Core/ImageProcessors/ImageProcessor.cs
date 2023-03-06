using Microsoft.UI.Xaml.Media.Imaging;
using v8.Core.Helpers;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.System;

namespace v8.Core.ImageProcessors;

public abstract class ImageProcessor : IAsyncDisposable {
	protected const int CHUNK = 4;

	public SoftwareBitmapSource ImageSource = new SoftwareBitmapSource();

	protected readonly ILogger Logger;
	protected readonly IFrameRateManager FrameRateManager;

	protected int OutputWidth;
	protected int OutputHeight;

	SoftwareBitmap BackBuffer;
	bool SwappingActiveImage = false;

	public ImageProcessor(
		ILogger logger,
		IFrameRateManager frameRateManager
	) {
		Logger = logger;
		FrameRateManager = frameRateManager;
	}

	public void ProcessFrame(MediaFrameReference frame) {
		if (frame is null) {
			return;
		}

		var softwareBitmap = ConvertFrame(frame.VideoMediaFrame);

		if (softwareBitmap is null) {
			return;
		}

		// Swap out the existing BackBuffer reference with the new one.
		softwareBitmap = Interlocked.Exchange(ref BackBuffer, softwareBitmap);

		// Dispose of the old BackBuffer data.
		softwareBitmap?.Dispose();

		SwapActiveImage();
	}

	public void SwapActiveImage() {
		if (SwappingActiveImage) {
			return;
		}

		SwappingActiveImage = true;

		//DispatcherQueue.TryEnqueue(() => {
			SoftwareBitmap latestBitmap;

			// Keep draining frames from the backbuffer until the backbuffer is empty.
			while ((latestBitmap = Interlocked.Exchange(ref BackBuffer, null)) != null) {
				try {
					//await ImageSource.SetBitmapAsync(latestBitmap);
				}
				catch (TaskCanceledException) { }

				latestBitmap.Dispose();
			}
		//});

		SwappingActiveImage = false;
	}

	public abstract Task InitializeAsync(MediaCapture mediaCapture);
	public abstract SoftwareBitmap ConvertFrame(VideoMediaFrame videoMediaFrame);
	public abstract Task DisposeAsync();

	protected FilterOffsets PrecalculateFilterOffsets(int layer) {
		int offset(int row, int col) => (OutputWidth * row + col) * CHUNK;

		var result = new FilterOffsets {
			TL = offset(-layer, -layer),
			TC = offset(-layer, 0),
			TR = offset(-layer, layer),
			CL = offset(0, -layer),
			CC = offset(0, 0),
			CR = offset(0, layer),
			BL = offset(layer, -layer),
			BC = offset(layer, 0),
			BR = offset(layer, layer),
		};

		result.Min = result.TL * -1;
		result.Max = OutputWidth * OutputHeight * CHUNK - result.BR - CHUNK;

		return result;
	}

	ValueTask IAsyncDisposable.DisposeAsync() {
		throw new NotImplementedException();
	}
}
