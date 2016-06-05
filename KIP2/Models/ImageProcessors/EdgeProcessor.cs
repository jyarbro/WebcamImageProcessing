using System.Collections.Generic;
using System.Linq;

namespace KIP2.Models.ImageProcessors {
	public class EdgeProcessor : ImageProcessorBase {
		protected int _pixelEdgeThreshold;

		protected int[] _edgeFilterWeights;
		protected int[] _edgeFilterOffsets;

		public EdgeProcessor() : base() {
			CalculateFilterValues();
			_pixelEdgeThreshold = 60 * 3;
		}

		public override byte[] ProcessImage() {
			for (var i = 0; i < ByteCount; i += 4) {
				var sample = 0;

				for (var j = 0; j < _edgeFilterOffsets.Length; j++) {
					if (_edgeFilterWeights[j] == 0)
						continue;

					var offset = i + _edgeFilterOffsets[j];

					if (offset > 0 && offset < ByteCount)
						sample += _edgeFilterWeights[j] * (ColorSensorData[offset] + ColorSensorData[offset + 1] + ColorSensorData[offset + 2]);
				}

				if (sample >= _pixelEdgeThreshold) {
					OutputArray[i] = 0;
					OutputArray[i + 1] = 0;
					OutputArray[i + 2] = 0;
				}
				else {
					OutputArray[i] = 255;
					OutputArray[i + 1] = 255;
					OutputArray[i + 2] = 255;
				}
			}

			return OutputArray;
		}

		void CalculateFilterValues() {
			// try changing this to left sample, control, right sample, and control offset

			var edgeFilterWeights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			//var edgeFilterWeights = new List<int> {
			//	 0, -1,  0, -1,  0,
			//	-1,  0,  0,  0, -1,
			//	 0,  0,  8,  0,  0,
			//	-1,  0,  0,  0, -1,
			//	 0, -1,  0, -1,  0,
			//};

			var edgeFilterOffsets = PrepareSquareOffsets(edgeFilterWeights.Count, ImageMax.X);

			var filteredPixelCount = edgeFilterWeights.Where(f => f != 0).Count();

			_edgeFilterOffsets = new int[filteredPixelCount];
			_edgeFilterWeights = new int[filteredPixelCount];

			var j = 0;

			for (var i = 0; i < edgeFilterWeights.Count; i++) {
				if (edgeFilterWeights[i] == 0)
					continue;

				_edgeFilterWeights[j] = edgeFilterWeights[i];
				_edgeFilterOffsets[j] = edgeFilterOffsets[i] * 4;

				j++;
			}
		}
	}
}
