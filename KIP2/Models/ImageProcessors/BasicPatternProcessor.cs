using System;
using System.Collections.Generic;

namespace KIP2.Models.ImageProcessors {
	public class BasicPatternProcessor : ImageProcessorBase {
		int _focusRegionArea;
		int _focusRegionWidth;
		int _focusRegionCenter;

		int _focusPartArea;
		int _focusPartWidth;
		int _focusPartHorizontalCount;
		int _focusPartTotalCount;

		List<int[]> _focusPartOffsets;
		List<byte[]> _focusParts;

		int _sampleGap;
		int _sampleByteCount;
		int _sampleCenterOffset;

		int[] _sampleOffsets;

		public BasicPatternProcessor() : base() {
			_sampleGap = 10;

			_focusPartWidth = 11;
			_focusRegionWidth = 99;

			if (_focusRegionWidth % _focusPartWidth > 0)
				throw new Exception("Focus area width must be divisible by sample area width");

			_focusPartArea = _focusPartWidth * _focusPartWidth; // 121
			_focusRegionArea = _focusRegionWidth * _focusRegionWidth; // 9801

			_focusPartHorizontalCount = _focusRegionWidth / _focusPartWidth; // 9
			_focusPartTotalCount = _focusPartHorizontalCount * _focusPartHorizontalCount; // 81

			_focusPartOffsets = new List<int[]>();
			_focusParts = new List<byte[]>();
		}

		public override void Load() {
			LoadSampleOffsets();
			LoadFocusPartOffsets();
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

		void LoadSampleOffsets() {
			_sampleByteCount = _focusPartArea * 4;
			_sampleOffsets = SquareOffsets(_focusPartArea, _imageMaxX);
			_sampleCenterOffset = Convert.ToInt32(Math.Floor(Math.Sqrt(_focusPartArea) / 2));
		}

		void LoadFocusPartOffsets() {
			var focusOffsets = SquareOffsets(9801, 640);

			for (int i = 0; i < 81; i++) {
				_focusParts.Add(new byte[121 * 4]);
				_focusPartOffsets.Add(new int[121]);
			}

			for (var i = 0; i < focusOffsets.Length; i++) {
				var y = Convert.ToInt32(Math.Floor((double)i / 99));
				var x = i % 99;

				var focusPartRow = Convert.ToInt32(Math.Floor((double)y / 11));
				var focusPartCol = Convert.ToInt32(Math.Floor((double)x / 11));

				var focusPartOffset = focusPartCol + (focusPartRow * 9);

				_focusPartOffsets[focusPartOffset][i % 121] = focusOffsets[i];
			}
		}
		
		void DetectFocalPoint() {
			var brightestPixelValue = 0;
			var brightestPixelDistance = _imageMidX + _imageMidY;

			var maxDistanceFromCenter = 0;

			for (int y = 0; y < _imageMaxY; y += _sampleGap) {
				var yOffset = y * _imageMaxX;

				for (int x = 0; x < _imageMaxX; x += _sampleGap) {
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
							_focusRegionCenter = pixel;
						}
					}
				}
			}
		 }

		void LoadFocusArea() {
			for (int i = 0; i < _focusPartOffsets.Count; i++) {
				var subAreaOffsets = _focusPartOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + _focusRegionCenter;

					if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
						_focusParts[i][byteCount] = _inputArray[effectiveOffset];
						_focusParts[i][byteCount + 1] = _inputArray[effectiveOffset + 1];
						_focusParts[i][byteCount + 2] = _inputArray[effectiveOffset + 2];
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

			for (int i = 0; i < _focusPartOffsets.Count; i++) {
				var subAreaOffsets = _focusPartOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + _focusRegionCenter;

					if (effectiveOffset > 0 && effectiveOffset < _byteCount) {
						_outputArray[effectiveOffset] = _focusParts[i][byteCount];
						_outputArray[effectiveOffset + 1] = _focusParts[i][byteCount + 1];
						_outputArray[effectiveOffset + 2] = _focusParts[i][byteCount + 2];
					}

					byteCount += 4;
				}
			}
		}

		void OverlaySamplingInfo() {
			// Add blue pixels for sampling grid
			for (int y = 0; y < _imageMaxY; y += _sampleGap) {
				var yOffset = y * _imageMaxX;

				for (int x = 0; x < _imageMaxX; x += _sampleGap) {
					var pixel = (yOffset + x) * 4;

					_outputArray[pixel + 0] = 255;
					_outputArray[pixel + 1] = 0;
					_outputArray[pixel + 2] = 0;
				}
			}

			// Add red spot to highlight focal point
			foreach (var sampleOffset in _sampleOffsets) {
				if (_focusRegionCenter + sampleOffset > 0 && _focusRegionCenter + sampleOffset < _byteCount) {
					_outputArray[_focusRegionCenter + sampleOffset] = 0;
					_outputArray[_focusRegionCenter + sampleOffset + 1] = 0;
					_outputArray[_focusRegionCenter + sampleOffset + 2] = 255;
				}
			}
		}
	}
}