using KIP.Structs;
using KIP5.Services;
using System.Collections.Generic;

namespace KIP5.ImageProcessors {
	unsafe class SobelEdgeFilter : ImageProcessor {
		const int FILTER_THRESHOLD = 40 * 3; // 3 Pixel properties

		int[] HorizontalWeights;
		int[] HorizontalOffsets;

		int[] VerticalWeights;
		int[] VerticalOffsets;

		int _i;
		int _offset;
		Pixel _pixel;

		public SobelEdgeFilter(SensorReader sensorReader) : base(sensorReader) {
			CalculateOffsetsAndWeights();
		}

		protected override void ApplyFilters(Pixel[] sensorData) {
			fixed (byte* outputPtr = Output) {
				var outputBytePtr = outputPtr;
				_i = 0;

				while (_i++ < PixelCount) {
					*(outputBytePtr) = 255;
					*(outputBytePtr + 1) = 255;
					*(outputBytePtr + 2) = 255;

					FilterDimension(outputBytePtr, VerticalOffsets, VerticalWeights, sensorData);
					FilterDimension(outputBytePtr, HorizontalOffsets, HorizontalWeights, sensorData);

					outputBytePtr += CHUNK_SIZE;
				}
			}
		}

		void FilterDimension(byte* outputBytePtr, int[] offsets, int[] weights, Pixel[] sensorData) {
			var totalEffectiveValue = 0;

			for (var filterIndex = 0; filterIndex < offsets.Length; filterIndex++) {
				_offset = _i + offsets[filterIndex];

				if (_offset >= 0 && _offset < PixelCount) {
					_pixel = sensorData[_offset];
					totalEffectiveValue += (_pixel.B + _pixel.G + _pixel.R) * weights[filterIndex];
				}
			}

			if (totalEffectiveValue > FILTER_THRESHOLD) {
				*(outputBytePtr) -= 128;
				*(outputBytePtr + 1) -= 128;
				*(outputBytePtr + 2) -= 128;
			}
		}

		void CalculateOffsetsAndWeights() {
			var verticalWeights = new List<int> {
				-1, 0, 1,
				-2, 0, 2,
				-1, 0, 1,
			};

			var horizontalWeights = new List<int> {
				 1,  2,  1,
				 0,  0,  0,
				-1, -2, -1,
			};

			var areaBox = new Rectangle {
				Origin = new Point { X = -1, Y = -1 },
				Extent = new Point { X = 1, Y = 1 },
			};

			var totalWeightCount = 9;
			var nonzeroWeightCount = 6;

			var offsets = CalculateOffsets(areaBox, totalWeightCount, FrameWidth);

			VerticalOffsets = new int[nonzeroWeightCount];
			VerticalWeights = new int[nonzeroWeightCount];
			HorizontalOffsets = new int[nonzeroWeightCount];
			HorizontalWeights = new int[nonzeroWeightCount];

			var j = 0;

			for (var i = 0; i < totalWeightCount; i++) {
				if (verticalWeights[i] == 0)
					continue;

				VerticalWeights[j] = verticalWeights[i];
				VerticalOffsets[j] = offsets[i];

				j++;
			}

			j = 0;

			for (var i = 0; i < totalWeightCount; i++) {
				if (horizontalWeights[i] == 0)
					continue;

				HorizontalWeights[j] = horizontalWeights[i];
				HorizontalOffsets[j] = offsets[i];

				j++;
			}
		}
	}
}