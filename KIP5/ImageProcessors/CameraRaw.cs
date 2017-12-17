using KIP.Structs;
using KIP5.Services;

namespace KIP5.ImageProcessors {
	unsafe class CameraRaw : ImageProcessor {
		int _i;

		public CameraRaw(SensorReader sensorReader) : base(sensorReader) { }

		protected override void ApplyFilters(Pixel[] sensorData) {
			fixed (Pixel* pixels = sensorData) {
				fixed (byte* outputData = Output) {
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
	}
}