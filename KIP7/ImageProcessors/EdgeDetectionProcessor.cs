using KIP7.FrameRate;
using KIP7.Helpers;
using KIP7.Logger;
using KIP7.Structs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;

namespace KIP7.ImageProcessors {
	public class EdgeDetectionProcessor : ColorCameraProcessor {
		FilterOffsets FilterLayer;
		int Threshold = 0;
		int ThresholdModifier = 1;

		int _i;
		int _totalEffectiveValue;
		
		public EdgeDetectionProcessor(
			ILogger logger,
			IFrameRateManager frameRateManager,
			CoreDispatcher dispatcher
		) : base(
			logger,
			frameRateManager,
			dispatcher
		) { }

		public override async Task InitializeAsync(MediaCapture mediaCapture) {
			await base.InitializeAsync(mediaCapture);

			OutputWidth = 1920;
			OutputHeight = 1080;

			FilterLayer = PrecalculateFilterOffsets(1);
		}

		public override SoftwareBitmap ConvertFrame(VideoMediaFrame frame) {
			try {
				var input = SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
				return ApplyFilter(input);
			}
			catch (ObjectDisposedException) { }

			return null;
		}

		public unsafe SoftwareBitmap ApplyFilter(SoftwareBitmap input) {
			var output = new SoftwareBitmap(BitmapPixelFormat.Bgra8, 1920, 1080, BitmapAlphaMode.Premultiplied);

			Threshold += ThresholdModifier;

			if (Threshold == 0 || Threshold == 255)
				ThresholdModifier *= -1;

			using (var inputBuffer = input.LockBuffer(BitmapBufferAccessMode.ReadWrite))
			using (var outputBuffer = output.LockBuffer(BitmapBufferAccessMode.ReadWrite))
			using (var inputReference = inputBuffer.CreateReference())
			using (var outputReference = outputBuffer.CreateReference()) {
				((IMemoryBufferByteAccess) inputReference).GetBuffer(out var inputDataPtr, out var inputCapacity);
				((IMemoryBufferByteAccess) outputReference).GetBuffer(out var outputDataPtr, out var outputCapacity);

				var _inputBytePtr = inputDataPtr;
				var _outputBytePtr = outputDataPtr;

				_inputBytePtr += FilterLayer.Min;
				_outputBytePtr += FilterLayer.Min;

				_i = FilterLayer.Min;

				while (_i < FilterLayer.Max) {
					_totalEffectiveValue = 8 * (*(_inputBytePtr) + *(_inputBytePtr + 1) + *(_inputBytePtr + 2));

					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.TL) + *(_inputBytePtr + FilterLayer.TL + 1) + *(_inputBytePtr + FilterLayer.TL + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.TC) + *(_inputBytePtr + FilterLayer.TC + 1) + *(_inputBytePtr + FilterLayer.TC + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.TR) + *(_inputBytePtr + FilterLayer.TR + 1) + *(_inputBytePtr + FilterLayer.TR + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.CL) + *(_inputBytePtr + FilterLayer.CL + 1) + *(_inputBytePtr + FilterLayer.CL + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.CR) + *(_inputBytePtr + FilterLayer.CR + 1) + *(_inputBytePtr + FilterLayer.CR + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.BL) + *(_inputBytePtr + FilterLayer.BL + 1) + *(_inputBytePtr + FilterLayer.BL + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.BC) + *(_inputBytePtr + FilterLayer.BC + 1) + *(_inputBytePtr + FilterLayer.BC + 2);
					_totalEffectiveValue -= *(_inputBytePtr + FilterLayer.BR) + *(_inputBytePtr + FilterLayer.BR + 1) + *(_inputBytePtr + FilterLayer.BR + 2);

					if (_totalEffectiveValue >= Threshold) {
						*(_outputBytePtr) = 0;
						*(_outputBytePtr + 1) = 0;
						*(_outputBytePtr + 2) = 0;
					}
					else {
						*(_outputBytePtr) = 255;
						*(_outputBytePtr + 1) = 255;
						*(_outputBytePtr + 2) = 255;
					}

					_inputBytePtr += CHUNK;
					_outputBytePtr += CHUNK;
					_i += CHUNK;
				}
			}

			return output;
		}
	}
}
