using System;
using System.Collections.Generic;
using System.Linq;

namespace KIP2.Models.ImageProcessors {
	public class EdgeProcessor : ImageProcessorBase {
		protected int _pixelEdgeThreshold;

		protected int[] _edgeFilterWeights;
		protected int[] _edgeFilterOffsets;

		int[] _tempArray;

		public EdgeProcessor() : base() {
			_tempArray = new int[_pixelCount];

			CalculateFilterValues();

			_pixelEdgeThreshold = 60 * 3;
		}

		public override byte[] ProcessImage(byte[] inputArray) {
			Buffer.BlockCopy(inputArray, 0, _outputArray, 0, _byteCount);

			for (var i = 0; i < _byteCount; i += 4) {
				var sample = 0;

				for (var j = 0; j < _edgeFilterOffsets.Length; j++) {
					if (_edgeFilterWeights[j] == 0)
						continue;

					var offset = i + (_edgeFilterOffsets[j] * 4);

					if (offset > 0 && offset < _byteCount)
						sample += _edgeFilterWeights[j] * (inputArray[offset] + inputArray[offset + 1] + inputArray[offset + 2]);
				}

				if (sample >= _pixelEdgeThreshold) {
					_outputArray[i] = 0;
					_outputArray[i + 1] = 0;
					_outputArray[i + 2] = 0;
				}
				else {
					_outputArray[i] = 255;
					_outputArray[i + 1] = 255;
					_outputArray[i + 2] = 255;
				}
			}

			return _outputArray;
		}

		void CalculateFilterValues() {
			// try changing this to left sample, control, right sample, and control offset

			//_edgeFilterWeights = new int[] {
			//	-1, -1, -1,
			//	-1,  8, -1,
			//	-1, -1, -1,
			//};

			var edgeFilterWeights = new List<int> {
				 0, -1,  0, -1,  0,
				-1,  0,  0,  0, -1,
				 0,  0,  8,  0,  0,
				-1,  0,  0,  0, -1,
				 0, -1,  0, -1,  0,
			};

			var edgeFilterOffsets = GetOffsetsForSquare(edgeFilterWeights.Count);

			var filteredPixelCount = edgeFilterWeights.Where(f => f != 0).Count();

			_edgeFilterOffsets = new int[filteredPixelCount];
			_edgeFilterWeights = new int[filteredPixelCount];

			var j = 0;

			for (var i = 0; i < edgeFilterWeights.Count; i++) {
				if (edgeFilterWeights[i] == 0)
					continue;

				_edgeFilterWeights[j] = edgeFilterWeights[i];
				_edgeFilterOffsets[j] = edgeFilterOffsets[i];

				j++;
			}
		}
	}
}
