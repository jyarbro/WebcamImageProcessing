using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

[DisplayName("Edge Detection")]
public class EdgeFilter : ImageFilterBase, IImageFilter {
	public int Threshold {
		set => _Threshold = value;
	}
	int _Threshold = 80;

	public void Initialize() { }

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		input.CopyToBuffer(_OutputData.AsBuffer());

		fixed (byte* _inputBytePtr = _InputData)
		fixed (byte* _outputBytePtr = _OutputData) {
			byte* currentInput = _inputBytePtr;
			byte* currentOutput = _outputBytePtr;

			currentInput += _FilterOffsets.Min;
			currentOutput += _FilterOffsets.Min;

			_i = _FilterOffsets.Min;

			while (_i < _FilterOffsets.Max) {
				_TotalEffectiveValue = 8 * (*currentInput + *(currentInput + 1) + *(currentInput + 2));

				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.TL) + *(currentInput + _FilterOffsets.TL + 1) + *(currentInput + _FilterOffsets.TL + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.TC) + *(currentInput + _FilterOffsets.TC + 1) + *(currentInput + _FilterOffsets.TC + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.TR) + *(currentInput + _FilterOffsets.TR + 1) + *(currentInput + _FilterOffsets.TR + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.CL) + *(currentInput + _FilterOffsets.CL + 1) + *(currentInput + _FilterOffsets.CL + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.CR) + *(currentInput + _FilterOffsets.CR + 1) + *(currentInput + _FilterOffsets.CR + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.BL) + *(currentInput + _FilterOffsets.BL + 1) + *(currentInput + _FilterOffsets.BL + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.BC) + *(currentInput + _FilterOffsets.BC + 1) + *(currentInput + _FilterOffsets.BC + 2);
				_TotalEffectiveValue -= *(currentInput + _FilterOffsets.BR) + *(currentInput + _FilterOffsets.BR + 1) + *(currentInput + _FilterOffsets.BR + 2);

				if (_TotalEffectiveValue >= _Threshold) {
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

		try {
			output.CopyFromBuffer(_OutputData.AsBuffer());
		}
		catch (UnauthorizedAccessException) { }
	}
}
