using KIP7.FrameRate;
using KIP7.Helpers;
using KIP7.Logger;
using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;

namespace KIP7.ImageProcessors {
	public class BoostGreenProcessor : ColorCameraProcessor {
		public BoostGreenProcessor(
			ILogger logger,
			IFrameRateManager frameRateManager,
			CoreDispatcher dispatcher
		) : base(
			logger,
			frameRateManager,
			dispatcher
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
			using (var buffer = bitmap.LockBuffer(BitmapBufferAccessMode.ReadWrite))
			using (var reference = buffer.CreateReference()) {
				((IMemoryBufferByteAccess) reference).GetBuffer(out var data, out var capacity);

				var description = buffer.GetPlaneDescription(0);

				for (uint row = 0; row < description.Height; row++) {
					for (uint col = 0; col < description.Width; col++) {
						// Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
						var currPixel = description.StartIndex + description.Stride * row + CHUNK * col;

						// Read the current pixel information into b,g,r channels (leave out alpha channel)
						var b = data[currPixel + 0]; // Blue
						var g = data[currPixel + 1]; // Green
						var r = data[currPixel + 2]; // Red

						// Boost the green channel, leave the other two untouched
						data[currPixel + 0] = b;
						data[currPixel + 1] = (byte) Math.Min(g + 80, 255);
						data[currPixel + 2] = r;
					}
				}
			}

			return bitmap;
		}
	}
}
