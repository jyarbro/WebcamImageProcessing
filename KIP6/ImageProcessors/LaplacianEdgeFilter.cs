using KIP.Structs;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe class LaplacianEdgeFilter : ImageProcessor {
		const int OUTPUT_CHUNK_SIZE = 4;
		const int FILTER_THRESHOLD = 128 * 3;

		public uint InputByteCount;
		public uint OutputByteCount;
		public int InputStride;

		public byte[] InputData;

		public LaplaceFilter Filter;

		int _i;
		int _j;
		byte _pixel;
		int _pixelValue;

		byte* _inputBytePtr;
		byte* _outputBytePtr;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.FrameDescription;

			InputByteCount = frameDescription.LengthInPixels * frameDescription.BytesPerPixel;
			InputData = new byte[InputByteCount];

			InputStride = frameDescription.Width * (int) frameDescription.BytesPerPixel;

			OutputByteCount = frameDescription.LengthInPixels * OUTPUT_CHUNK_SIZE;
			OutputData = new byte[OutputByteCount];

			OutputHeight = frameDescription.Height;
			OutputWidth = frameDescription.Width;
			OutputStride = frameDescription.Width * OUTPUT_CHUNK_SIZE;

			OutputUpdateRect = new System.Windows.Int32Rect(0, 0, OutputWidth, OutputHeight);
			OutputImage = new WriteableBitmap(OutputWidth, OutputHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

			CalculateOffsetsAndWeights();
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
			fixed (byte* inputDataPtr = InputData) {
				fixed (byte* outputDataPtr = OutputData) {
					_inputBytePtr = inputDataPtr;
					_outputBytePtr = outputDataPtr;

					_i = -1;

					while (_i++ < InputByteCount) {
						if (_i % 2 == 0) {
							*(_outputBytePtr) = *(_inputBytePtr);
							*(_outputBytePtr + 1) = *(_inputBytePtr);
						}
						else {
							*(_outputBytePtr) = *(_inputBytePtr + 1);
							*(_outputBytePtr + 1) = *(_inputBytePtr + 1);
						}

						_inputBytePtr++;
						_outputBytePtr += 2;
					}
				}
			}
		}

		public void CalculateOffsetsAndWeights() {
			// A wider sample seems more accurate. This is probably due to horizontal compression from the Kinect.

			var weights = new List<int> {
				-1,  0, -1,  0, -1,
				-1,  0,  8,  0, -1,
				-1,  0, -1,  0, -1,
			};

			var areaBox = new Rectangle {
				Origin = new Point { X = -2, Y = -1 },
				Extent = new Point { X = 2, Y = 1 },
			};

			var offsets = CalculateOffsets(areaBox, weights.Count, InputStride);

			Filter = new LaplaceFilter {
				Weight1 = -1,
				Offset1 = offsets[0],
				Weight2 = -1,
				Offset2 = offsets[1],
				Weight3 = -1,
				Offset3 = offsets[2],
				Weight4 = -1,
				Offset4 = offsets[3],
				Weight5 = 8,
				Offset5 = offsets[4],
				Weight6 = -1,
				Offset6 = offsets[5],
				Weight7 = -1,
				Offset7 = offsets[6],
				Weight8 = -1,
				Offset8 = offsets[7],
				Weight9 = -1,
				Offset9 = offsets[8],
			};
		}
	}
}