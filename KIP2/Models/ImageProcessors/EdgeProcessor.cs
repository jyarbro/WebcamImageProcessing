using System;

namespace KIP2.Models.ImageProcessors {
	public class EdgeProcessor : ImageProcessor {
		protected int _pixelEdgeThreshold;

		protected int[] _edgeFilterWeights;
		protected int[] _edgeFilterOffsets;

		int[] _tempIntArray1;
		int[] _tempIntArray2;

		public EdgeProcessor() : base() {
			_tempIntArray1 = new int[_pixelCount];
			_tempIntArray2 = new int[_pixelCount];

			_edgeFilterOffsets = GetOffsetsForSquare(3);

			_edgeFilterWeights = new int[] {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			_pixelEdgeThreshold = (255 * 255 * 3) / 4;
		}

		public override byte[] ProcessImage(byte[] inputArray) {
			Buffer.BlockCopy(inputArray, 0, _inputArray, 0, _byteCount);

			AggregatePixelValues();
			FilterEdges();
			ExpandPixelValues();

			return _outputArray;
		}

		void AggregatePixelValues() {
			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < _byteCount; byteOffset += 4) {
				_tempIntArray1[pixelOffset] =
					(_inputArray[byteOffset] * _inputArray[byteOffset]) +
					(_inputArray[byteOffset + 1] * _inputArray[byteOffset + 1]) +
					(_inputArray[byteOffset + 2] * _inputArray[byteOffset + 2]);
				pixelOffset++;
			}
		}

		void FilterEdges() {
			var filterLength = _edgeFilterWeights.Length;

			for (var pixel = 0; pixel < _pixelCount; pixel++) {
				var aggregate = 0;

				for (var filterOffset = 0; filterOffset < filterLength; filterOffset++) {
					var offset = _edgeFilterOffsets[filterOffset] + pixel;

					if (offset > 0 && offset < _pixelCount)
						aggregate += _tempIntArray1[offset] * _edgeFilterWeights[filterOffset];
				}

				if (aggregate >= _pixelEdgeThreshold)
					_tempIntArray2[pixel] = 1;
				else
					_tempIntArray2[pixel] = 0;
			}
		}

		void ExpandPixelValues() {
			var byteCount = 0;

			for (var pixelCount = 0; pixelCount < _pixelCount; pixelCount++) {
				if (_tempIntArray2[pixelCount] == 1) {
					_outputArray[byteCount] = 0;
					_outputArray[byteCount + 1] = 0;
					_outputArray[byteCount + 2] = 0;
				}
				else {
					_outputArray[byteCount] = _inputArray[byteCount];
					_outputArray[byteCount + 1] = _inputArray[byteCount + 1];
					_outputArray[byteCount + 2] = _inputArray[byteCount + 2];
				}

				byteCount += 4;
			}
		}
	}
}
