using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public class CameraColor : ImageProcessor {
		public uint ByteCount;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var colorFrameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

			ByteCount = colorFrameDescription.LengthInPixels * colorFrameDescription.BytesPerPixel;
			OutputData = new byte[ByteCount];

			OutputWidth = colorFrameDescription.Width;
			OutputHeight = colorFrameDescription.Height;
			OutputStride = (int)(colorFrameDescription.Width * colorFrameDescription.BytesPerPixel);
			OutputUpdateRect = new Int32Rect(0, 0, OutputWidth, OutputHeight);

			OutputImage = new WriteableBitmap(OutputWidth, OutputHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
		}

		public override void ProcessFrame(ColorFrameReference frameReference) {
			using (var colorFrame = frameReference.AcquireFrame()) {
				colorFrame.CopyConvertedFrameDataToArray(OutputData, ColorImageFormat.Bgra);
			}
		}

		void OnFrameArrived(object sender, ColorFrameArrivedEventArgs e) {
			LoadFrame(e.FrameReference);
		}
	}
}