using KIP.Structs;
using KIP5.Services;
using System.Collections.Generic;
using System.Linq;

namespace KIP5.ImageProcessors {
	unsafe class LaplaceEdgeFilter : ImageProcessor {
		const int FILTER_THRESHOLD = 60 * 3;

		int[] Weights;
		int[] Offsets;
		
		int _i;
		int _j;
		Pixel _pixel;
		int _pixelValue;

		public LaplaceEdgeFilter(SensorReader sensorReader) : base(sensorReader) {
			CalculateOffsetsAndWeights();
		}

		protected override void ApplyFilters(Pixel[] sensorData) {
			fixed (byte* outputPtr = Output) {
				var outputBytePtr = outputPtr;
				_i = 0;

				while (_i++ < PixelCount) {
					var totalEffectiveValue = 0;

					for (var filterIndex = 0; filterIndex < Offsets.Length; filterIndex++) {
						_j = _i + Offsets[filterIndex];

						if (_j < 0 || _j >= PixelCount)
							continue;

						_pixel = sensorData[_j];
						_pixelValue = (_pixel.B + _pixel.G + _pixel.R) * Weights[filterIndex];

						totalEffectiveValue += _pixelValue;
					}

					if (totalEffectiveValue > FILTER_THRESHOLD) {
						*(outputBytePtr) = 0;
						*(outputBytePtr + 1) = 0;
						*(outputBytePtr + 2) = 0;
					}
					else {
						*(outputBytePtr) = 255;
						*(outputBytePtr + 1) = 255;
						*(outputBytePtr + 2) = 255;
					}

					outputBytePtr += CHUNK_SIZE;
				}
			}
		}

		void CalculateOffsetsAndWeights() {
			var weights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			var areaBox = new Rectangle {
				Origin = new Point { X = -1, Y = -1 },
				Extent = new Point { X = 1, Y = 1 },
			};
			
			var offsets = CalculateOffsets(areaBox, weights.Count, FrameStride);

			var filteredPixelCount = weights.Where(f => f != 0).Count();

			Offsets = new int[filteredPixelCount];
			Weights = new int[filteredPixelCount];

			var j = 0;

			for (var i = 0; i < weights.Count; i++) {
				if (weights[i] == 0)
					continue;

				Weights[j] = weights[i];
				Offsets[j] = offsets[i];

				j++;
			}
		}
	}
}