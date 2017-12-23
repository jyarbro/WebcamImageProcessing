using Microsoft.Kinect;
using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe class CameraMonochrome : ImageProcessor {
		const int INPUT_CHUNK_SIZE = 2; // YUY2
		const int OUTPUT_CHUNK_SIZE = 4; // BGRA

		public uint InputByteCount;
		public uint OutputByteCount;

		public byte[] InputData;

		int _i;
		byte* _inputBytePtr;
		byte* _outputBytePtr;

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

		[HandleProcessCorruptedStateExceptions]
		public void LoadOutputData() {
			fixed (byte* inputData = InputData) {
				fixed (byte* outputData = OutputData) {
					_inputBytePtr = inputData;
					_outputBytePtr = outputData;

					_i = 0;

					while (_i < InputByteCount) {
						try {
							*(_outputBytePtr) = *(_inputBytePtr);
							*(_outputBytePtr + 1) = *(_inputBytePtr);
							*(_outputBytePtr + 2) = *(_inputBytePtr + 2);
							*(_outputBytePtr + 3) = *(_inputBytePtr + 2);
						}
						catch (AccessViolationException) { }

						_inputBytePtr += INPUT_CHUNK_SIZE;
						_outputBytePtr += OUTPUT_CHUNK_SIZE;
						_i += INPUT_CHUNK_SIZE;
					}
				}
			}
		}
	}
}