using System;

namespace KIP2.Models.ImageProcessors {
	public class BrokenProcessor : ImageProcessor {
		protected int _pixelEdgeThreshold;

		protected int[] _edgeFilterWeights;
		protected int[] _edgeFilterOffsets;

		int[] _tempIntArray1;
		int[] _tempIntArray2;

		public BrokenProcessor() : base() {
			_tempIntArray1 = new int[_pixelCount];
			_tempIntArray2 = new int[_pixelCount];

			BuildFilters();
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
					_tempIntArray2[pixel] = _pixelValueMax;
				else
					_tempIntArray2[pixel] = 0;
			}

			Buffer.BlockCopy(_tempIntArray2, 0, _tempIntArray1, 0, _pixelCount);
		}

		void ExpandPixelValues() {
			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < _byteCount; byteOffset += 4) {
				if (_tempIntArray1[pixelOffset] > _pixelEdgeThreshold) {
					_outputArray[byteOffset] = 0;
					_outputArray[byteOffset + 1] = 0;
					_outputArray[byteOffset + 2] = 0;
				}
				else {
					_outputArray[byteOffset] = _inputArray[byteOffset];
					_outputArray[byteOffset + 1] = _inputArray[byteOffset + 1];
					_outputArray[byteOffset + 2] = _inputArray[byteOffset + 2];
				}

				pixelOffset++;
			}
		}

		void BuildFilters() {
			var edgeFilter = new int[,] {
				{  0, -1,  0, -1,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0,  0, 12,  0,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0, -1,  0, -1,  0 }
			};

			_pixelEdgeThreshold = _pixelValueMax / 4;

			var filterLength = edgeFilter.GetLength(0);
			var filterOffset = Convert.ToInt32(Math.Floor((double)filterLength / 2));
			var filterEnd = filterLength - filterOffset;

			_edgeFilterWeights = new int[filterLength * filterLength];
			_edgeFilterOffsets = new int[filterLength * filterLength];

			var filterOffsetCount = 0;

			for (int filterY = -filterOffset; filterY < filterEnd; filterY++) {
				for (int filterX = -filterOffset; filterX < filterEnd; filterX++) {
					if (edgeFilter[filterY + filterOffset, filterX + filterOffset] > 0) {
						_edgeFilterWeights[filterOffsetCount] = edgeFilter[filterY + filterOffset, filterX + filterOffset];
						_edgeFilterOffsets[filterOffsetCount] = (640 * filterY) + filterX;
						filterOffsetCount++;
					}
				}
			}
		}
	}
}
