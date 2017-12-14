using KIP.Helpers;
using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KIP3.Infrastructure {
	public class ImageProcessor : Observable {
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

		public Pixel[][] OverlayLayers;
		public Pixel[] InputLayer;

		public ColorImagePoint[] CalculatedDepthPoints;
		public DepthImagePixel[] RawDepthPixels;

		public int FocusIndex;

		public int PixelCount;
		public int ByteCount;

		public byte[] OutputData;
		public byte[] ByteScratchLayer;

		int _inputTick;
		int _processTick;
		int _outputTick;

		public enum Layer {
			FocalPoint,
			Middles
		}

		public void LoadProcessor() {
			CalculatedDepthPoints = new ColorImagePoint[PixelCount];

			FocusPartWidth = 11;
			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121

			var halfWidth = Convert.ToInt32(Math.Floor((double)FocusPartWidth / 2));

			var window = new Rectangle {
				Origin = new Point { X = -halfWidth, Y = -halfWidth },
				Extent = new Point { X = halfWidth, Y = halfWidth }
			};

			FocusPartOffsets = PrepareOffsets(window, FocusPartArea, FrameWidth, false);

			PrepareEdgeFilterOffsetsAndWeights();

			PreparePixelLayers();
		}

		public void UpdateInput(KinectSensor sensor, ColorImageFrame colorFrame, DepthImageFrame depthFrame) {
			try {
				CopyColorFrame(colorFrame);
				colorFrame.Dispose();

				CopyDepthFrame(sensor, depthFrame);
				depthFrame.Dispose();
			}
			catch { }
		}

		public void UpdateOutput() {
			//UpdateMiddlesLayer();
			UpdateFocalPointLayer();
			SendLayersToOutput();
		}

		#region Loading

		/// <summary>
		/// Precalculate pixel values
		/// </summary>
		void PreparePixelLayers() {
			var layers = Enum.GetValues(typeof(Layer));

			OverlayLayers = new Pixel[layers.Length][];

			foreach (int layer in layers)
				OverlayLayers[layer] = new Pixel[PixelCount];

			InputLayer = new Pixel[PixelCount];

			var imageMidX = FrameWidth / 2;
			var imageMidY = FrameHeight / 2;

			for (var i = 0; i < PixelCount; i++) {
				var y = i / FrameWidth;
				var x = i % FrameWidth;

				var aSq = Math.Pow(x - imageMidX, 2);
				var bSq = Math.Pow(y - imageMidY, 2);
				var cSq = Math.Sqrt(aSq + bSq);

				InputLayer[i].Location = new Point {
					X = x,
					Y = y,
					Distance = cSq
				};
			}
		}

		/// <summary>
		/// Calculates offsets and weights used in edge filtering.
		/// </summary>
		void PrepareEdgeFilterOffsetsAndWeights() {
			var edgeFilterWeights = new List<int> {
				-1, -1, -1,
				-1,  8, -1,
				-1, -1, -1,
			};

			var areaBox = new Rectangle {
				Origin = new Point { X = -1, Y = -1 },
				Extent = new Point { X = 1, Y = 1 },
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
		int[] PrepareOffsets(Rectangle areaBox, int area, int stride, bool byteMultiplier = true) {
			if (area % 2 == 0)
				throw new Exception("Odd sizes only!");

			var offsets = new int[area];

			var offset = 0;

			for (int yOffset = areaBox.Origin.Y; yOffset <= areaBox.Extent.Y; yOffset++) {
				for (int xOffset = areaBox.Origin.X; xOffset <= areaBox.Extent.X; xOffset++) {
					offsets[offset] = (yOffset * stride) + xOffset;

					if (byteMultiplier)
						offsets[offset] = offsets[offset] * 4;

					offset++;
				}
			}

			return offsets;
		}

		#endregion

		#region Base Layer

		unsafe void CopyColorFrame(ColorImageFrame colorFrame) {
			_inputTick = 0;

			fixed (Pixel* pixels = InputLayer)
			{
				fixed (byte* colorSensorData = colorFrame.GetRawPixelData())
				{
					var pixel = pixels;
					var color = colorSensorData;

					while (_inputTick++ < PixelCount) {
						pixel->B = *(color);
						pixel->G = *(color + 1);
						pixel->R = *(color + 2);
						pixel->Depth = short.MaxValue;

						color += 4;
						pixel++;
					}
				}
			}
		}

		unsafe void CopyDepthFrame(KinectSensor sensor, DepthImageFrame depthFrame) {
			RawDepthPixels = depthFrame.GetRawPixelData();
			sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, RawDepthPixels, ColorImageFormat.RgbResolution640x480Fps30, CalculatedDepthPoints);

			_inputTick = 0;

			fixed (DepthImagePixel* rawDepthPixels = RawDepthPixels)
			{
				fixed (ColorImagePoint* calculatedDepthPoints = CalculatedDepthPoints)
				{
					var rawDepthPixel = rawDepthPixels;
					var calculatedDepthPoint = calculatedDepthPoints;
					var currentMinimumDepth = int.MaxValue;
					double currentMinimumDistance = double.MaxValue;

					while (_inputTick++ < PixelCount) {
						if ((calculatedDepthPoint->X >= 0 && calculatedDepthPoint->X < FrameWidth)
							&& (calculatedDepthPoint->Y >= 0 && calculatedDepthPoint->Y < FrameHeight)) {

							var depthPixelOffset = calculatedDepthPoint->Y * FrameWidth + calculatedDepthPoint->X;

							fixed (Pixel* pixels = InputLayer)
							{
								var pixel = pixels + depthPixelOffset;

								pixel->Depth = rawDepthPixel->Depth;

								if (rawDepthPixel->Depth > 0 && rawDepthPixel->Depth <= currentMinimumDepth
									&& pixel->Location.Distance <= currentMinimumDistance) {

									FocusIndex = depthPixelOffset;

									currentMinimumDepth = rawDepthPixel->Depth;
									currentMinimumDistance = pixel->Location.Distance;
								}
							}
						}

						calculatedDepthPoint++;
						rawDepthPixel++;
					}
				}
			}
		}

		#endregion

		#region Processed Layers

		unsafe void UpdateFocalPointLayer() {
			_processTick = 0;

			fixed (int* focusPartOffsets = FocusPartOffsets)
			{
				var focusPartOffset = focusPartOffsets;

				while (_processTick++ < FocusPartOffsets.Length) {
					var offset = FocusIndex + *(focusPartOffset);

					if (offset > 0 && offset < PixelCount)
						OverlayLayers[(int)Layer.FocalPoint][offset].R = 255;

					focusPartOffset++;
				}
			}

			_processTick = 0;
		}

		unsafe void UpdateMiddlesLayer() {
			_processTick = 0;
			int x = 0;
			int y = 0;

			fixed (Pixel* inputPixels = InputLayer, outputPixels = OverlayLayers[(int)Layer.Middles])
			{
				var inputPixel = inputPixels;
				var outputPixel = outputPixels;

				while (_processTick++ < PixelCount) {
					fixed(Pixel* neighbors = InputLayer)
					{
						var neighbor = neighbors;

						var j = -4;
						var neighborTotal = 0;

						while (j++ < 5) {
							if (j != 0)
								neighborTotal += neighbor->B + neighbor->G + neighbor->R;

							neighbor++;
						}

						if (neighborTotal > (inputPixel->B + inputPixel->G + inputPixel->R) * 8 + 180) {
							outputPixel->B = 0;
							outputPixel->G = 0;
							outputPixel->R = 255;
						}

						j = 0;
					}

					x++;

					if (x > FrameWidth) {
						x = 0;
						y++;
					}

					inputPixel++;
					outputPixel++;
				}
			}
		}

		#endregion
		
		unsafe void SendLayersToOutput() {
			ByteScratchLayer = new byte[ByteCount];

			foreach (var pixelLayer in OverlayLayers) {
				_outputTick = 0;

				fixed (Pixel* pixels = pixelLayer)
				{
					fixed (byte* outputData = ByteScratchLayer)
					{
						var pixel = pixels;
						var outputByte = outputData;

						while (_outputTick++ < PixelCount) {
							if (*(outputByte) == 0) *(outputByte) = pixel->B;
							if (*(outputByte + 1) == 0) *(outputByte + 1) = pixel->G;
							if (*(outputByte + 2) == 0) *(outputByte + 2) = pixel->R;

							pixel->B = 0;
							pixel->G = 0;
							pixel->R = 0;

							pixel++;
							outputByte += 4;
						}
					}
				}
			}

			_outputTick = 0;

			fixed (Pixel* pixels = InputLayer)
			{
				fixed (byte* outputData = ByteScratchLayer)
				{
					var pixel = pixels;
					var outputByte = outputData;

					while (_outputTick++ < PixelCount) {
						if (*(outputByte) == 0) *(outputByte) = pixel->B;
						if (*(outputByte + 1) == 0) *(outputByte + 1) = pixel->G;
						if (*(outputByte + 2) == 0) *(outputByte + 2) = pixel->R;

						pixel++;
						outputByte += 4;
					}
				}
			}

			Buffer.BlockCopy(ByteScratchLayer, 0, OutputData, 0, ByteCount);
		}
	}
}
