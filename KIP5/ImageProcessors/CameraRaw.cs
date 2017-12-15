using KIP.Structs;
using KIP5.Services;
using System.Windows;

namespace KIP5.ImageProcessors {
	unsafe class CameraRaw : ImageProcessor {
		int _i;

		public CameraRaw(SensorReader sensorReader) : base(sensorReader) { }

		protected override void LoadInput(Pixel[] sensorData) {
			fixed (Pixel* pixels = sensorData) {
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

		protected override void ApplyFilters() { }

		protected override void WriteOutput() {
			Application.Current?.Dispatcher.Invoke(() => {
				OutputImage.Lock();
				OutputImage.WritePixels(FrameRect, OutputData, FrameStride, 0);
				OutputImage.Unlock();	
			});
		}
	}
}