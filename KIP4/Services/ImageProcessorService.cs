using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP4.Services {
	public class ImageProcessorService {
		const int FOCUSPARTWIDTH = 11;

		public WriteableBitmap OutputImage { get; } = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);

		KinectBuffer ColorBuffer;
		Int32Rect FrameChangedRect;
		Pixel[][] OverlayLayers;
		Pixel[] Pixels;
		int FrameWidth;
		int FrameHeight;
		int FrameStride;
		int FocusPartArea;
		uint PixelCount;
		uint ByteCount;
		int[] FocusPartOffsets;
		int[] EdgeFilterWeights;
		int[] EdgeFilterOffsets;
		byte[] ColorFrameData;
		byte[] OutputData;

		int _processTick;
		int _i;

		public void UpdateInput(ColorFrame colorFrame) {
			if (colorFrame.FrameDescription.Width != OutputImage.PixelWidth || colorFrame.FrameDescription.Height != OutputImage.PixelHeight)
				return;

			using (ColorBuffer = colorFrame.LockRawImageBuffer()) {
				// Do I need a lock?
				lock(ColorFrameData) {
					colorFrame.CopyConvertedFrameDataToArray(ColorFrameData, ColorImageFormat.Bgra);
				}
			}

			// I had a try-catch here. why??
		}

		public void UpdateOutput() {
			CopyFrameData();
			SendToOutputData();

			Application.Current?.Dispatcher.Invoke(() => {
				OutputImage.Lock();
				OutputImage.WritePixels(FrameChangedRect, OutputData, FrameStride, 0);
				//OutputImage.AddDirtyRect(FrameChangedRect);
				OutputImage.Unlock();
			});
		}

		void CopyFrameData() {
			unsafe {
				_i = 0;

				fixed (Pixel* pixels = Pixels) {
					fixed (byte* inputData = ColorFrameData) {
						var pixel = pixels;
						var color = inputData;

						while (_i++ < PixelCount) {
							pixel->B = *(color);
							pixel->G = *(color + 1);
							pixel->R = *(color + 2);

							color += 4;
							pixel++;
						}
					}
				}
			}
		}

		void SendToOutputData() {
			unsafe {
				fixed (Pixel* pixels = Pixels) {
					fixed (byte* outputData = OutputData) {
						var pixel = pixels;
						var outputByte = outputData;
						_i = 0;

						while (_i++ < PixelCount) {
							*(outputByte) = pixel->B;
							*(outputByte + 1) = pixel->G;
							*(outputByte + 2) = pixel->R;

							pixel++;
							outputByte += 4;
						}
					}
				}
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

			Pixels = new Pixel[pixelCount];

			var imageMidX = frameWidth / 2;
			var imageMidY = frameHeight / 2;

			for (var i = 0; i < pixelCount; i++) {
				var y = i / frameWidth;
				var x = i % frameWidth;

				var aSq = Math.Pow(x - imageMidX, 2);
				var bSq = Math.Pow(y - imageMidY, 2);
				var cSq = Math.Sqrt(aSq + bSq);

				Pixels[i].Location = new KIP.Structs.Point {
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

		/// <summary>
		/// ImageProcessorService factory
		/// </summary>
		public static ImageProcessorService Create(FrameDescription frameDescription) {
			var frameWidth = frameDescription.Width;
			var frameHeight = frameDescription.Height;
			var frameStride = frameWidth * 4;
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
				FrameStride = frameStride,
				FocusPartArea = focusPartArea,
				PixelCount = pixelCount,
				ByteCount = byteCount,
				OutputData = new byte[byteCount],
				ColorFrameData = new byte[byteCount],
				FrameChangedRect = new Int32Rect(0, 0, frameDescription.Width, frameDescription.Height)
			};

			service.FocusPartOffsets = service.PrepareOffsets(window, focusPartArea, frameWidth, false);
			service.PrepareEdgeFilterOffsetsAndWeights();
			service.PreparePixelLayers(frameDescription);

			return service;
		}
	}
}