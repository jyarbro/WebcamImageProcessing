using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using v10.Contracts;
using v10.ImageFilters.Helpers;
using Windows.Graphics.Imaging;

namespace v10.ImageFilters.ImageFilters;

[DisplayName("Edge Detection")]
public class EdgeFilter : ImageFilterBase, IImageFilter {
	FilterOffsets _FilterOffsets;

	const int THRESHOLD = 80;

	int _TotalEffectiveValue;
	int _i;

	public EdgeFilter() {
		SetFilterOffsets(1);
	}

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		input.CopyToBuffer(_OutputData.AsBuffer());

		fixed (byte* _InputDataPtr = _InputData)
		fixed (byte* _OutputDataPtr = _OutputData) {
			byte* inputData = _InputDataPtr;
			byte* outputData = _OutputDataPtr;

			inputData += _FilterOffsets.Min;
			outputData += _FilterOffsets.Min;

			_i = _FilterOffsets.Min;

			while (_i < _FilterOffsets.Max) {
				_TotalEffectiveValue = 8 * (*inputData + *(inputData + 1) + *(inputData + 2));

				_TotalEffectiveValue -= *(inputData + _FilterOffsets.TL) + *(inputData + _FilterOffsets.TL + 1) + *(inputData + _FilterOffsets.TL + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.TC) + *(inputData + _FilterOffsets.TC + 1) + *(inputData + _FilterOffsets.TC + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.TR) + *(inputData + _FilterOffsets.TR + 1) + *(inputData + _FilterOffsets.TR + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.CL) + *(inputData + _FilterOffsets.CL + 1) + *(inputData + _FilterOffsets.CL + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.CR) + *(inputData + _FilterOffsets.CR + 1) + *(inputData + _FilterOffsets.CR + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.BL) + *(inputData + _FilterOffsets.BL + 1) + *(inputData + _FilterOffsets.BL + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.BC) + *(inputData + _FilterOffsets.BC + 1) + *(inputData + _FilterOffsets.BC + 2);
				_TotalEffectiveValue -= *(inputData + _FilterOffsets.BR) + *(inputData + _FilterOffsets.BR + 1) + *(inputData + _FilterOffsets.BR + 2);

				if (_TotalEffectiveValue >= THRESHOLD) {
					*outputData = 0;
					*(outputData + 1) = 0;
					*(outputData + 2) = 0;
					*(outputData + 3) = 255;
				}
				else {
					*outputData = 255;
					*(outputData + 1) = 255;
					*(outputData + 2) = 255;
					*(outputData + 3) = 255;
				}

				inputData += CHUNK;
				outputData += CHUNK;
				_i += CHUNK;
			}
		}

		try {
			output.CopyFromBuffer(_OutputData.AsBuffer());
		}
		catch (UnauthorizedAccessException) { }
	}

	void SetFilterOffsets(int distance) {
		int offset(int row, int col) => row * STRIDE + col * CHUNK;

		var result = new FilterOffsets {
			TL = offset(-distance, -distance),
			TC = offset(-distance, 0),
			TR = offset(-distance, distance),
			CL = offset(0, -distance),
			CC = offset(0, 0),
			CR = offset(0, distance),
			BL = offset(distance, -distance),
			BC = offset(distance, 0),
			BR = offset(distance, distance),
		};

		result.Min = result.TL * -1;
		result.Max = STRIDE * HEIGHT - result.BR - CHUNK;

		_FilterOffsets = result;
	}
}
