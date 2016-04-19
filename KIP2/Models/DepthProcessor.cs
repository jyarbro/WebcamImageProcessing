using Microsoft.Kinect;

namespace KIP2.Models {
	public class DepthProcessor {
		protected int _imageMaxX = 640;
		protected int _imageMaxY = 480;
		protected int _imageMidX = 320;
		protected int _imageMidY = 240;

		protected int _pixelCount;

		protected short[] _outputArray;

		public DepthProcessor() {
			_pixelCount = _imageMaxX * _imageMaxY;

			_outputArray = new short[_pixelCount];
		}

		public short[] ProcessImage(DepthImagePixel[] inputArray) {
			for (var i = 0; i < _pixelCount; i++)
				_outputArray[i] = inputArray[i].Depth;

			return _outputArray;
		}
	}
}
