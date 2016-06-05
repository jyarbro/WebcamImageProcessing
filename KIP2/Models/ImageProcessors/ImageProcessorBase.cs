using System;
using System.Collections.Generic;

namespace KIP2.Models.ImageProcessors {
	public abstract class ImageProcessorBase {
		public int FocusRegionArea;
		public int FocusRegionWidth;

		public int FocusPartArea;
		public int FocusPartWidth;
		public int FocusPartHorizontalCount;
		public int FocusPartTotalCount;

		public List<int[]> FocusPartOffsets;
		public List<byte[]> FocusParts;

		public int SampleGap;
		public int SampleByteCount;
		public int SampleCenterOffset;

		public int[] SampleOffsets;

		public int[] CompressedSensorData;
		public byte[] ColorSensorData;
		public short[] ImageDepthData;

		public Coordinates ImageMax = new Coordinates { X = 640, Y = 480 };
		public Coordinates ImageMid = new Coordinates { X = 320, Y = 240 };
		public Coordinates FocalPoint = new Coordinates();

		public int ByteCount;
		public int PixelCount;

		public byte[] OutputArray;

		public int FocalPointOffset { get; set; }

		public ImageProcessorBase() {
			PixelCount = ImageMid.X * ImageMid.Y;
			ByteCount = ImageMax.X * ImageMax.Y * 4;

			CompressedSensorData = new int[PixelCount];
			OutputArray = new byte[ByteCount];

			SampleGap = 10;
			SampleOffsets = PrepareSquareOffsets(11 * 11, ImageMax.X, false);
		}

		/// <summary>
		/// Optional method for overriding.
		/// </summary>
		public virtual void Prepare() { }

		public abstract byte[] ProcessImage();

		public int[] PrepareSquareOffsets(int size, int stride, bool byteMultiplier = true) {
			if (size % 2 == 0)
				throw new Exception("Odd sizes only!");

			var offsets = new int[size];
			var areaMax = Convert.ToInt32(Math.Floor(Math.Sqrt(size) / 2));
			var areaMin = areaMax * -1;

			var offset = 0;

			for (int yOffset = areaMin; yOffset <= areaMax; yOffset++) {
				for (int xOffset = areaMin; xOffset <= areaMax; xOffset++) {
					offsets[offset] = xOffset + (yOffset * stride);

					if (byteMultiplier)
						offsets[offset] = offsets[offset] * 4;

					offset++;
				}
			}

			return offsets;
		}

		public void PrepareSampleOffsets() {
			SampleByteCount = FocusPartArea * 4;
			SampleOffsets = PrepareSquareOffsets(FocusPartArea, ImageMax.X);
			SampleCenterOffset = Convert.ToInt32(Math.Floor(Math.Sqrt(FocusPartArea) / 2));
		}

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

		public void PrepareCompressedSensorData() {
			for (int y = 0; y < ImageMax.Y; y += 2) {
				for (int x = 0; x < ImageMax.X; x += 2) {
					var pixel = ((y * ImageMax.X) + x) * 4;

					var compressedValue =
						ColorSensorData[pixel] + ColorSensorData[pixel + 1] + ColorSensorData[pixel + 2]
						+ ColorSensorData[pixel + 4] + ColorSensorData[pixel + 5] + ColorSensorData[pixel + 6];

					pixel = (((y + 1) * ImageMax.X) + x) * 4;

					compressedValue +=
						ColorSensorData[pixel] + ColorSensorData[pixel + 1] + ColorSensorData[pixel + 2]
						+ ColorSensorData[pixel + 4] + ColorSensorData[pixel + 5] + ColorSensorData[pixel + 6];

					CompressedSensorData[((y / 2) * ImageMid.X) + (x / 2)] = compressedValue;
				}
			}
		}

		public Coordinates GetNearestFocalPoint(Coordinates Target) {
			var nearestFocalPoint = new Coordinates();

			var closestPixelValue = 10000;
			var closestPixelDistance = ImageMid.X + ImageMid.Y;

			var maxDistanceFromCenter = 0;

			for (int y = 0; y < ImageMax.Y; y += SampleGap) {
				var yOffset = y * ImageMax.X;

				for (int x = 0; x < ImageMax.X; x += SampleGap) {
					var pixel = yOffset + x;
					var depth = ImageDepthData[pixel];

					if (depth > 0 && depth <= closestPixelValue) {
						// speed cheat - not true hypoteneuse!
						var distanceFromCenter = Math.Abs(x - Target.X) + Math.Abs(y - Target.Y);

						maxDistanceFromCenter = distanceFromCenter;

						if (distanceFromCenter <= closestPixelDistance) {
							closestPixelDistance = distanceFromCenter;
							closestPixelValue = depth;

							nearestFocalPoint.X = x;
							nearestFocalPoint.Y = y;
						}
					}
				}
			}

			return nearestFocalPoint;
		}

		public Coordinates GetBrightestFocalPoint(Coordinates Target) {
			var nearestFocalPoint = new Coordinates();

			var brightestPixelValue = 0;
			var brightestPixelDistance = ImageMid.X + ImageMid.Y;

			var maxDistanceFromCenter = 0;

			for (int y = 0; y < ImageMax.Y; y += SampleGap) {
				var yOffset = y * ImageMax.X;

				for (int x = 0; x < ImageMax.X; x += SampleGap) {
					var pixel = (yOffset + x) * 4;
					var brightness = 0;

					foreach (var sampleOffset in SampleOffsets) {
						if (pixel + sampleOffset > 0 && pixel + sampleOffset < ByteCount) {
							brightness += ColorSensorData[pixel + sampleOffset] + ColorSensorData[pixel + sampleOffset + 1] + ColorSensorData[pixel + sampleOffset + 2];
						}
					}

					if (brightness >= brightestPixelValue) {
						// speed cheat - not true hypoteneuse!
						var distanceFromCenter = Math.Abs(x - Target.X) + Math.Abs(y - Target.Y);

						maxDistanceFromCenter = distanceFromCenter;

						if (distanceFromCenter <= brightestPixelDistance) {
							brightestPixelDistance = distanceFromCenter;
							brightestPixelValue = brightness;

							nearestFocalPoint.X = x;
							nearestFocalPoint.Y = y;
						}
					}
				}
			}

			return nearestFocalPoint;
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
		public void OverlayFocalPoint(int color) {
			foreach (var sampleOffset in SampleOffsets) {
				var sampleByteOffset = sampleOffset * 4;

				if (FocalPointOffset + sampleByteOffset > 0 && FocalPointOffset + sampleByteOffset < ByteCount) {
					OutputArray[FocalPointOffset + sampleByteOffset] = (byte) (color == 1 ? 255 : 0);
					OutputArray[FocalPointOffset + sampleByteOffset + 1] = (byte)(color == 2 ? 255 : 0);
					OutputArray[FocalPointOffset + sampleByteOffset + 2] = (byte)(color == 3 ? 255 : 0);
				}
			}
		}

		public virtual void PrepareOutput() {
			Buffer.BlockCopy(ColorSensorData, 0, OutputArray, 0, ColorSensorData.Length);
		}
	}
}
