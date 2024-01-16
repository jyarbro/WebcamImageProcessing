using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

[DisplayName("Boost Green")]
public class GreenBoosterFilter : ImageFilterBase, IImageFilter {
	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		output.CopyToBuffer(_OutputData.AsBuffer());

		fixed (byte* _inputBytePtr = _InputData)
		fixed (byte* _outputBytePtr = _OutputData) {
			byte* currentInput = _inputBytePtr;
			byte* currentOutput = _outputBytePtr;

			_i = 0;

			while (_i < PIXELS) {
				// Boost the green channel, leave the others untouched
				*(currentOutput + 1) = (byte) Math.Min(*(currentInput + 1) + 80, 255);

				currentInput += CHUNK;
				currentOutput += CHUNK;
				_i++;
			}
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}
}
