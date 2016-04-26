using System;

namespace KIP2.Models.ImageProcessors {
	public class BrightnessFocusProcessor : ImageProcessorBase {
		int _focusAreaSize;
		int _focusByteCount;
		int _focusAreaCenter;

		int[] _focusOffsets;
		byte[] _focusArea;

		int _sampleAreaGap;
		int _sampleAreaSize;
		int _sampleByteCount;

		int[] _sampleOffsets;

		public BrightnessFocusProcessor() : base() {
			_focusAreaSize = 99 * 99;
			_focusByteCount = _focusAreaSize * 4;

			_sampleAreaSize = 11 * 11;
			_sampleAreaGap = 10;
			_sampleByteCount = _sampleAreaSize * 4;

			_focusArea = new byte[_focusByteCount];
			_focusOffsets = new int[_focusAreaSize];
			_sampleOffsets = new int[_sampleAreaSize];

			_focusOffsets = SquareOffsets(_focusAreaSize, _imageMaxX);
			_sampleOffsets = SquareOffsets(_sampleAreaSize, _imageMaxX);
		}

		public override byte[] ProcessImage() {
			DetectBrightness();
			LoadFocusArea();
			BuildOutput();
			OverlaySamplingInfo();

			return _outputArray;
		}

		void DetectBrightness() {
			var brightestPixelValue = 0;
			var brightestPixelDistance = _imageMidX + _imageMidY;

			var maxDistanceFromCenter = 0;

			for (int y = 0; y < _imageMaxY; y += _sampleAreaGap) {
				var yOffset = y * _imageMaxX;

				for (int x = 0; x < _imageMaxX; x += _sampleAreaGap) {
					var pixel = (yOffset + x) * 4;
					var brightness = 0;

					foreach (var sampleOffset in _sampleOffsets) {
						if (pixel + sampleOffset > 0 && pixel + sampleOffset < _byteCount) {
							brightness += ColorSensorData[pixel + sampleOffset] + ColorSensorData[pixel + sampleOffset + 1] + ColorSensorData[pixel + sampleOffset + 2];
						}
					}

					if (brightness >= brightestPixelValue) {
						// speed cheat - not true hypoteneuse!
						var distanceFromCenter = Math.Abs(x - _imageMidX) + Math.Abs(y - _imageMidY);

						maxDistanceFromCenter = distanceFromCenter;

						if (distanceFromCenter <= brightestPixelDistance) {
							brightestPixelDistance = distanceFromCenter;
							brightestPixelValue = brightness;
							_focusAreaCenter = pixel;
						}
					}
				}
			}
		 }

		void LoadFocusArea() {
			var byteCount = 0;

			foreach (var offset in _focusOffsets) {
				var effectiveOffset = offset + _focusAreaCenter;

				if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
					_focusArea[byteCount] = ColorSensorData[effectiveOffset];
					_focusArea[byteCount + 1] = ColorSensorData[effectiveOffset + 1];
					_focusArea[byteCount + 2] = ColorSensorData[effectiveOffset + 2];
				}

				byteCount += 4;
			}
		}

		void BuildOutput() {
			//var byteCount = 0;

			//for (var i = 0; i < _byteCount; i += 4) {
			//	_outputArray[i] = 0;
			//	_outputArray[i + 1] = 0;
			//	_outputArray[i + 2] = 0;
			//}

			//foreach (var offset in _focusOffsets) {
			//	var effectiveOffset = offset + _focusAreaCenter;

			//	if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
			//		_outputArray[effectiveOffset] = _focusArea[byteCount];
			//		_outputArray[effectiveOffset + 1] = _focusArea[byteCount + 1];
			//		_outputArray[effectiveOffset + 2] = _focusArea[byteCount + 2];
			//	}

			//	byteCount += 4;
			//}

			Buffer.BlockCopy(ColorSensorData, 0, _outputArray, 0, ColorSensorData.Length);
		}

		void OverlaySamplingInfo() {
			// Add blue pixels for sampling grid
			for (int y = 0; y < _imageMaxY; y += _sampleAreaGap) {
				var yOffset = y * _imageMaxX;

				for (int x = 0; x < _imageMaxX; x += _sampleAreaGap) {
					var pixel = (yOffset + x) * 4;

					_outputArray[pixel + 0] = 255;
					_outputArray[pixel + 1] = 0;
					_outputArray[pixel + 2] = 0;
				}
			}

			// Add red spot to highlight focal point
			foreach (var sampleOffset in _sampleOffsets) {
				if (_focusAreaCenter + sampleOffset > 0 && _focusAreaCenter + sampleOffset < _byteCount) {
					_outputArray[_focusAreaCenter + sampleOffset] = 0;
					_outputArray[_focusAreaCenter + sampleOffset + 1] = 0;
					_outputArray[_focusAreaCenter + sampleOffset + 2] = 255;
				}
			}
		}
	}
}