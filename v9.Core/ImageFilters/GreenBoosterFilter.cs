using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

public class GreenBoosterFilter : ImageFilterBase {
	public override unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		input.CopyToBuffer(_OutputData.AsBuffer());

		fixed (byte* _inputBytePtr = _InputData)
		fixed (byte* _outputBytePtr = _OutputData) {
			byte* currentInput = _inputBytePtr;
			byte* currentOutput = _outputBytePtr;

			// Boost the green channel, leave the other two untouched
			*currentOutput = *currentInput;
			*(currentOutput + 1) = (byte) Math.Min(*(currentInput + 1) + 80, 255);
			*(currentOutput + 2) = *(currentInput + 2);

			currentInput += CHUNK;
			currentOutput += CHUNK;
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}
}
