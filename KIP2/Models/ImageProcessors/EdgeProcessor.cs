using System.Collections.Generic;
using System.Linq;

namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// A general laplacian edge filter
	/// </summary>
	public class EdgeProcessor : ImageProcessorBase {
		public int PixelEdgeThreshold;

		public int[] EdgeFilterWeights;
		public int[] EdgeFilterOffsets;

		public EdgeProcessor() : base() {
			CalculateFilterValues();
			PixelEdgeThreshold = 60 * 3;
		}

		public override byte[] ProcessImage() {
			for (var i = 0; i < ByteCount; i += 4) {
				var sample = 0;

				for (var j = 0; j < EdgeFilterOffsets.Length; j++) {
					if (EdgeFilterWeights[j] == 0)
						continue;

					var offset = i + EdgeFilterOffsets[j];

					if (offset > 0 && offset < ByteCount)
						sample += EdgeFilterWeights[j] * (ColorSensorData[offset] + ColorSensorData[offset + 1] + ColorSensorData[offset + 2]);
				}

				if (sample >= PixelEdgeThreshold) {
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

			EdgeFilterOffsets = new int[filteredPixelCount];
			EdgeFilterWeights = new int[filteredPixelCount];

			var j = 0;

			for (var i = 0; i < edgeFilterWeights.Count; i++) {
				if (edgeFilterWeights[i] == 0)
					continue;

				EdgeFilterWeights[j] = edgeFilterWeights[i];
				EdgeFilterOffsets[j] = edgeFilterOffsets[i] * 4;

				j++;
			}
		}
	}
}
