using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using v10.ImageFilters.Contracts;
using Windows.Graphics.Imaging;

namespace v10.ImageFilters.ImageFilters;

[DisplayName("Boost Green")]
public class GreenBoosterFilter : ImageFilterBase, IImageFilter {
	int _i;

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		input.CopyToBuffer(_InputData.AsBuffer());
		output.CopyToBuffer(_OutputData.AsBuffer());

		fixed (byte* _InputDataPtr = _InputData)
		fixed (byte* _OutputDataPtr = _OutputData) {
			byte* inputData = _InputDataPtr;
			byte* outputData = _OutputDataPtr;

			for (_i = 0; _i < PIXELS; _i++) {
				// Boost the green channel, leave the others untouched
				*(outputData + 1) = (byte) Math.Min(*(inputData + 1) + 80, 255);

				inputData += CHUNK;
				outputData += CHUNK;
			}
		}

		output.CopyFromBuffer(_OutputData.AsBuffer());
	}
}
