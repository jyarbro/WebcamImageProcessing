using Microsoft.Kinect;

namespace KIP2.Models.DepthProcessors {
	public abstract class DepthProcessorBase {
		protected int _imageMaxX = 640;
		protected int _imageMaxY = 480;
		protected int _imageMidX = 320;
		protected int _imageMidY = 240;

		protected int _pixelCount;
		protected int _byteCount;

		protected byte[] _inputArray;
		protected byte[] _outputArray;

		public DepthProcessorBase() {
			_pixelCount = _imageMaxX * _imageMaxY;
			_byteCount = _pixelCount * 4;

			_inputArray = new byte[_byteCount];
			_outputArray = new byte[_byteCount];
		}

		public abstract byte[] ProcessImage(DepthImagePixel[] inputArray);
	}
}
