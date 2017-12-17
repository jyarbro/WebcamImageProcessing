using KIP.Structs;
using KIP5.Services;
using System.Collections.Generic;
using System.Linq;

namespace KIP5.ImageProcessors {
	unsafe class EdgeFilter : ImageProcessor {
		const int THRESHOLD = 128;

		int[] Weights;
		int[] Offsets;
		
		int _i;
		int _j;
		Pixel _pixel;
		int _pixelValue;

		public EdgeFilter(SensorReader sensorReader) : base(sensorReader) {
			CalculateEdgeFilterOffsetsAndWeights();
		}

		protected override void ApplyFilters(Pixel[] sensorData) {
			fixed (byte* outputData = OutputData) {
				var outputBytePtr = outputData;
				_i = 0;

				while (_i++ < PixelCount) {
					var totalEffectiveValue = 0;

					for (var weightsIndex = 0; weightsIndex < Offsets.Length; weightsIndex++) {
						_j = _i + Offsets[weightsIndex];

						if (_j < 0 || _j >= PixelCount)
							continue;

						_pixel = sensorData[_j];
						_pixelValue = (_pixel.B + _pixel.G + _pixel.R) * Weights[weightsIndex];

						totalEffectiveValue += _pixelValue;
					}

					if (totalEffectiveValue > THRESHOLD) {
						*(outputBytePtr) = 0;
						*(outputBytePtr + 1) = 0;
						*(outputBytePtr + 2) = 0;
					}
					else {
						*(outputBytePtr) = 255;
						*(outputBytePtr + 1) = 255;
						*(outputBytePtr + 2) = 255;
					}

					outputBytePtr += 4;
				}
			}
		}

		void CalculateEdgeFilterOffsetsAndWeights() {
			var edgeFilterWeights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			var areaBox = new Rectangle {
				Origin = new Point { X = -1, Y = -1 },
				Extent = new Point { X = 1, Y = 1 },
			};

			var edgeFilterOffsets = CalculateOffsets(areaBox, edgeFilterWeights.Count, FrameWidth);

			var filteredPixelCount = edgeFilterWeights.Where(f => f != 0).Count();

			Offsets = new int[filteredPixelCount];
			Weights = new int[filteredPixelCount];

			var j = 0;

			for (var i = 0; i < edgeFilterWeights.Count; i++) {
				if (edgeFilterWeights[i] == 0)
					continue;

				Weights[j] = edgeFilterWeights[i];
				Offsets[j] = edgeFilterOffsets[i] * 4;

				j++;
			}
		}
	}
}