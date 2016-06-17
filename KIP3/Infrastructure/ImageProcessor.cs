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

		public int FocusRegionArea;
		public int FocusRegionWidth;

		public int[] FocusRegionOffsets;

		public int FocusPartArea;
		public int FocusPartWidth;

		public int SampleGap;

		public int[] FocusPartOffsets;

		public int[] EdgeFilterWeights;
		public int[] EdgeFilterOffsets;

		public int PixelEdgeThreshold;

		public Point ImageMax;
		public Point ImageMid;

		public Rectangle Window;

		public Pixel[] Pixels;
		public PixelLocation[] PixelLocations;
		public Pixel CurrentPixel;
		public int FocusIndex;

		public int ByteCount;
		public int PixelCount;

		public byte[] OutputData;

		public int i;
		public int j;
		public int x;
		public int y;
		public int yOffset;
		public int xOffset;
		public int measuredValue;
		public int highestMeasuredValue;
		public int offset;
		public int byteOffset;
		public int pixelOffset;
		public int focalPointOffset;
		public int filterFocalPointOffset;

		public double xSq;
		public double ySq;
		public double closestPixelDistance;
		public double distanceFromCenter;

		#endregion

		public void Load() {
			ImageMax = new Point(640, 480);
			ImageMid = new Point(320, 240);

			PixelCount = ImageMax.X * ImageMax.Y;
			ByteCount = ImageMax.X * ImageMax.Y * 4;
			
			SampleGap = 10;

			if (ImageMid.X % SampleGap > 0)
				throw new Exception("Image width must be evently divisible by sample gap.");

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			var halfWidth = Convert.ToInt32(Math.Floor((double)FocusPartWidth / 2));

			FocusPartOffsets = PrepareOffsets(new Rectangle(-halfWidth, -halfWidth, halfWidth, halfWidth), FocusPartArea, ImageMax.X, false);

			FocusRegionWidth = 99;
			FocusRegionArea = FocusRegionWidth * FocusRegionWidth; // 9801

			halfWidth = Convert.ToInt32(Math.Floor((double)FocusRegionWidth / 2));

			if (FocusRegionWidth % FocusPartWidth > 0)
				throw new Exception("Focus region width must be evenly divisible by focus part width");

			FocusRegionOffsets = PrepareOffsets(new Rectangle(-halfWidth, -halfWidth, halfWidth, halfWidth), FocusRegionArea, ImageMax.X, false);

			PrepareEdgeFilterOffsetsAndWeights();
			PixelEdgeThreshold = 180;

			Window = new Rectangle(-ImageMid.X, -ImageMid.Y, ImageMid.X, ImageMid.Y);

			PreparePixels();
		}

		/// <summary>
		/// Precalculate pixel values
		/// </summary>
		public void PreparePixels() {
			Pixels = new Pixel[PixelCount];
			PixelLocations = new PixelLocation[PixelCount];

			for (i = 0; i < PixelCount; i++) {
				var y = i / ImageMax.X;
				var x = i % ImageMax.X;

				var xSq = Math.Pow(Math.Abs(x - ImageMid.X), 2);
				var ySq = Math.Pow(Math.Abs(y - ImageMid.Y), 2);
				var distance = Math.Sqrt(xSq + ySq);

				PixelLocations[i] = new PixelLocation {
					X = x,
					Y = y,
					Distance = distance,
					OffsetB = i * 4,
					OffsetG = i * 4 + 1,
					OffsetR = i * 4 + 2
				};
			}
		}

		public void ProcessImage() {
			SetFocusIndex();

			byteOffset = 0;

			for (i = 0; i < PixelCount; i++) {
				OutputData[byteOffset] = Pixels[i].B;
				OutputData[byteOffset + 1] = Pixels[i].G;
				OutputData[byteOffset + 2] = Pixels[i].R;

				byteOffset += 4;
			}
		}

		public void SetFocusIndex() {
			FocusIndex = 0;
			
			for (i = 0; i < PixelCount; i++) {
				if (Pixels[i].Depth > 0 && Pixels[i].Depth <= Pixels[FocusIndex].Depth 
					&& PixelLocations[i].Distance <= PixelLocations[FocusIndex].Distance)

					FocusIndex = i;
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
