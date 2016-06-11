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
			PixelEdgeThreshold = 180;
		}

		public override void Prepare() {
			PrepareCompressedSensorData();
		}

		public override byte[] ProcessImage() {
			return OutputUncompressed();
		}

		public byte[] OutputCompressed() {
			int sample;
			int i;
			int j;

			for (i = 0; i < PixelCount; i++) {
				sample = 0;

				for (j = 0; j < EdgeFilterOffsets.Length; j++) {
					if (EdgeFilterWeights[j] == 0)
						continue;

					var offset = i + EdgeFilterOffsets[j];

					if (offset > 0 && offset < PixelCount)
						sample += EdgeFilterWeights[j] * CompressedSensorData[offset];
				}

				var y = (i / ImageMid.X) * 2;
				var x = (i % ImageMid.X) * 2;

				var b = ((y * ImageMax.X) + x) * 4;

				if (y >= ImageMax.Y - 1)
					continue;

				if (sample >= PixelEdgeThreshold) {
					OutputArray[b] = 0;
					OutputArray[b + 1] = 0;
					OutputArray[b + 2] = 0;
					OutputArray[b + 4] = 0;
					OutputArray[b + 5] = 0;
					OutputArray[b + 6] = 0;

					b += ImageMax.X * 4;

					OutputArray[b] = 0;
					OutputArray[b + 1] = 0;
					OutputArray[b + 2] = 0;
					OutputArray[b + 4] = 0;
					OutputArray[b + 5] = 0;
					OutputArray[b + 6] = 0;
				}
				else {
					OutputArray[b] = 255;
					OutputArray[b + 1] = 255;
					OutputArray[b + 2] = 255;
					OutputArray[b + 4] = 255;
					OutputArray[b + 5] = 255;
					OutputArray[b + 6] = 255;

					b += ImageMax.X * 4;

					OutputArray[b] = 255;
					OutputArray[b + 1] = 255;
					OutputArray[b + 2] = 255;
					OutputArray[b + 4] = 255;
					OutputArray[b + 5] = 255;
					OutputArray[b + 6] = 255;
				}
			}

			return OutputArray;
		}

		public byte[] OutputUncompressed() {
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
