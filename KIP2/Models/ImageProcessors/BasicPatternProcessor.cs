using System;
using System.Collections.Generic;

namespace KIP2.Models.ImageProcessors {
	public class BasicPatternProcessor : ImageProcessorBase {
		int _focusAreaSize;
		int _focusByteCount;
		int _focusAreaCenter;
		int _focusSubAreaWidth;
		int _sampleCenterOffset;

		List<int[]> _focusSubAreaOffsets;
		List<byte[]> _focusAreas;

		int _sampleAreaGap;
		int _sampleAreaSize;
		int _sampleByteCount;

		int[] _sampleOffsets;

		public BasicPatternProcessor() : base() {
			_sampleAreaSize = 11 * 11;
			_focusAreaSize = 99 * 99;
			_sampleAreaGap = 10;

			_focusSubAreaOffsets = new List<int[]>();
			_focusAreas = new List<byte[]>();
		}

		public override void Load() {
			LoadSampleOffsets();
			LoadFocusOffsets();
		}

		void LoadSampleOffsets() {
			_sampleByteCount = _sampleAreaSize * 4;
			_sampleOffsets = SquareOffsets(_sampleAreaSize, _imageMaxX);
			_sampleCenterOffset = Convert.ToInt32(Math.Floor(Math.Sqrt(_sampleAreaSize) / 2));
		}

		void LoadFocusOffsets() {
			_focusByteCount = _focusAreaSize * 4;

			var focusOffsets = SquareOffsets(_focusAreaSize, _imageMaxX);
			var focusSubAreaCount = (double)_focusAreaSize / _sampleAreaSize;

			_focusSubAreaWidth = Convert.ToInt32(Math.Sqrt(focusSubAreaCount));

			var focusSubAreaOffsets = SquareOffsets(_sampleAreaSize, _focusSubAreaWidth, false);

			for (int y = 0; y < _focusSubAreaWidth; y++) {
				var yEffective = ((y * _sampleAreaSize) + _sampleCenterOffset) * _focusSubAreaWidth;

				for (int x = 0; x < _focusSubAreaWidth; x++) {
					var xyTarget = (x * _sampleAreaSize) + _sampleCenterOffset + yEffective;

					_focusAreas.Add(new byte[_sampleAreaSize * 4]);

					var focusSubArea = new int[_sampleAreaSize];

					for (int i = 0; i < focusSubAreaOffsets.Length; i++)
						focusSubArea[i] = focusOffsets[xyTarget + focusSubAreaOffsets[i]];

					_focusSubAreaOffsets.Add(focusSubArea);
				}
			}
		}

		public override byte[] ProcessImage(byte[] inputArray, short[] depthArray = null) {
			_inputArray = inputArray;

			DetectFocalPoint();
			LoadFocusArea();

			// in here do some neural networking

			BuildOutput();
			OverlaySamplingInfo();

			return _outputArray;
		}

		void DetectFocalPoint() {
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
							brightness += _inputArray[pixel + sampleOffset] + _inputArray[pixel + sampleOffset + 1] + _inputArray[pixel + sampleOffset + 2];
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
			for (int i = 0; i < _focusSubAreaOffsets.Count; i++) {
				var subAreaOffsets = _focusSubAreaOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + _focusAreaCenter;

					if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
						_focusAreas[i][byteCount] = _inputArray[effectiveOffset];
						_focusAreas[i][byteCount + 1] = _inputArray[effectiveOffset + 1];
						_focusAreas[i][byteCount + 2] = _inputArray[effectiveOffset + 2];
					}

					byteCount += 4;
				}
			}
		}

		void BuildOutput() {
			for (var i = 0; i < _byteCount; i += 4) {
				_outputArray[i] = 0;
				_outputArray[i + 1] = 0;
				_outputArray[i + 2] = 0;
			}

			for (int i = 0; i < _focusSubAreaOffsets.Count; i++) {
				var subAreaOffsets = _focusSubAreaOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + _focusAreaCenter;

					if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
						_outputArray[effectiveOffset] = _focusAreas[i][byteCount];
						_outputArray[effectiveOffset + 1] = _focusAreas[i][byteCount + 1];
						_outputArray[effectiveOffset + 2] = _focusAreas[i][byteCount + 2];
					}

					byteCount += 4;
				}
			}
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