using System.Runtime.InteropServices.WindowsRuntime;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

public class CompressionFilter : ImageFilterBase, IImageFilter {
	const int RATIO = 3;

	int[] _CompressedTargetLocations = new int[PIXELS];
	byte[]? _ScaledSourcePixelsPerRow;
	byte[]? _ScaledSourcePixelsPerColumn;
	byte[]? _ScaledSourceSubpixelsPerRow;
	byte[]? _ScaledSourcePixelsTotal;
	int _ScaledWidth = WIDTH;
	int _ScaledStride = WIDTH * CHUNK;
	int _ScaledHeight = HEIGHT;
	int _ScaledPixels = PIXELS;
	int[] _BufferData = new int[3];

	public void Initialize() {
		CalculateCompression();
	}

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		Array.Clear(_OutputData);

		//fixed (byte* _ScaledSourcePixelsPerRowPtr = _ScaledSourcePixelsPerRow)
		//fixed (byte* _ScaledSourcePixelsPerColumnPtr = _ScaledSourcePixelsPerColumn)
		//fixed (byte* _ScaledSourceSubpixelsPerRowPtr = _ScaledSourceSubpixelsPerRow)
		//fixed (byte* _ScaledSourcePixelsTotalPtr = _ScaledSourcePixelsTotal)
		//fixed (byte* _InputDataPtr = _InputData)
		//fixed (byte* _OutputDataPtr = _OutputData) {
		//	byte* scaledSourcePixelsPerRow = _ScaledSourcePixelsPerRowPtr;
		//	byte* scaledSourcePixelsPerColumn = _ScaledSourcePixelsPerColumnPtr;
		//	byte* scaledSourceSubpixelsPerRow = _ScaledSourceSubpixelsPerRowPtr;
		//	byte* scaledSourcePixelsTotal = _ScaledSourcePixelsTotalPtr;
		//	byte* inputData = _InputDataPtr;
		//	byte* outputData = _OutputDataPtr;
		//}

		var newRowBuffer = (WIDTH - _ScaledWidth) * CHUNK;

		var inputSubpixel = 0;
		var outputSubpixel = 0;

		for (var scaledY = 0; scaledY < _ScaledHeight; scaledY++) {
			var scaledSourcePixelHorizontalCount = RATIO;
			var scaledSourcePixelVerticalCount = RATIO;

			for (var scaledX = 0; scaledX < _ScaledWidth; scaledX++) {
				// The compressed edges could have a smaller number of source pixels
				if (scaledX == _ScaledWidth - 1) {
					scaledSourcePixelHorizontalCount = WIDTH % RATIO;
				}

				if (scaledY == _ScaledHeight - 1) {
					scaledSourcePixelVerticalCount = HEIGHT % RATIO;
				}

				if (scaledSourcePixelHorizontalCount == 0) {
					scaledSourcePixelHorizontalCount = RATIO;
				}

				if (scaledSourcePixelVerticalCount == 0) {
					scaledSourcePixelVerticalCount = RATIO;
				}

				var scaledSourcePixelsTotal = scaledSourcePixelHorizontalCount * scaledSourcePixelVerticalCount;

				Array.Clear(_BufferData);

				for (var sourceY = 0; sourceY < scaledSourcePixelVerticalCount; sourceY++) {
					for (var sourceX = 0; sourceX < scaledSourcePixelHorizontalCount; sourceX++) {
						_BufferData[0] += _InputData[inputSubpixel];
						_BufferData[1] += _InputData[inputSubpixel + 1];
						_BufferData[2] += _InputData[inputSubpixel + 2];

						//Console.WriteLine($"({scaledX}, {scaledY})\t({sourceX}, {sourceY})\t{inputSubpixel}");

						// goto next pixel
						inputSubpixel += CHUNK;
					}

					// return to first pixel
					inputSubpixel -= scaledSourcePixelHorizontalCount * CHUNK;

					// goto next row
					inputSubpixel += STRIDE;
				}

				_OutputData[outputSubpixel] = Convert.ToByte(_BufferData[0] / scaledSourcePixelsTotal);
				_OutputData[outputSubpixel + 1] = Convert.ToByte(_BufferData[1] / scaledSourcePixelsTotal);
				_OutputData[outputSubpixel + 2] = Convert.ToByte(_BufferData[2] / scaledSourcePixelsTotal);

				outputSubpixel += CHUNK;

				// return to first row
				inputSubpixel -= scaledSourcePixelVerticalCount * STRIDE;

				// goto next horizontal set
				inputSubpixel += scaledSourcePixelHorizontalCount * CHUNK;
			}

			// return to first horizontal set
			inputSubpixel -= STRIDE;

			// goto next vertical set
			inputSubpixel += scaledSourcePixelVerticalCount * STRIDE;

			// add buffer to right of compressed image
			outputSubpixel += newRowBuffer;
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}

	void CalculateCompression() {
		_ScaledWidth = Convert.ToInt32(Math.Ceiling(1f * WIDTH / RATIO));
		_ScaledHeight = Convert.ToInt32(Math.Ceiling(1f * HEIGHT / RATIO));
		_ScaledPixels = _ScaledWidth * _ScaledHeight;
		_ScaledSourcePixelsPerRow = new byte[_ScaledPixels];
		_ScaledSourcePixelsPerColumn = new byte[_ScaledPixels];
		_ScaledSourceSubpixelsPerRow = new byte[_ScaledPixels];
		_ScaledSourcePixelsTotal = new byte[_ScaledPixels];

		// Determine the destination pixel for each source pixel
		for (var y = 0; y < HEIGHT; y++) {
			var rowPixels = y * WIDTH;
			var scaledRowPixels = y * _ScaledWidth;

			for (var x = 0; x < WIDTH; x++) {
				var colPixels = x;
				var scaledColPixels = Convert.ToInt32(Math.Round(1f * colPixels / RATIO));

				var currentPixel = rowPixels + colPixels;
				var currentScaledPixel = scaledRowPixels + scaledColPixels;

				_CompressedTargetLocations[currentPixel] = currentScaledPixel;
			}
		}

		// Determine how many source pixels will be compressed into each destination pixel
		for (var y = 0; y < _ScaledHeight; y++) {
			for (var x = 0; x < _ScaledWidth; x++) {
				var scaledTargetPixel = y * _ScaledWidth + x;
				var scaledSourcePixelHorizontalCount = RATIO;
				var scaledSourcePixelVerticalCount = RATIO;

				// The final pixel is likely to have a smaller number of source pixels
				if (x == _ScaledWidth - 1) {
					scaledSourcePixelHorizontalCount = WIDTH % RATIO;
				}

				if (y == _ScaledHeight - 1) {
					scaledSourcePixelVerticalCount = HEIGHT % RATIO;
				}

				_ScaledSourcePixelsPerRow[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount);
				_ScaledSourcePixelsPerColumn[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelVerticalCount);
				_ScaledSourceSubpixelsPerRow[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount * CHUNK);
				_ScaledSourcePixelsTotal[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount * scaledSourcePixelVerticalCount);
			}
		}
	}
}
