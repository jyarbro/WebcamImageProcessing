namespace Tests;

[TestClass]
public class ImageMath {

	[TestMethod]
	public unsafe void TestCompression() {
		const int CHUNK = 4;
		const int WIDTH = 23;
		const int HEIGHT = 45;
		const int STRIDE = WIDTH * CHUNK;
		const int PIXELS = WIDTH * HEIGHT;
		const int SUBPIXELS = PIXELS * CHUNK;
		const int RATIO = 3;

		byte[] _InputData = new byte[SUBPIXELS];
		byte[] _OutputData = new byte[SUBPIXELS];
		int[] _BufferData = new int[3];

		var _ScaledWidth = Convert.ToInt32(Math.Ceiling(1f * WIDTH / RATIO));
		var _ScaledHeight = Convert.ToInt32(Math.Ceiling(1f * HEIGHT / RATIO));
		var _ScaledPixels = _ScaledWidth * _ScaledHeight;
		var _ScaledSourcePixelsPerRow = new byte[_ScaledPixels];
		var _ScaledSourcePixelsPerColumn = new byte[_ScaledPixels];
		var _ScaledSourceSubpixelsPerRow = new byte[_ScaledPixels];
		var _ScaledSourcePixelsTotal = new byte[_ScaledPixels];

		Console.WriteLine($"STRIDE: {STRIDE}");
		Console.WriteLine($"SUBPIXELS: {SUBPIXELS}");

		var rand = new Random();

		for (int i = 0; i < SUBPIXELS; i++) {
			// The end of the chunk is the alpha channel and is always fully opaque.
			if (i % CHUNK == CHUNK - 1) {
				_InputData[i] = 255;
			}
			else {
				_InputData[i] = Convert.ToByte(rand.Next(0, 255));
			}
		}

		// Input and Output have the same size array but different size image.
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

		Console.WriteLine($"FINAL: {inputSubpixel}");

		Debug.Assert(inputSubpixel == SUBPIXELS);
	}
}