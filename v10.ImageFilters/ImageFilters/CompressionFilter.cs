using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using v10.Contracts;
using Windows.Graphics.Imaging;

namespace v10.ImageFilters.ImageFilters;

[DisplayName("3x Compressed")]
public class CompressionFilter : ImageFilterBase, IImageFilter {
	const int RATIO = 3;

	byte[] _ScaledSourcePixelHorizontalCount;
	byte[] _ScaledSourcePixelVerticalCount;
	byte[] _ScaledSourcePixelsTotal;
	int _ScaledWidth = WIDTH;
	int _ScaledHeight = HEIGHT;
	int[] _BufferData = new int[4];
	int _NewRowBuffer = 0;

	int _ScaledX = 0;
	int _ScaledY = 0;
	int _SourceX = 0;
	int _SourceY = 0;

	public CompressionFilter() {
		_ScaledWidth = Convert.ToInt32(Math.Ceiling(1f * WIDTH / RATIO));
		_ScaledHeight = Convert.ToInt32(Math.Ceiling(1f * HEIGHT / RATIO));

		var scaledPixels = _ScaledWidth * _ScaledHeight;
		_ScaledSourcePixelHorizontalCount = new byte[scaledPixels];
		_ScaledSourcePixelVerticalCount = new byte[scaledPixels];
		_ScaledSourcePixelsTotal = new byte[scaledPixels];

		_NewRowBuffer = (WIDTH - _ScaledWidth) * CHUNK;

		// Precalculate how many source pixels will be compressed into each destination pixel
		for (var y = 0; y < _ScaledHeight; y++) {
			for (var x = 0; x < _ScaledWidth; x++) {
				var scaledTargetPixel = y * _ScaledWidth + x;
				var scaledSourcePixelHorizontalCount = RATIO;
				var scaledSourcePixelVerticalCount = RATIO;

				// The compressed edges could have a smaller number of source pixels
				if (x == _ScaledWidth - 1 && WIDTH % RATIO != 0) {
					scaledSourcePixelHorizontalCount = WIDTH % RATIO;
				}

				if (y == _ScaledHeight - 1 && HEIGHT % RATIO != 0) {
					scaledSourcePixelVerticalCount = HEIGHT % RATIO;
				}

				_ScaledSourcePixelHorizontalCount[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount);
				_ScaledSourcePixelVerticalCount[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelVerticalCount);
				_ScaledSourcePixelsTotal[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount * scaledSourcePixelVerticalCount);
			}
		}
	}

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		Array.Clear(_OutputData);

		fixed (byte* _ScaledSourcePixelHorizontalCountPtr = _ScaledSourcePixelHorizontalCount)
		fixed (byte* _ScaledSourcePixelVerticalCountPtr = _ScaledSourcePixelVerticalCount)
		fixed (byte* _ScaledSourcePixelsTotalPtr = _ScaledSourcePixelsTotal)
		fixed (byte* _InputDataPtr = _InputData)
		fixed (byte* _OutputDataPtr = _OutputData) {
			byte* scaledSourcePixelHorizontalCount = _ScaledSourcePixelHorizontalCountPtr;
			byte* scaledSourcePixelVerticalCount = _ScaledSourcePixelVerticalCountPtr;
			byte* scaledSourcePixelsTotal = _ScaledSourcePixelsTotalPtr;
			byte* inputData = _InputDataPtr;
			byte* outputData = _OutputDataPtr;

			for (_ScaledY = 0; _ScaledY < _ScaledHeight; _ScaledY++) {
				for (_ScaledX = 0; _ScaledX < _ScaledWidth; _ScaledX++) {
					Array.Clear(_BufferData);

					for (_SourceY = 0; _SourceY < *scaledSourcePixelVerticalCount; _SourceY++) {
						for (_SourceX = 0; _SourceX < *scaledSourcePixelHorizontalCount; _SourceX++) {
							_BufferData[0] += *inputData;
							_BufferData[1] += *(inputData + 1);
							_BufferData[2] += *(inputData + 2);
							_BufferData[3] += *(inputData + 3);

							// goto next pixel
							inputData += CHUNK;
						}

						// return to first pixel
						inputData -= *scaledSourcePixelHorizontalCount * CHUNK;

						// goto next row
						inputData += STRIDE;
					}

					*outputData = Convert.ToByte(_BufferData[0] / *scaledSourcePixelsTotal);
					*(outputData + 1) = Convert.ToByte(_BufferData[1] / *scaledSourcePixelsTotal);
					*(outputData + 2) = Convert.ToByte(_BufferData[2] / *scaledSourcePixelsTotal);
					*(outputData + 3) = Convert.ToByte(_BufferData[3] / *scaledSourcePixelsTotal);

					outputData += CHUNK;
					scaledSourcePixelsTotal++;

					// return to first row
					inputData -= *scaledSourcePixelVerticalCount * STRIDE;

					// goto next horizontal set
					inputData += *scaledSourcePixelHorizontalCount * CHUNK;

					scaledSourcePixelHorizontalCount++;
				}

				// return to first horizontal set
				inputData -= STRIDE;

				// goto next vertical set
				inputData += *scaledSourcePixelVerticalCount * STRIDE;

				// add buffer to right of compressed image
				outputData += _NewRowBuffer;

				scaledSourcePixelVerticalCount++;
			}
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}
}
