using System;
using System.Collections.Generic;
using System.Linq;
using KIP3.Helpers;
using Microsoft.Kinect;

namespace KIP3.Infrastructure {
	public class ImageProcessor : Observable {
		#region Fields

		public string StatusText {
			get { return _StatusText ?? (_StatusText = string.Empty); }
			set { SetProperty(ref _StatusText, value); }
		}
		string _StatusText;

		public int FocusPartArea;
		public int FocusPartWidth;
		public int[] FocusPartOffsets;

		public int[] EdgeFilterWeights;
		public int[] EdgeFilterOffsets;

		public int FrameWidth;
		public int FrameHeight;

		public Pixel[] Pixels;
		public GraphLocation[] PixelLocations;

		public ColorImagePoint[] ColorCoordinates;
		public DepthImagePixel[] RawDepthSensorData;

		public int FocusIndex;

		public int PixelCount;
		public int ByteCount;

		public byte[] OutputData;

		int _i;
		int _byteOffset;

		#endregion

		public void Load() {
			ColorCoordinates = new ColorImagePoint[PixelCount];

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			var halfWidth = Convert.ToInt32(Math.Floor((double)FocusPartWidth / 2));

			var window = new Rectangle {
				Origin = new GraphLocation { X = -halfWidth, Y = -halfWidth },
				Extent = new GraphLocation { X = halfWidth, Y = halfWidth }
			};

			FocusPartOffsets = PrepareOffsets(window, FocusPartArea, FrameWidth, true);

			PrepareEdgeFilterOffsetsAndWeights();
			PreparePixels();
		}

		public void ProcessImage() {
			unsafe
			{
				fixed(Pixel* pixels = Pixels)
				{
					fixed(byte* outputData = OutputData)
					{
						var pixel = pixels;
						var outputByte = outputData;
						_i = 0;

						while(_i++ < PixelCount) {
							*(outputByte) = pixel->B;
							*(outputByte + 1) = pixel->G;
							*(outputByte + 2) = pixel->R;

							pixel++;
							outputByte += 4;
						}
					}
				}

				fixed(int* focusPartOffsets = FocusPartOffsets)
				{
					var focusPartOffset = focusPartOffsets;
					var focusOffset = FocusIndex * 4;
					_i = 0;
					_byteOffset = 0;

					while (_i++ < FocusPartOffsets.Length) {
						_byteOffset = focusOffset + *(focusPartOffset);

						if (_byteOffset > 0 && _byteOffset < ByteCount) {
							fixed(byte* outputData = OutputData)
							{
								var outputByte = outputData;
								outputByte += _byteOffset;

								*(outputByte) = 0;
								*(outputByte + 1) = 0;
								*(outputByte + 2) = 255;
							}
						}

						focusPartOffset++;
					}
				}
			}
		}

		/// <summary>
		/// Precalculate pixel values
		/// </summary>
		public void PreparePixels() {
			Pixels = new Pixel[PixelCount];
			PixelLocations = new GraphLocation[PixelCount];

			var imageMidX = FrameWidth / 2;
			var imageMidY = FrameHeight / 2;

			for (var i = 0; i < PixelCount; i++) {
				var y = i / FrameWidth;
				var x = i % FrameWidth;

				var aSq = Math.Pow(x - imageMidX, 2);
				var bSq = Math.Pow(y - imageMidY, 2);
				var cSq = Math.Sqrt(aSq + bSq);

				PixelLocations[i] = new GraphLocation {
					X = x,
					Y = y,
					Distance = cSq
				};
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

			var areaBox = new Rectangle {
				Origin = new GraphLocation { X = -1, Y = -1 },
				Extent = new GraphLocation { X = 1, Y = 1 },
			};

			var edgeFilterOffsets = PrepareOffsets(areaBox, edgeFilterWeights.Count, FrameWidth);

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

		public void CopyFrameData(KinectSensor sensor, ColorImageFrame colorFrame, DepthImageFrame depthFrame) {
			try {
				unsafe
				{
					var i = 0;

					fixed (Pixel* pixels = Pixels)
					{
						fixed (byte* colorSensorData = colorFrame.GetRawPixelData())
						{
							var pixel = pixels;
							var color = colorSensorData;

							while (i++ < PixelCount) {
								pixel->B = *(color);
								pixel->G = *(color + 1);
								pixel->R = *(color + 2);
								pixel->Depth = short.MaxValue;

								color += 4;
								pixel++;
							}
						}
					}

					RawDepthSensorData = depthFrame.GetRawPixelData();
					sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, RawDepthSensorData, ColorImageFormat.RgbResolution640x480Fps30, ColorCoordinates);

					i = 0;

					fixed (DepthImagePixel* depthSensorData = RawDepthSensorData)
					{
						fixed (ColorImagePoint* colorCoordinates = ColorCoordinates)
						{
							var depthData = depthSensorData;
							var depthPoint = colorCoordinates;
							var currentMinimumDepth = int.MaxValue;
							double currentMinimumDistance = double.MaxValue;

							while (i++ < PixelCount) {
								if ((depthPoint->X >= 0 && depthPoint->X < FrameWidth)
									&& (depthPoint->Y >= 0 && depthPoint->Y < FrameHeight)) {

									var depthPixelOffset = depthPoint->Y * FrameWidth + depthPoint->X;

									Pixels[depthPixelOffset].Depth = depthData->Depth;

									if (depthData->Depth > 0 && depthData->Depth <= currentMinimumDepth
										&& PixelLocations[depthPixelOffset].Distance <= currentMinimumDistance) {

										FocusIndex = depthPixelOffset;

										currentMinimumDepth = depthData->Depth;
										currentMinimumDistance = PixelLocations[FocusIndex].Distance;
									}
								}

								depthPoint++;
								depthData++;
							}
						}
					}
				}

				colorFrame.Dispose();
				depthFrame.Dispose();
			}
			catch { }
		}
	}
}
