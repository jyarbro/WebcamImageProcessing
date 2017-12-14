using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP4.Services {
	public class ImageProcessorService {
		const int FOCUSPARTWIDTH = 11;

		public WriteableBitmap OutputImage { get; } = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);

		FrameDescription FrameDescription;
		Int32Rect FrameChangedRect;
		Pixel[][] OverlayLayers;
		Pixel[] InputLayer;
		int FrameWidth;
		int FrameHeight;
		int FocusPartArea;
		uint PixelCount;
		uint ByteCount;
		int[] FocusPartOffsets;
		int[] EdgeFilterWeights;
		int[] EdgeFilterOffsets;
		byte[] OutputData;

		int _processTick;

		public void UpdateInput(ColorFrame colorFrame) {
			CopyColorFrame(colorFrame);

			// I had a try-catch here. why??
		}

		void CopyColorFrame(ColorFrame colorFrame) {
			FrameDescription = colorFrame.FrameDescription;

			if (FrameDescription.Width != OutputImage.PixelWidth || FrameDescription.Height != OutputImage.PixelHeight)
				return;

			using (var colorBuffer = colorFrame.LockRawImageBuffer()) {
				colorFrame.CopyConvertedFrameDataToArray(OutputData, ColorImageFormat.Bgra);
			}
		}

		public void UpdateOutput() {
			OutputImage.Lock();

			// copy outputData to OutputImage

			OutputImage.AddDirtyRect(FrameChangedRect);
			OutputImage.Unlock();
		}

		/// <summary>
		/// ImageProcessorService factory
		/// </summary>
		public static ImageProcessorService Create(FrameDescription frameDescription) {
			var frameWidth = frameDescription.Width;
			var frameHeight = frameDescription.Height;
			var pixelCount = frameDescription.LengthInPixels;
			var byteCount = frameDescription.LengthInPixels * 4;

			var halfWidth = Convert.ToInt32(Math.Floor((double) FOCUSPARTWIDTH / 2));

			var window = new Rectangle {
				Origin = new KIP.Structs.Point { X = -halfWidth, Y = -halfWidth },
				Extent = new KIP.Structs.Point { X = halfWidth, Y = halfWidth }
			};

			var focusPartArea = FOCUSPARTWIDTH * FOCUSPARTWIDTH; // 121

			var service = new ImageProcessorService {
				FrameWidth = frameWidth,
				FrameHeight = frameHeight,
				FocusPartArea = focusPartArea,
				PixelCount = pixelCount,
				ByteCount = byteCount,
				OutputData = new byte[byteCount],
				FrameChangedRect = new Int32Rect(0, 0, frameDescription.Width, frameDescription.Height)
			};

			service.FocusPartOffsets = service.PrepareOffsets(window, focusPartArea, frameWidth, false);
			service.PrepareEdgeFilterOffsetsAndWeights();
			service.PreparePixelLayers(frameDescription);

			return service;
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
				Origin = new KIP.Structs.Point { X = -1, Y = -1 },
				Extent = new KIP.Structs.Point { X = 1, Y = 1 },
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
		/// <exception cref="ArgumentException" />
		int[] PrepareOffsets(Rectangle areaBox, int area, int stride, bool byteMultiplier = true) {
			if (area % 2 == 0)
				throw new ArgumentException("Odd sizes only.");

			var offsets = new int[area];

			var offset = 0;

			for (var yOffset = areaBox.Origin.Y; yOffset <= areaBox.Extent.Y; yOffset++) {
				for (var xOffset = areaBox.Origin.X; xOffset <= areaBox.Extent.X; xOffset++) {
					offsets[offset] = (yOffset * stride) + xOffset;

					if (byteMultiplier)
						offsets[offset] = offsets[offset] * 4;

					offset++;
				}
			}

			return offsets;
		}

		/// <summary>
		/// Precalculate pixel values
		/// </summary>
		void PreparePixelLayers(FrameDescription frameDescription) {
			var frameWidth = frameDescription.Width;
			var frameHeight = frameDescription.Height;
			var pixelCount = frameDescription.LengthInPixels;
			
			var layers = Enum.GetValues(typeof(ELayer));

			OverlayLayers = new Pixel[layers.Length][];

			foreach (int layer in layers)
				OverlayLayers[layer] = new Pixel[pixelCount];

			InputLayer = new Pixel[pixelCount];

			var imageMidX = frameWidth / 2;
			var imageMidY = frameHeight / 2;

			for (var i = 0; i < pixelCount; i++) {
				var y = i / frameWidth;
				var x = i % frameWidth;

				var aSq = Math.Pow(x - imageMidX, 2);
				var bSq = Math.Pow(y - imageMidY, 2);
				var cSq = Math.Sqrt(aSq + bSq);

				InputLayer[i].Location = new KIP.Structs.Point {
					X = x,
					Y = y,
					Distance = cSq
				};
			}
		}

		enum ELayer {
			FocalPoint,
			Middles
		}
	}
}