using System.Diagnostics;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;

namespace KIP7.Helpers {
	public static class FrameConverter {
		/// <summary>
		/// Function delegate that transforms a scanline from an input image to an output image. Cannot be an Action<> parameter because byte* isn't allowed.
		/// </summary>
		unsafe delegate void TransformScanline(int pixelWidth, byte* inputRowBytes, byte* outputRowBytes);

		/// <summary>
		/// Converts a frame to a SoftwareBitmap of a valid format to display in an Image control.
		/// </summary>
		/// <param name="inputFrame">Frame to convert.</param>
		public static unsafe SoftwareBitmap ConvertToDisplayableImage(VideoMediaFrame inputFrame) {
			if (inputFrame is null)
				return null;

			SoftwareBitmap result = null;

			var inputBitmap = inputFrame.SoftwareBitmap;

			if (inputBitmap is null)
				return null;

			switch (inputFrame.FrameReference.SourceKind) {
				case MediaFrameSourceKind.Color:
					// XAML requires Bgra8 with premultiplied alpha.
					if (inputBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
						return null;

					if (inputBitmap.BitmapAlphaMode == BitmapAlphaMode.Premultiplied)
						result = SoftwareBitmap.Copy(inputBitmap);
					else
						result = SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

					break;

				case MediaFrameSourceKind.Depth:
					// We requested D16 from the MediaFrameReader, so the frame should
					// be in Gray16 format.
					if (inputBitmap.BitmapPixelFormat == BitmapPixelFormat.Gray16) {
						// Use a special pseudo color to render 16 bits depth frame.
						var depthScale = (float) inputFrame.DepthMediaFrame.DepthFormat.DepthScaleInMeters;
						var minReliableDepth = inputFrame.DepthMediaFrame.MinReliableDepth;
						var maxReliableDepth = inputFrame.DepthMediaFrame.MaxReliableDepth;

						result = TransformBitmap(inputBitmap, (w, i, o) => PseudoColorHelper.PseudoColorForDepth(w, i, o, depthScale, minReliableDepth, maxReliableDepth));
					}
					break;

				case MediaFrameSourceKind.Infrared:
					// We requested L8 or L16 from the MediaFrameReader, so the frame should
					// be in Gray8 or Gray16 format. 
					switch (inputBitmap.BitmapPixelFormat) {
						case BitmapPixelFormat.Gray16:
							// Use pseudo color to render 16 bits frames.
							result = TransformBitmap(inputBitmap, PseudoColorHelper.PseudoColorFor16BitInfrared);
							break;

						case BitmapPixelFormat.Gray8:

							// Use pseudo color to render 8 bits frames.
							result = TransformBitmap(inputBitmap, PseudoColorHelper.PseudoColorFor8BitInfrared);
							break;

						default:
							Debug.WriteLine("Infrared frame in unexpected format.");
							break;
					}
					break;
			}

			inputBitmap.Dispose();

			return result;
		}

		/// <summary>
		/// Transform image into Bgra8 image
		/// </summary>
		/// <param name="softwareBitmap">Input image to transform.</param>
		/// <param name="transformScanline">Method to map pixels in a scanline.</param>
		static unsafe SoftwareBitmap TransformBitmap(SoftwareBitmap softwareBitmap, TransformScanline transformScanline) {
			// XAML Image control only supports premultiplied Bgra8 format.
			var outputBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
				softwareBitmap.PixelWidth, softwareBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);

			using (var input = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
			using (var output = outputBitmap.LockBuffer(BitmapBufferAccessMode.Write)) {
				// Get stride values to calculate buffer position for a given pixel x and y position.
				var inputStride = input.GetPlaneDescription(0).Stride;
				var outputStride = output.GetPlaneDescription(0).Stride;
				var pixelWidth = softwareBitmap.PixelWidth;
				var pixelHeight = softwareBitmap.PixelHeight;

				using (var outputReference = output.CreateReference())
				using (var inputReference = input.CreateReference()) {
					((IMemoryBufferByteAccess) inputReference).GetBuffer(out var inputBytes, out var inputCapacity);
					((IMemoryBufferByteAccess) outputReference).GetBuffer(out var outputBytes, out var outputCapacity);

					// Iterate over all pixels and store converted value.
					for (var y = 0; y < pixelHeight; y++) {
						var inputRowBytes = inputBytes + y * inputStride;
						var outputRowBytes = outputBytes + y * outputStride;

						transformScanline(pixelWidth, inputRowBytes, outputRowBytes);
					}
				}
			}

			return outputBitmap;
		}
	}
}
