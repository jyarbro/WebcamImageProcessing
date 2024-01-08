using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.ImageFilters;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;

namespace v9.Core.ImageProcessors;

public class BoostGreenProcessor : ColorCameraProcessor {
	byte[] internalImageData = new byte[PIXELS];

	GreenBooster GreenBooster => new();

	public BoostGreenProcessor(
		ILogger logger,
		IFrameRateHandler frameRateHandler,
		DispatcherQueue dispatcherQueue
	) : base(
		logger,
		frameRateHandler,
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
