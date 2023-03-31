using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Dispatching;
using v8.Core.ImageFilters;
using v8.Core.Services.Logger;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using v8.Core.Helpers;
using Windows.Media.Capture;
using v8.Core.Contracts.Services;

namespace v8.Core.ImageProcessors;

public class BoostGreenProcessor : ColorCameraProcessor {
	byte[] internalImageData = new byte[PIXELS];

	GreenBooster GreenBooster => new();

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
			bitmap.CopyToBuffer(internalImageData.AsBuffer());
			
			GreenBooster.BoostGreen(internalImageData, HEIGHT, WIDTH);
			
			bitmap.CopyFromBuffer(internalImageData.AsBuffer());
			return bitmap;
		}
		catch (ObjectDisposedException) { }

		return null;
	}
}
