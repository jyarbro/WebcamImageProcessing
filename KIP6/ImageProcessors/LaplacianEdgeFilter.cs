using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Runtime.ExceptionServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe class LaplacianEdgeFilter : ImageProcessor {
		const int OUTPUT_CHUNK_SIZE = 4;
		const int FILTER_THRESHOLD = 60 * 3;

		public uint InputByteCount;
		public uint OutputByteCount;
		public int InputStride;

		public byte[] InputData;

		public LaplaceFilter Filter;

		int _i;
		int _totalEffectiveValue;

		byte* _inputBytePtr;
		byte* _outputBytePtr;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

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
				colorFrame.CopyConvertedFrameDataToArray(InputData, ColorImageFormat.Bgra);
			}
		}

		[HandleProcessCorruptedStateExceptions]
		public void LoadOutputData() {
			fixed (byte* inputDataPtr = InputData) {
				fixed (byte* outputDataPtr = OutputData) {
					_inputBytePtr = inputDataPtr;
					_outputBytePtr = outputDataPtr;

					_i = 0;

					while (_i < InputByteCount) {
						try {
							_totalEffectiveValue = 8 * (*(_inputBytePtr) + *(_inputBytePtr + 1) + *(_inputBytePtr + 2));

							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset1) + *(_inputBytePtr + Filter.Offset1 + 1) + *(_inputBytePtr + Filter.Offset1 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset2) + *(_inputBytePtr + Filter.Offset2 + 1) + *(_inputBytePtr + Filter.Offset2 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset3) + *(_inputBytePtr + Filter.Offset3 + 1) + *(_inputBytePtr + Filter.Offset3 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset4) + *(_inputBytePtr + Filter.Offset4 + 1) + *(_inputBytePtr + Filter.Offset4 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset5) + *(_inputBytePtr + Filter.Offset5 + 1) + *(_inputBytePtr + Filter.Offset5 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset6) + *(_inputBytePtr + Filter.Offset6 + 1) + *(_inputBytePtr + Filter.Offset6 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset7) + *(_inputBytePtr + Filter.Offset7 + 1) + *(_inputBytePtr + Filter.Offset7 + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.Offset8) + *(_inputBytePtr + Filter.Offset8 + 1) + *(_inputBytePtr + Filter.Offset8 + 2);

							if (_totalEffectiveValue >= FILTER_THRESHOLD) {
								*(_outputBytePtr) = 0;
								*(_outputBytePtr + 1) = 0;
								*(_outputBytePtr + 2) = 0;
							}
							else {
								*(_outputBytePtr) = 255;
								*(_outputBytePtr + 1) = 255;
								*(_outputBytePtr + 2) = 255;
							}
						}
						catch (AccessViolationException) { }

						_inputBytePtr += OUTPUT_CHUNK_SIZE;
						_outputBytePtr += OUTPUT_CHUNK_SIZE;
						_i += OUTPUT_CHUNK_SIZE;
					}
				}
			}
		}

		public void CalculateOffsetsAndWeights() {
			// A wider sample seems more accurate. This is probably due to horizontal compression from the Kinect.
			var areaBox = new Rectangle {
				Origin = new Point { X = -2, Y = -1 },
				Extent = new Point { X = 2, Y = 1 },
			};

			var offsets = CalculateOffsets(areaBox, 15, InputStride, 4);

			Filter = new LaplaceFilter {
				Offset1 = offsets[0],
				Offset2 = offsets[2],
				Offset3 = offsets[4],
				Offset4 = offsets[5],
				Offset5 = offsets[9],
				Offset6 = offsets[10],
				Offset7 = offsets[12],
				Offset8 = offsets[14]
			};
		}
	}
}