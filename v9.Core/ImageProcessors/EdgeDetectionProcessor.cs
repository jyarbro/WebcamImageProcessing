using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.Helpers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v9.Core.ImageProcessors;

public class EdgeDetectionProcessor(
		ILogger<EdgeDetectionProcessor> logger,
		IFrameRateHandler frameRateHandler
	) : ColorCameraProcessor(
		logger,
		frameRateHandler
	) {

	FilterOffsets FilterLayer;
	int Threshold = 0;
	int ThresholdModifier = 1;

	int _i;
	int _totalEffectiveValue;

	byte[] _InputData = new byte[PIXELS];
	byte[] _OutputData = new byte[PIXELS];

	public async override Task InitializeAsync(MediaCapture mediaCapture) {
		await base.InitializeAsync(mediaCapture);

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

	public unsafe SoftwareBitmap ApplyFilter(SoftwareBitmap bitmap) {
		bitmap.CopyToBuffer(_InputData.AsBuffer());
		bitmap.CopyToBuffer(_OutputData.AsBuffer());

		Threshold += ThresholdModifier;

		if (Threshold == 0 || Threshold == 255) {
			ThresholdModifier *= -1;
		}

		fixed (byte* _inputBytePtr = _InputData)
		fixed (byte* _outputBytePtr = _OutputData) {
			byte* currentInput = _inputBytePtr;
			byte* currentOutput = _outputBytePtr;

			currentInput += FilterLayer.Min;
			currentOutput += FilterLayer.Min;

			_i = FilterLayer.Min;

			while (_i < FilterLayer.Max) {
				_totalEffectiveValue = 8 * (*currentInput + *(currentInput + 1) + *(currentInput + 2));

				_totalEffectiveValue -= *(currentInput + FilterLayer.TL) + *(currentInput + FilterLayer.TL + 1) + *(currentInput + FilterLayer.TL + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.TC) + *(currentInput + FilterLayer.TC + 1) + *(currentInput + FilterLayer.TC + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.TR) + *(currentInput + FilterLayer.TR + 1) + *(currentInput + FilterLayer.TR + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.CL) + *(currentInput + FilterLayer.CL + 1) + *(currentInput + FilterLayer.CL + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.CR) + *(currentInput + FilterLayer.CR + 1) + *(currentInput + FilterLayer.CR + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.BL) + *(currentInput + FilterLayer.BL + 1) + *(currentInput + FilterLayer.BL + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.BC) + *(currentInput + FilterLayer.BC + 1) + *(currentInput + FilterLayer.BC + 2);
				_totalEffectiveValue -= *(currentInput + FilterLayer.BR) + *(currentInput + FilterLayer.BR + 1) + *(currentInput + FilterLayer.BR + 2);

				if (_totalEffectiveValue >= Threshold) {
					*currentOutput = 0;
					*(currentOutput + 1) = 0;
					*(currentOutput + 2) = 0;
				}
				else {
					*currentOutput = 255;
					*(currentOutput + 1) = 255;
					*(currentOutput + 2) = 255;
				}

				currentInput += CHUNK;
				currentOutput += CHUNK;
				_i += CHUNK;
			}
		}

		bitmap.CopyFromBuffer(_OutputData.AsBuffer());

		return bitmap;
	}
}
