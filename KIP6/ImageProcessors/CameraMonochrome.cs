using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe class CameraMonochrome : ImageProcessor {
		const int OUTPUT_CHUNK_SIZE = 4; // BGRA

		public uint InputByteCount;
		public uint OutputByteCount;

		public byte[] InputData;

		int _i;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.FrameDescription;

			InputByteCount = frameDescription.LengthInPixels * frameDescription.BytesPerPixel;
			InputData = new byte[InputByteCount];

			OutputByteCount = frameDescription.LengthInPixels * OUTPUT_CHUNK_SIZE;
			OutputData = new byte[OutputByteCount];

			OutputHeight = frameDescription.Height;
			OutputWidth = frameDescription.Width;
			OutputStride = OutputWidth * OUTPUT_CHUNK_SIZE;
			OutputUpdateRect = new Int32Rect(0, 0, OutputWidth, OutputHeight);

			OutputImage = new WriteableBitmap(OutputWidth, OutputHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
		}

		public override void ProcessFrame(ColorFrameReference frameReference) {
			LoadInputData(frameReference);
			LoadOutputData();
		}

		public void LoadInputData(ColorFrameReference frameReference) {
			using (var colorFrame = frameReference.AcquireFrame()) {
				colorFrame.CopyRawFrameDataToArray(InputData);
			}
		}

		public void LoadOutputData() {
			fixed (byte* inputData = InputData) {
				fixed (byte* outputData = OutputData) {
					var inputByte = inputData;
					var outputByte = outputData;

					_i = -1;

					while (_i++ < InputByteCount) {
						if (_i % 2 == 0) {
							*(outputByte) = *(inputByte);
							*(outputByte + 1) = *(inputByte);
						}
						else {
							*(outputByte) = *(inputByte + 1);
							*(outputByte + 1) = *(inputByte + 1);
						}

						inputByte++;
						outputByte += 2;
					}
				}
			}
		}
	}
}