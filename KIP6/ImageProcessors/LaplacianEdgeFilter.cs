using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Runtime.ExceptionServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe class LaplacianEdgeFilter : ImageProcessor {
		const int FILTER_THRESHOLD = 25 * 3;

		public int InputByteCount;
		public int OutputByteCount;
		public int InputStride;

		public byte[] InputData;

		public FilterOffsets Filter;

		int _i;
		int _totalEffectiveValue;

		byte* _inputBytePtr;
		byte* _outputBytePtr;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
			Chunk = Convert.ToInt32(frameDescription.BytesPerPixel);
			var pixelCount = Convert.ToInt32(frameDescription.LengthInPixels);

			InputByteCount = pixelCount * Chunk;
			InputData = new byte[InputByteCount];

			InputStride = frameDescription.Width * (int) frameDescription.BytesPerPixel;

			OutputByteCount = pixelCount * Chunk;
			OutputData = new byte[OutputByteCount];

			OutputHeight = frameDescription.Height;
			OutputWidth = frameDescription.Width;
			OutputStride = frameDescription.Width * Chunk;

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

							_totalEffectiveValue -= *(_inputBytePtr + Filter.TL) + *(_inputBytePtr + Filter.TL + 1) + *(_inputBytePtr + Filter.TL + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.TC) + *(_inputBytePtr + Filter.TC + 1) + *(_inputBytePtr + Filter.TC + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.TR) + *(_inputBytePtr + Filter.TR + 1) + *(_inputBytePtr + Filter.TR + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.CL) + *(_inputBytePtr + Filter.CL + 1) + *(_inputBytePtr + Filter.CL + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.CR) + *(_inputBytePtr + Filter.CR + 1) + *(_inputBytePtr + Filter.CR + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.BL) + *(_inputBytePtr + Filter.BL + 1) + *(_inputBytePtr + Filter.BL + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.BC) + *(_inputBytePtr + Filter.BC + 1) + *(_inputBytePtr + Filter.BC + 2);
							_totalEffectiveValue -= *(_inputBytePtr + Filter.BR) + *(_inputBytePtr + Filter.BR + 1) + *(_inputBytePtr + Filter.BR + 2);

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

						_inputBytePtr += Chunk;
						_outputBytePtr += Chunk;
						_i += Chunk;
					}
				}
			}
		}

		public void CalculateOffsetsAndWeights() {
			int offset(int row, int col) => ((OutputWidth * row) + col) * Chunk;

			FilterOffsets filterOffsets(int layer) => new FilterOffsets {
				TL = offset(-layer, -layer),
				TC = offset(-layer, 0),
				TR = offset(-layer, layer),
				CL = offset(0, -layer),
				CC = offset(0, 0),
				CR = offset(0, layer),
				BL = offset(layer, -layer),
				BC = offset(layer, 0),
				BR = offset(layer, layer),
			};

			Filter = filterOffsets(3);
		}
	}
}