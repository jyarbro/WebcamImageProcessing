using System;
using System.Collections.Generic;
using System.Linq;
using KIP3.Helpers;

namespace KIP3.Infrastructure {
	public class ImageProcessor : Observable {
		#region Fields

		public string StatusText {
			get { return _StatusText ?? (_StatusText = string.Empty); }
			set { SetProperty(ref _StatusText, value); }
		}
		string _StatusText;

		public int SampleGap;
		public int FocusPartArea;
		public int FocusPartWidth;
		public int[] FocusPartOffsets;

		public int[] EdgeFilterWeights;
		public int[] EdgeFilterOffsets;

		public Point ImageMax;
		public Point ImageMid;

		public Pixel[] Pixels;
		public PixelLocation[] PixelLocations;

		public int FocusIndex;

		public int PixelCount;

		public byte[] OutputData;

		public int i;
		public int byteOffset;

		#endregion

		public void Load() {
			SampleGap = 10;

			if (ImageMid.X % SampleGap > 0)
				throw new Exception("Image width must be evently divisible by sample gap.");

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			var halfWidth = Convert.ToInt32(Math.Floor((double)FocusPartWidth / 2));

			FocusPartOffsets = PrepareOffsets(new Rectangle(-halfWidth, -halfWidth, halfWidth, halfWidth), FocusPartArea, ImageMax.X, false);

			PrepareEdgeFilterOffsetsAndWeights();
		}

		public void ProcessImage() {
			byteOffset = 0;

			for (i = 0; i < PixelCount; i++) {
				OutputData[byteOffset] = Pixels[i].B;
				OutputData[byteOffset + 1] = Pixels[i].G;
				OutputData[byteOffset + 2] = Pixels[i].R;

				byteOffset += 4;
			}
		}

		/// <summary>
		/// Calculates offsets and weights used in edge filtering.
		/// </summary>
		public void PrepareEdgeFilterOffsetsAndWeights() {
			var edgeFilterWeights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			var edgeFilterOffsets = PrepareOffsets(new Rectangle(-1, -1, 1, 1), edgeFilterWeights.Count, ImageMax.X);

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

		/// <summary>
		/// A universal method for calculating all of the linear offsets for a given square area
		/// </summary>
		public int[] PrepareOffsets(Rectangle areaBox, int area, int stride, bool byteMultiplier = true) {
			if (area % 2 == 0)
				throw new Exception("Odd sizes only!");

			var offsets = new int[area];

			var offset = 0;

			for (int yOffset = areaBox.Origin.Y; yOffset <= areaBox.Extent.Y; yOffset++) {
				for (int xOffset = areaBox.Origin.X; xOffset <= areaBox.Extent.X; xOffset++) {
					offsets[offset] = xOffset + (yOffset * stride);

					if (byteMultiplier)
						offsets[offset] = offsets[offset] * 4;

					offset++;
				}
			}

			return offsets;
		}
	}
}
