namespace v8.Core.ImageFilters;

public class GreenBooster {
	const int CHUNK = 4;

	byte b, g, r;

	public unsafe byte[] BoostGreen(byte[] sourceData, int height, int width) {
		var stride = width * CHUNK;

		for (uint row = 0; row < height; row++) {
			for (uint col = 0; col < width; col++) {
				// Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
				var currPixel = 0 + stride * row + CHUNK * col;

				// Read the current pixel information into b,g,r channels (leave out alpha channel)
				b = sourceData[currPixel + 0]; // Blue
				g = sourceData[currPixel + 1]; // Green
				r = sourceData[currPixel + 2]; // Red

				// Boost the green channel, leave the other two untouched
				sourceData[currPixel + 0] = b;
				sourceData[currPixel + 1] = (byte) Math.Min(g + 80, 255);
				sourceData[currPixel + 2] = r;
			}
		}

		return sourceData;
	}
}
