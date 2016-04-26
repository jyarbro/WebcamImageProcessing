using System;
using Microsoft.Kinect;

namespace KIP2.Models.ImageProcessors {
	public class DepthFocusProcessor : ImageProcessorBase {
		int _focusAreaCenter;

		int _sampleAreaGap;
		int _sampleAreaHorizontalCount;
		int _sampleAreaVerticalCount;
		int _sampleAreaTotalCount;

		int[] _sampleOffsets;

		public DepthFocusProcessor() : base() {
			_sampleAreaGap = 5;

			_sampleAreaHorizontalCount = _imageMaxX / _sampleAreaGap;
			_sampleAreaVerticalCount = _imageMaxY / _sampleAreaGap;
			_sampleAreaTotalCount = (_imageMaxX * _imageMaxY) / _sampleAreaGap;

			_sampleOffsets = SquareOffsets(11 * 11, _imageMaxX, false);
		}

		public override byte[] ProcessImage(byte[] inputArray, short[] depthArray = null) {
			_inputArray = inputArray;
			_depthArray = depthArray;

			if (_depthArray == null)
				return _inputArray;

			DetectClosestObject();
			BuildOutput();
			OverlaySamplingInfo();

			return _outputArray;
		}

		void DetectClosestObject() {
			var closestPixelValue = 0;
			var closestPixelDistance = _imageMidX + _imageMidY;

			var maxDistanceFromCenter = 0;

			for (int y = 0; y < _imageMaxY; y += _sampleAreaGap) {
				var yOffset = y * _imageMaxX;

				for (int x = 0; x < _imageMaxX; x += _sampleAreaGap) {
					var pixel = yOffset + x;
					var depth = 0;

					foreach (var sampleOffset in _sampleOffsets) {
						if (pixel + sampleOffset > 0 && pixel + sampleOffset < _pixelCount) {
							var depthOffset = pixel + sampleOffset;
							depth += _depthArray[depthOffset];
						}
					}

					if (depth <= closestPixelValue) {
						// speed cheat - not true hypoteneuse!
						var distanceFromCenter = Math.Abs(x - _imageMidX) + Math.Abs(y - _imageMidY);

						maxDistanceFromCenter = distanceFromCenter;

						if (distanceFromCenter <= closestPixelDistance) {
							closestPixelDistance = distanceFromCenter;
							closestPixelValue = depth;
							_focusAreaCenter = pixel;
						}
					}
				}
			}

			_focusAreaCenter = _focusAreaCenter * 4;
		}

		void BuildOutput() {
			Buffer.BlockCopy(_inputArray, 0, _outputArray, 0, _inputArray.Length);
		}

		void OverlaySamplingInfo() {
			// Add blue pixels for sampling grid
			//for (int y = 0; y < _imageMaxY; y += _sampleAreaGap) {
			//	var yOffset = y * _imageMaxX;

			//	for (int x = 0; x < _imageMaxX; x += _sampleAreaGap) {
			//		var pixel = (yOffset + x) * 4;

			//		_outputArray[pixel + 0] = 255;
			//		_outputArray[pixel + 1] = 0;
			//		_outputArray[pixel + 2] = 0;
			//	}
			//}

			// Add green spot to highlight focal point
			foreach (var sampleOffset in _sampleOffsets) {
				var sampleByteOffset = sampleOffset * 4;

				if (_focusAreaCenter + sampleByteOffset > 0 && _focusAreaCenter + sampleByteOffset < _byteCount) {
					_outputArray[_focusAreaCenter + sampleByteOffset] = 0;
					_outputArray[_focusAreaCenter + sampleByteOffset + 1] = 255;
					_outputArray[_focusAreaCenter + sampleByteOffset + 2] = 0;
				}
			}
		}
	}
}