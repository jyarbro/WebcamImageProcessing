using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP7.ImageProcessors {
	public class CameraColor : ImageProcessor {
		public uint ByteCount;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

			ByteCount = frameDescription.LengthInPixels * frameDescription.BytesPerPixel;
			OutputData = new byte[ByteCount];

			OutputHeight = frameDescription.Height;
			OutputWidth = frameDescription.Width;
			OutputStride = (int)(OutputWidth * frameDescription.BytesPerPixel);
			OutputUpdateRect = new Int32Rect(0, 0, OutputWidth, OutputHeight);

			OutputImage = new WriteableBitmap(OutputWidth, OutputHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
		}

		public override void ProcessFrame(ColorFrameReference frameReference) {
			using (var colorFrame = frameReference.AcquireFrame()) {
				colorFrame.CopyConvertedFrameDataToArray(OutputData, ColorImageFormat.Bgra);
			}
		}
	}
}