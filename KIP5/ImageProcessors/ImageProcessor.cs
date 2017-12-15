using KIP.Structs;
using KIP5.Helpers;
using KIP5.Interfaces;
using KIP5.Services;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP5.ImageProcessors {
	abstract class ImageProcessor : IImageProcessor {
		public WriteableBitmap OutputImage { get; } = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);

		protected Int32Rect FrameRect;
		protected int FrameStride;
		protected uint PixelCount;
		protected byte[] OutputData;

		protected int _i;

		public ImageProcessor(SensorReader sensorReader) {
			FrameRect = new Int32Rect(0, 0, sensorReader.SensorImageWidth, sensorReader.SensorImageHeight);
			FrameStride = sensorReader.SensorImageWidth * 4;
			PixelCount = sensorReader.PixelCount;

			OutputData = new byte[sensorReader.ByteCount];

			sensorReader.SensorDataReady += OnSensorDataReady;
		}

		protected abstract void LoadInput(Pixel[] sensorData);
		protected abstract void ApplyFilters();
		protected abstract void WriteOutput();

		void OnSensorDataReady(object sender, SensorDataReadyEventArgs args) {
			LoadInput(args.SensorData);
			ApplyFilters();
			WriteOutput();
		}
	}
}