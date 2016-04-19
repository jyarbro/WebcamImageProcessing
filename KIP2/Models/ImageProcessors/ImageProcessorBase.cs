using System;

namespace KIP2.Models.ImageProcessors {
	public abstract class ImageProcessorBase {
		protected int _imageMaxX = 640;
		protected int _imageMaxY = 480;
		protected int _imageMidX = 320;
		protected int _imageMidY = 240;

		protected int _pixelCount;
		protected int _byteCount;

		protected byte[] _inputArray;
		protected byte[] _outputArray;
		protected short[] _depthArray;

		public ImageProcessorBase() {
			_pixelCount = _imageMaxX * _imageMaxY;
			_byteCount = _pixelCount * 4;

			_inputArray = new byte[_byteCount];
			_outputArray = new byte[_byteCount];
		}

		/// <summary>
		/// Optional method for overriding.
		/// </summary>
		public virtual void Load() { }

		public abstract byte[] ProcessImage(byte[] inputArray, short[] depthArray = null);

		protected static int[] SquareOffsets(int size, int stride, bool byteMultiplier = true) {
			if (size % 2 == 0)
				throw new Exception("Odd sizes only!");

			var offsets = new int[size];
			var areaMax = Convert.ToInt32(Math.Floor(Math.Sqrt(size) / 2));
			var areaMin = areaMax * -1;

			var offset = 0;

			for (int yOffset = areaMin; yOffset <= areaMax; yOffset++) {
				for (int xOffset = areaMin; xOffset <= areaMax; xOffset++) {
					offsets[offset] = xOffset + (yOffset * stride);

					if (byteMultiplier)
						offsets[offset] = offsets[offset] * 4;

					offset++;
				}
			}

			return offsets;
		}
	}
}
