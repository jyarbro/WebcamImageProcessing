using KIP.Structs;
using Microsoft.Kinect;

namespace KIP6.ImageProcessors {
	unsafe class CameraRaw : ImageProcessor {
		int _i;

		public void ApplyFilters(Pixel[] sensorData) {
			fixed (Pixel* pixels = sensorData) {
				fixed (byte* outputData = OutputData) {
					var pixel = pixels;
					var outputByte = outputData;
					_i = -1;

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

		public override void ProcessFrame(ColorFrameReference frameReference) => throw new System.NotImplementedException();
	}
}