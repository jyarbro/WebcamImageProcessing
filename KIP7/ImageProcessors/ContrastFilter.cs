using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP7.ImageProcessors {
	public unsafe class ContrastFilter : ImageProcessor {
		int Chunk;
		int Center;
		int ByteCount;
		int SampleLayerCount;

		byte[] InputData;

		LayerOffsets[] Layers;

		int _i;
		int _j;
		int _pixelValue;

		byte* _currentInput;
		byte* _currentOutput;
		byte* _offsetInput;
		byte* _offsetOutput;

		LayerOffsets _layerOffsets;
		LayerOffsets* _currentLayer;

		public void Initialize(KinectSensor sensor, ColorFrameReader frameReader) {
			frameReader.FrameArrived += OnFrameArrived;

			var frameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

			Chunk = Convert.ToInt32(frameDescription.BytesPerPixel);

			var halfRow = 1920 / 2 * Chunk;
			Center = 1920 * 1080 * Chunk / 2 - 2 - halfRow;

			var pixelCount = Convert.ToInt32(frameDescription.LengthInPixels);
			ByteCount = pixelCount * Chunk;

			InputData = new byte[ByteCount];
			OutputData = new byte[ByteCount];

			OutputHeight = frameDescription.Height;
			OutputWidth = frameDescription.Width;
			OutputStride = OutputWidth * Chunk;
			OutputUpdateRect = new Int32Rect(0, 0, OutputWidth, OutputHeight);

			OutputImage = new WriteableBitmap(OutputWidth, OutputHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

			SampleLayerCount = 3;

			CalculateOffsets();
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
			fixed (byte* inputPtr = InputData)
			fixed (byte* outputPtr = OutputData) {
				_currentInput = inputPtr;
				_currentOutput = outputPtr;

				_i = 0;

				while (_i < ByteCount) {
					fixed (LayerOffsets* layerPtr = Layers) {
						_currentLayer = layerPtr;

						_j = 0;

						while (_j < SampleLayerCount) {
							_layerOffsets = *(_currentLayer);

							//ProcessLayer();

							_currentLayer++;
							_j++;
						}
					}

					_currentInput += Chunk;
					_currentOutput += Chunk;
					_i += Chunk;
				}
			}
		}

		[HandleProcessCorruptedStateExceptions]
		void ProcessLayer() {
			try {
				_offsetInput = _currentInput + _layerOffsets.TL;
				_offsetOutput = _currentOutput + _layerOffsets.TL;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.TC;
				_offsetOutput = _currentOutput + _layerOffsets.TC;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.TR;
				_offsetOutput = _currentOutput + _layerOffsets.TR;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.CL;
				_offsetOutput = _currentOutput + _layerOffsets.CL;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.CC;
				_offsetOutput = _currentOutput + _layerOffsets.CC;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.CR;
				_offsetOutput = _currentOutput + _layerOffsets.CR;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.BL;
				_offsetOutput = _currentOutput + _layerOffsets.BL;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.BC;
				_offsetOutput = _currentOutput + _layerOffsets.BC;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }

			try {
				_offsetInput = _currentInput + _layerOffsets.BR;
				_offsetOutput = _currentOutput + _layerOffsets.BR;
				*(_offsetOutput) = GetPixel();
			}
			catch (AccessViolationException) { }
		}

		[HandleProcessCorruptedStateExceptions]
		byte GetPixel() {
			try {
				_pixelValue = 0;

				// BGR
				_pixelValue += *(_offsetInput);
				_pixelValue += *(_offsetInput + 1);
				_pixelValue += *(_offsetInput + 2);

				// Average brightness
				_pixelValue /= 3;

				// Makes pixelValue a multiple of 8 for some reason
				//_pixelValue -= _pixelValue % 8;

				return (byte) _pixelValue;
			}
			catch (AccessViolationException) { }

			return 255;
		}

		void CalculateOffsets() {
			int offset(int row, int col) => ((OutputWidth * row) + col) * Chunk;

			Layers = new LayerOffsets[SampleLayerCount];

			for (var i = 1; i <= SampleLayerCount; i++) {
				Layers[i - 1] = new LayerOffsets {
					TL = offset(-i, -i),
					TC = offset(-i, 0),
					TR = offset(-i, i),
					CL = offset(0, -i),
					CC = offset(0, 0),
					CR = offset(0, i),
					BL = offset(i, -i),
					BC = offset(i, 0),
					BR = offset(i, i),
				};				
			}
		}
	}
}