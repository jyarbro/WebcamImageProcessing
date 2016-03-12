using System;

namespace KIP2.Models.ImageProcessors {
	public class EdgeProcessor : ImageProcessor {
		protected int _pixelEdgeThreshold;

		protected int[] _edgeFilterWeights;
		protected int[] _edgeFilterOffsets;

		int[] _tempArray;

		public EdgeProcessor() : base() {
			_tempArray = new int[_pixelCount];

			_edgeFilterOffsets = GetOffsetsForSquare(3);

			// try changing this to left sample, control, right sample, and control offset

			_edgeFilterWeights = new int[] {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			_pixelEdgeThreshold = 255 * _edgeFilterWeights.Length;
		}

		public override byte[] ProcessImage(byte[] inputArray) {
			Buffer.BlockCopy(inputArray, 0, _outputArray, 0, inputArray.Length);

			for (var byteOffset = 0; byteOffset < _byteCount; byteOffset += 4) {
				var aggregate = 0;

				for (var filterOffset = 0; filterOffset < _edgeFilterOffsets.Length; filterOffset++) {
					var offset = byteOffset + (_edgeFilterOffsets[filterOffset] * 4);

					if (offset > 0 && offset < _byteCount) {
						var brightness =
							((inputArray[offset] * inputArray[offset]) +
							(inputArray[offset + 1] * inputArray[offset + 1]) +
							(inputArray[offset + 2] * inputArray[offset + 2])) / 3;

						aggregate += brightness * _edgeFilterWeights[filterOffset];
					}
				}

				if (aggregate >= _pixelEdgeThreshold) {
					_outputArray[byteOffset] = 0;
					_outputArray[byteOffset + 1] = 0;
					_outputArray[byteOffset + 2] = 0;
				}
			}

			return _outputArray;
		}
	}
}
