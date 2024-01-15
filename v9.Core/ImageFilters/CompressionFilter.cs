using System.Runtime.InteropServices.WindowsRuntime;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

public class CompressionFilter : ImageFilterBase, IImageFilter {
	const int RATIO = 3;

	int[] _CompressedTargetLocations = new int[PIXELS];
	byte[]? _ScaledSourcePixelHorizontalCount;
	byte[]? _ScaledSourcePixelVerticalCount;
	byte[]? _ScaledSourceSubpixelsPerRow;
	byte[]? _ScaledSourcePixelsTotal;
	int _ScaledWidth = WIDTH;
	int _ScaledStride = WIDTH * CHUNK;
	int _ScaledHeight = HEIGHT;
	int _ScaledPixels = PIXELS;
	int[] _BufferData = new int[3];
	int _NewRowBuffer = 0;

	int _InputSubpixel = 0;
	int _OutputSubpixel = 0;
	int _ScaledX = 0;
	int _ScaledY = 0;
	int _SourceX = 0;
	int _SourceY = 0;

	public void Initialize() {
		InitializeCompressionValues();
	}

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		Array.Clear(_OutputData);

		_InputSubpixel = 0;
		_OutputSubpixel = 0;

		fixed (byte* _ScaledSourcePixelHorizontalCountPtr = _ScaledSourcePixelHorizontalCount)
		fixed (byte* _ScaledSourcePixelVerticalCountPtr = _ScaledSourcePixelVerticalCount)
		fixed (byte* _ScaledSourceSubpixelsPerRowPtr = _ScaledSourceSubpixelsPerRow)
		fixed (byte* _ScaledSourcePixelsTotalPtr = _ScaledSourcePixelsTotal)
		fixed (byte* _InputDataPtr = _InputData)
		fixed (byte* _OutputDataPtr = _OutputData) {
			byte* scaledSourcePixelHorizontalCount = _ScaledSourcePixelHorizontalCountPtr;
			byte* scaledSourcePixelVerticalCount = _ScaledSourcePixelVerticalCountPtr;
			byte* scaledSourceSubpixelsPerRow = _ScaledSourceSubpixelsPerRowPtr;
			byte* scaledSourcePixelsTotal = _ScaledSourcePixelsTotalPtr;
			byte* inputData = _InputDataPtr;
			byte* outputData = _OutputDataPtr;

			for (_ScaledY = 0; _ScaledY < _ScaledHeight; _ScaledY++) {
				for (_ScaledX = 0; _ScaledX < _ScaledWidth; _ScaledX++) {
					Array.Clear(_BufferData);

					for (_SourceY = 0; _SourceY < *(scaledSourcePixelVerticalCount); _SourceY++) {
						for (_SourceX = 0; _SourceX < *(scaledSourcePixelHorizontalCount); _SourceX++) {
							_BufferData[0] += _InputData[_InputSubpixel];
							_BufferData[1] += _InputData[_InputSubpixel + 1];
							_BufferData[2] += _InputData[_InputSubpixel + 2];

							// goto next pixel
							_InputSubpixel += CHUNK;
						}

						// return to first pixel
						_InputSubpixel -= *(scaledSourcePixelHorizontalCount) * CHUNK;

						// goto next row
						_InputSubpixel += STRIDE;
					}

					_OutputData[_OutputSubpixel] = Convert.ToByte(_BufferData[0] / *(scaledSourcePixelsTotal));
					_OutputData[_OutputSubpixel + 1] = Convert.ToByte(_BufferData[1] / *(scaledSourcePixelsTotal));
					_OutputData[_OutputSubpixel + 2] = Convert.ToByte(_BufferData[2] / *(scaledSourcePixelsTotal));

					_OutputSubpixel += CHUNK;
					scaledSourcePixelsTotal++;

					// return to first row
					_InputSubpixel -= *(scaledSourcePixelVerticalCount) * STRIDE;

					// goto next horizontal set
					_InputSubpixel += *(scaledSourcePixelHorizontalCount) * CHUNK;

					scaledSourcePixelHorizontalCount++;
				}

				// return to first horizontal set
				_InputSubpixel -= STRIDE;

				// goto next vertical set
				_InputSubpixel += *(scaledSourcePixelVerticalCount) * STRIDE;

				// add buffer to right of compressed image
				_OutputSubpixel += _NewRowBuffer;

				scaledSourcePixelVerticalCount++;
			}
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}

	void InitializeCompressionValues() {
		_ScaledWidth = Convert.ToInt32(Math.Ceiling(1f * WIDTH / RATIO));
		_ScaledHeight = Convert.ToInt32(Math.Ceiling(1f * HEIGHT / RATIO));
		_ScaledPixels = _ScaledWidth * _ScaledHeight;
		_ScaledSourcePixelHorizontalCount = new byte[_ScaledPixels];
		_ScaledSourcePixelVerticalCount = new byte[_ScaledPixels];
		_ScaledSourceSubpixelsPerRow = new byte[_ScaledPixels];
		_ScaledSourcePixelsTotal = new byte[_ScaledPixels];
		_NewRowBuffer = (WIDTH - _ScaledWidth) * CHUNK;

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

				// The compressed edges could have a smaller number of source pixels
				if (x == _ScaledWidth - 1 && WIDTH % RATIO != 0) {
					scaledSourcePixelHorizontalCount = WIDTH % RATIO;
				}

				if (y == _ScaledHeight - 1 && HEIGHT % RATIO != 0) {
					scaledSourcePixelVerticalCount = HEIGHT % RATIO;
				}

				_ScaledSourcePixelHorizontalCount[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount);
				_ScaledSourcePixelVerticalCount[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelVerticalCount);
				_ScaledSourceSubpixelsPerRow[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount * CHUNK);
				_ScaledSourcePixelsTotal[scaledTargetPixel] = Convert.ToByte(scaledSourcePixelHorizontalCount * scaledSourcePixelVerticalCount);
			}
		}
	}
}
