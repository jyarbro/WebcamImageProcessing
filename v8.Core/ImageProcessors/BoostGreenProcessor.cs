using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;

namespace v8.Core.ImageProcessors;

public class BoostGreenProcessor : ColorCameraProcessor {
	byte[] internalImageData = new byte[PIXELS];

	public BoostGreenProcessor(
		ILogger logger,
		IFrameRateManager frameRateManager,
		DispatcherQueue dispatcherQueue
	) : base(
		logger,
		frameRateManager,
		dispatcherQueue
	) { }

	public override SoftwareBitmap ConvertFrame(VideoMediaFrame frame) {
		try {
			var bitmap = SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
			BoostGreen(bitmap);
			return bitmap;
		}
		catch (ObjectDisposedException) { }

		return null;
	}

	public unsafe SoftwareBitmap BoostGreen(SoftwareBitmap bitmap) {
		bitmap.CopyToBuffer(internalImageData.AsBuffer());

		for (uint row = 0; row < HEIGHT; row++) {
			for (uint col = 0; col < WIDTH; col++) {
				// Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
				var currPixel = 0 + STRIDE * row + CHUNK * col;

				// Read the current pixel information into b,g,r channels (leave out alpha channel)
				var b = internalImageData[currPixel + 0]; // Blue
				var g = internalImageData[currPixel + 1]; // Green
				var r = internalImageData[currPixel + 2]; // Red

				// Boost the green channel, leave the other two untouched
				internalImageData[currPixel + 0] = b;
				internalImageData[currPixel + 1] = (byte) Math.Min(g + 80, 255);
				internalImageData[currPixel + 2] = r;
			}
		}

		bitmap.CopyFromBuffer(internalImageData.AsBuffer());

		return bitmap;
	}
}
