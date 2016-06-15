using System;
using System.Collections.Generic;
using System.Linq;

namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// The base for all image processors
	/// </summary>
	public abstract class ImageProcessorBase {
		#region Fields

		public int FocusRegionArea;
		public int FocusRegionWidth;

		public int[] FocusRegionOffsets;

		public int FocusPartArea;
		public int FocusPartWidth;
		public int FocusPartHorizontalCount;
		public int FocusPartTotalCount;

		public int FocalPointOffset;

		public List<int[]> FocusPartOffsets;
		public List<byte[]> FocusParts;

		public int SampleGap;
		public int SampleByteCount;
		public int SampleCenterOffset;

		public int[] SampleOffsets;

		public int[] CompressedSensorData;
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

		int _sample;

		#endregion

		public ImageProcessorBase() {
			SetFieldValues();
			SetDelegates();
		}

		/// <summary>
		/// Optional method for overriding.
		/// </summary>
		public virtual void Prepare() { }

		/// <summary>
		/// Requires override
		/// </summary>
		public abstract byte[] ProcessImage();

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
		/// Calculate a window around a centered point given only the square's area.
		/// </summary>
		public Rectangle GetCenteredBox(int area) {
			var areaMax = Convert.ToInt32(Math.Floor(Math.Sqrt(area) / 2));
			var areaMin = areaMax * -1;

			return new Rectangle(areaMin, areaMin, areaMax, areaMax);
		}

		/// <summary>
		/// Calculates offsets used in sampling.
		/// </summary>
		public void PrepareSampleOffsets() {
			SampleByteCount = FocusPartArea * 4;
			SampleOffsets = PrepareSquareOffsets(FocusPartArea, ImageMax.X);
			SampleCenterOffset = Convert.ToInt32(Math.Floor(Math.Sqrt(FocusPartArea) / 2));
		}

		/// <summary>
		/// Calculatets offsets used when focal region is split into parts
		/// </summary>
		public void PrepareFocusPartOffsets() {
			var focusOffsets = PrepareSquareOffsets(FocusRegionArea, ImageMax.X);

			for (int i = 0; i < FocusPartTotalCount; i++) {
				FocusParts.Add(new byte[FocusPartArea * 4]);
				FocusPartOffsets.Add(new int[FocusPartArea]);
			}

			for (var i = 0; i < focusOffsets.Length; i++) {
				var y = i / FocusRegionWidth;
				var x = i % FocusRegionWidth;

				var focusPartRow = y / FocusPartWidth;
				var focusPartCol = x / FocusPartWidth;

				var focusPartOffset = focusPartCol + (focusPartRow * FocusPartHorizontalCount);

				FocusPartOffsets[focusPartOffset][i % FocusPartArea] = focusOffsets[i];
			}
		}

		/// <summary>
		/// Compresses ColorSensorData by 50%
		/// </summary>
		public void PrepareCompressedSensorData() {
			int y;
			int x;
			int pixel;
			int compressedValue;

			for (y = 0; y < ImageMax.Y; y += 2) {
				for (x = 0; x < ImageMax.X; x += 2) {
					pixel = ((y * ImageMax.X) + x) * 4;

					compressedValue =
						ColorSensorData[pixel] + ColorSensorData[pixel + 1] + ColorSensorData[pixel + 2]
						+ ColorSensorData[pixel + 4] + ColorSensorData[pixel + 5] + ColorSensorData[pixel + 6];

					pixel = (((y + 1) * ImageMax.X) + x) * 4;

					compressedValue +=
						ColorSensorData[pixel] + ColorSensorData[pixel + 1] + ColorSensorData[pixel + 2]
						+ ColorSensorData[pixel + 4] + ColorSensorData[pixel + 5] + ColorSensorData[pixel + 6];

					CompressedSensorData[((y / 2) * ImageMid.X) + (x / 2)] = compressedValue / 12;
				}
			}
		}

		/// <summary>
		/// Calculates the closest focal point near a target coordinate
		/// </summary>
		public Point GetNearestFocalPoint(Rectangle window, Point target) {
			return GetMeasuredFocalPoint(window, target, DepthMeasurement, DepthValueComparison);
		}

		/// <summary>
		/// Calculates the brightest focal point near a target coordinate
		/// </summary>
		public Point GetBrightestFocalPoint(Rectangle window, Point target) {
			return GetMeasuredFocalPoint(window, target, BrightnessMeasurement, BrightnessValueComparison);
		}

		public Point GetMeasuredFocalPoint(Rectangle window, Point target, Func<int, int> measurement, Func<int, int, bool> valueComparison) {
			var focalPoint = new Point();

			int x;
			int y;
			int yOffset;
			int measuredValue;

			var xSq = Math.Pow(Math.Abs(ImageMid.X), 2);
			var ySq = Math.Pow(Math.Abs(ImageMid.Y), 2);

			var closestPixelDistance = Math.Sqrt(xSq + ySq);

			double distanceFromCenter;
			int highestMeasuredValue = 0;

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

							focalPoint.X = x;
							focalPoint.Y = y;
						}
					}
				}
			}

			return focalPoint;
		}

		/// <summary>
		/// Add blue pixels for sampling grid
		/// </summary>
		public void OverlaySampleGrid() {
			for (int y = 0; y < ImageMax.Y; y += SampleGap) {
				var yOffset = y * ImageMax.X;

				for (int x = 0; x < ImageMax.X; x += SampleGap) {
					var pixel = (yOffset + x) * 4;

					OutputArray[pixel + 0] = 255;
					OutputArray[pixel + 1] = 0;
					OutputArray[pixel + 2] = 0;
				}
			}
		}

		/// <summary>
		/// Add color spot to highlight focal point
		/// </summary>
		public void OverlayFocalPoint(int color, int focalPointOffset) {
			foreach (var sampleOffset in SampleOffsets) {
				var sampleByteOffset = sampleOffset * 4;

				if (focalPointOffset + sampleByteOffset > 0 && focalPointOffset + sampleByteOffset < ByteCount) {
					OutputArray[focalPointOffset + sampleByteOffset] = (byte)(color == 1 ? 255 : 0);
					OutputArray[focalPointOffset + sampleByteOffset + 1] = (byte)(color == 2 ? 255 : 0);
					OutputArray[focalPointOffset + sampleByteOffset + 2] = (byte)(color == 3 ? 255 : 0);
				}
			}
		}

		public void FilterEdges(int focalPointOffset) {
			foreach (var baseOffset in FocusRegionOffsets) {
				_sample = 0;

				var byteOffset = baseOffset * 4;
				var pixelOffset = focalPointOffset + byteOffset;

				if (pixelOffset > 0 && pixelOffset < ByteCount) {
					for (var j = 0; j < EdgeFilterOffsets.Length; j++) {
						if (EdgeFilterWeights[j] == 0)
							continue;

						var offset = pixelOffset + EdgeFilterOffsets[j];

						if (offset > 0 && offset < ByteCount)
							_sample += EdgeFilterWeights[j] * (ColorSensorData[offset] + ColorSensorData[offset + 1] + ColorSensorData[offset + 2]);
					}

					if (_sample >= PixelEdgeThreshold) {
						OutputArray[pixelOffset] = 0;
						OutputArray[pixelOffset + 1] = 0;
						OutputArray[pixelOffset + 2] = 0;
					}
					else {
						OutputArray[pixelOffset] = 255;
						OutputArray[pixelOffset + 1] = 255;
						OutputArray[pixelOffset + 2] = 255;
					}
				}
			}
		}

		public void CalculateEdgeFilterValues() {
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
		/// Simply copies the input to the output. Useful in most situations.
		/// </summary>
		public virtual void PrepareOutput() {
			Buffer.BlockCopy(ColorSensorData, 0, OutputArray, 0, ColorSensorData.Length);
		}

		void SetFieldValues() {
			ImageMax = new Point(640, 480);
			ImageMid = new Point(320, 240);

			Window = new Rectangle(-ImageMid.X, -ImageMid.Y, ImageMid.X, ImageMid.Y);

			PixelCount = ImageMid.X * ImageMid.Y;
			ByteCount = ImageMax.X * ImageMax.Y * 4;

			CompressedSensorData = new int[PixelCount];
			OutputArray = new byte[ByteCount];

			SampleGap = 10;

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			FocusRegionWidth = 99;
			FocusRegionArea = FocusRegionWidth * FocusRegionWidth; // 9801
			FocusRegionOffsets = PrepareSquareOffsets(FocusRegionArea, ImageMax.X, false);
			AreaBoundBox = GetCenteredBox(FocusRegionArea);

			if (FocusRegionWidth % FocusPartWidth > 0)
				throw new Exception("Focus area width must be divisible by sample area width");

			FocusPartHorizontalCount = FocusRegionWidth / FocusPartWidth; // 9
			FocusPartTotalCount = FocusPartHorizontalCount * FocusPartHorizontalCount; // 81

			FocusPartOffsets = new List<int[]>();
			FocusParts = new List<byte[]>();

			FocalPoint = new Point();

			SampleOffsets = PrepareSquareOffsets(FocusPartArea, ImageMax.X, false);

			CalculateEdgeFilterValues();
			PixelEdgeThreshold = 180;
		}

		void SetDelegates() {
			BrightnessMeasurement = (pixel) => {
				pixel = pixel * 4;
				var measuredValue = 0;

				foreach (var sampleOffset in SampleOffsets.Where(s => pixel + s > 0 && pixel + s < ByteCount))
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
