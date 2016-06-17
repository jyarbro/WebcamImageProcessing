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
		public int SampleByteCount;
		public int SampleCenterOffset;

		public int[] FocusPartOffsets;

		public byte[] ColorSensorData;
		public short[] ImageDepthData;

		public int[] EdgeFilterWeights;
		public int[] EdgeFilterOffsets;

		public int PixelEdgeThreshold;

		public Point ImageMax;
		public Point ImageMid;
		public Point FocalPoint;

		public Rectangle Window;
		public Rectangle AreaBoundBox;

		public int ByteCount;
		public int PixelCount;

		public byte[] OutputArray;

		public Func<int, int> BrightnessMeasurement;
		public Func<int, int> DepthMeasurement;

		public Func<int, int, bool> BrightnessValueComparison;
		public Func<int, int, bool> DepthValueComparison;

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
			SetFieldValues();
			SetDelegates();
		}

		public byte[] ProcessImage() {
			PrepareOutput();
			ResetOutput();

			SetMeasuredFocalPoint(Window, ImageMid, DepthMeasurement, DepthValueComparison);
			SetMeasuredFocalPoint(AreaBoundBox, FocalPoint, BrightnessMeasurement, BrightnessValueComparison);

			FilterObjectByDepth();

			return OutputArray;
		}

		/// <summary>
		/// A universal method for calculating all of the linear offsets for a given square area
		/// </summary>
		public int[] PrepareSquareOffsets(int size, int stride, bool byteMultiplier = true) {
			if (size % 2 == 0)
				throw new Exception("Odd sizes only!");

			var offsets = new int[size];
			var areaBox = GetCenteredBox(size);

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

		/// <summary>
		/// Calculates offsets and weights used in edge filtering.
		/// </summary>
		public void PrepareEdgeFilterOffsetsAndWeights() {
			var edgeFilterWeights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

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

		/// <summary>
		/// Calculate a window around a centered point given only the square's area.
		/// </summary>
		public Rectangle GetCenteredBox(int area) {
			var areaMax = Convert.ToInt32(Math.Floor(Math.Sqrt(area) / 2));
			var areaMin = areaMax * -1;

			return new Rectangle(areaMin, areaMin, areaMax, areaMax);
		}

		public void SetMeasuredFocalPoint(Rectangle window, Point target, Func<int, int> measurement, Func<int, int, bool> valueComparison) {
			xSq = Math.Pow(Math.Abs(ImageMid.X), 2);
			ySq = Math.Pow(Math.Abs(ImageMid.Y), 2);

			closestPixelDistance = Math.Sqrt(xSq + ySq);

			highestMeasuredValue = 0;
			
			for (y = target.Y + window.Origin.Y; y < target.Y + window.Extent.Y; y += SampleGap) {
				yOffset = y * ImageMax.X;

				for (x = target.X + window.Origin.X; x < target.X + window.Extent.X; x += SampleGap) {
					measuredValue = measurement(yOffset + x);

					if (valueComparison(measuredValue, highestMeasuredValue)) {
						xSq = Math.Pow(Math.Abs(x - target.X), 2);
						ySq = Math.Pow(Math.Abs(y - target.Y), 2);

						distanceFromCenter = Math.Sqrt(xSq + ySq);

						if (distanceFromCenter <= closestPixelDistance) {
							closestPixelDistance = distanceFromCenter;
							highestMeasuredValue = measuredValue;

							FocalPoint.X = x;
							FocalPoint.Y = y;
						}
					}
				}
			}
		}

		public void FilterEdges() {
			filterFocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X);

			for (i = 0; i < FocusRegionOffsets.Length; i++) {
				measuredValue = 0;

				byteOffset = (filterFocalPointOffset + FocusRegionOffsets[i]) * 4;

				if (byteOffset > 0 && byteOffset < ByteCount) {
					for (j = 0; j < EdgeFilterOffsets.Length; j++) {
						if (EdgeFilterWeights[j] == 0)
							continue;

						offset = byteOffset + EdgeFilterOffsets[j];

						if (offset > 0 && offset < ByteCount)
							measuredValue += EdgeFilterWeights[j] * (ColorSensorData[offset] + ColorSensorData[offset + 1] + ColorSensorData[offset + 2]);
					}

					if (measuredValue >= PixelEdgeThreshold) {
						OutputArray[byteOffset] = 0;
						OutputArray[byteOffset + 1] = 0;
						OutputArray[byteOffset + 2] = 0;
					}
					else {
						OutputArray[byteOffset] = 255;
						OutputArray[byteOffset + 1] = 255;
						OutputArray[byteOffset + 2] = 255;
					}
				}
			}
		}

		public void FilterObjectByDepth() {
			filterFocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X);
			var filterFocalPointDepth = ImageDepthData[filterFocalPointOffset];

			byteOffset = 0;

			for (i = 0; i < PixelCount; i++) {
				measuredValue = ImageDepthData[i] - filterFocalPointDepth;

				if (measuredValue > -300 && measuredValue < 300) {
					OutputArray[byteOffset] = ColorSensorData[byteOffset];
					OutputArray[byteOffset + 1] = ColorSensorData[byteOffset + 1];
					OutputArray[byteOffset + 2] = ColorSensorData[byteOffset + 2];
				}

				byteOffset += 4;
			}
		}

		public void ResetOutput() {
			for (i = 0; i < OutputArray.Length; i++)
				OutputArray[i] = 0;
		}

		/// <summary>
		/// Simply copies the input to the output. Useful in most situations.
		/// </summary>
		public void PrepareOutput() {
			Buffer.BlockCopy(ColorSensorData, 0, OutputArray, 0, ColorSensorData.Length);
		}

		void SetFieldValues() {
			ImageMax = new Point(640, 480);
			ImageMid = new Point(320, 240);

			PixelCount = ImageMax.X * ImageMax.Y;
			ByteCount = ImageMax.X * ImageMax.Y * 4;

			SampleGap = 10;

			if (ImageMid.X % SampleGap > 0)
				throw new Exception("Image width must be evently divisible by sample gap.");

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			FocusPartOffsets = PrepareSquareOffsets(FocusPartArea, ImageMax.X, false);

			FocusRegionWidth = 99;
			FocusRegionArea = FocusRegionWidth * FocusRegionWidth; // 9801

			if (FocusRegionWidth % FocusPartWidth > 0)
				throw new Exception("Focus region width must be evenly divisible by focus part width");

			FocusRegionOffsets = PrepareSquareOffsets(FocusRegionArea, ImageMax.X, false);
			AreaBoundBox = GetCenteredBox(FocusRegionArea);

			PrepareEdgeFilterOffsetsAndWeights();
			PixelEdgeThreshold = 180;

			Window = new Rectangle(-ImageMid.X, -ImageMid.Y, ImageMid.X, ImageMid.Y);

			FocalPoint = new Point();
			OutputArray = new byte[ByteCount];
		}

		void SetDelegates() {
			BrightnessMeasurement = (pixel) => {
				pixel = pixel * 4;
				var measuredValue = 0;

				foreach (var sampleOffset in FocusPartOffsets.Where(offset => pixel + offset > 0 && pixel + offset < ByteCount - 4))
					measuredValue += ColorSensorData[pixel + sampleOffset] + ColorSensorData[pixel + sampleOffset + 1] + ColorSensorData[pixel + sampleOffset + 2];

				return measuredValue;
			};

			DepthMeasurement = (pixel) => {
				return ImageDepthData[pixel];
			};

			DepthValueComparison = (newValue, currentValue) => {
				if (currentValue == 0)
					currentValue = int.MaxValue;

				return newValue > 0 && newValue <= currentValue;
			};

			BrightnessValueComparison = (newValue, currentValue) => {
				return newValue >= currentValue;
			};
		}
	}
}
