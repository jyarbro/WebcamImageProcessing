using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.Helpers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v9.Core.ImageProcessors;

public abstract class ImageProcessor(
		ILogger<ImageProcessor> logger,
		IFrameRateHandler frameRateHandler
	) : IAsyncDisposable {

	protected const int CHUNK = 4;
	protected const int WIDTH = 640;
	protected const int HEIGHT = 480;
	protected const int STRIDE = WIDTH * CHUNK;
	protected const int PIXELS = WIDTH * HEIGHT * CHUNK;

	public SoftwareBitmapSource ImageSource = new();

	public DispatcherQueue? DispatcherQueue { protected get; set; }

	protected ILogger Logger { get; init; } = logger;
	protected IFrameRateHandler FrameRateHandler { get; init; } = frameRateHandler;

	SoftwareBitmap? BackBuffer;
	bool SwappingActiveImage = false;

	public void ProcessFrame(MediaFrameReference frame) {
		if (frame is null) {
			return;
		}

		var softwareBitmap = ConvertFrame(frame.VideoMediaFrame);

		Debug.Assert(softwareBitmap is not null);

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

		DispatcherQueue?.TryEnqueue(async () => {
			SoftwareBitmap? latestBitmap;

			// Keep draining frames from the backbuffer until the backbuffer is empty.
			while ((latestBitmap = Interlocked.Exchange(ref BackBuffer, null)) is not null) {
				try {
					await ImageSource.SetBitmapAsync(latestBitmap);
				}
				catch (TaskCanceledException) { }
				catch (COMException) { }

				latestBitmap.Dispose();
			}
		});

		SwappingActiveImage = false;
	}

	public abstract Task InitializeAsync(MediaCapture mediaCapture);
	public abstract SoftwareBitmap? ConvertFrame(VideoMediaFrame videoMediaFrame);
	public abstract Task DisposeAsync();

	protected FilterOffsets PrecalculateFilterOffsets(int layer) {
		int offset(int row, int col) => (WIDTH * row + col) * CHUNK;

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
		result.Max = WIDTH * HEIGHT * CHUNK - result.BR - CHUNK;

		return result;
	}

	ValueTask IAsyncDisposable.DisposeAsync() {
		throw new NotImplementedException();
	}
}
