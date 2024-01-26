﻿using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

[DisplayName("Average Filter")]
public class AverageFilter(ILogger<AverageFilter> logger)
	: ImageFilterBase, IImageFilter {

	const byte THRESHOLD = 10;

	readonly ILogger Logger = logger;

	byte[] _TemporalDataLayer1 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer2 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer3 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer4 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer5 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer6 = new byte[SUBPIXELS];
	byte[] _TemporalDataLayer7 = new byte[SUBPIXELS];

	int temporalPixel;

	public unsafe void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		Array.Clear(_OutputData);
		
		input.CopyToBuffer(_InputData.AsBuffer());

		fixed (byte* _TemporalDataLayer1Ptr = _TemporalDataLayer1)
		fixed (byte* _TemporalDataLayer2Ptr = _TemporalDataLayer2)
		fixed (byte* _TemporalDataLayer3Ptr = _TemporalDataLayer3)
		fixed (byte* _TemporalDataLayer4Ptr = _TemporalDataLayer4)
		fixed (byte* _TemporalDataLayer5Ptr = _TemporalDataLayer5)
		fixed (byte* _TemporalDataLayer6Ptr = _TemporalDataLayer6)
		fixed (byte* _TemporalDataLayer7Ptr = _TemporalDataLayer7)
		fixed (byte* _InputDataPtr = _InputData)
		fixed (byte* _OutputDataPtr = _OutputData) {
			byte* temporalDataLayer1 = _TemporalDataLayer1Ptr;
			byte* temporalDataLayer2 = _TemporalDataLayer2Ptr;
			byte* temporalDataLayer3 = _TemporalDataLayer3Ptr;
			byte* temporalDataLayer4 = _TemporalDataLayer4Ptr;
			byte* temporalDataLayer5 = _TemporalDataLayer5Ptr;
			byte* temporalDataLayer6 = _TemporalDataLayer6Ptr;
			byte* temporalDataLayer7 = _TemporalDataLayer7Ptr;
			byte* inputData = _InputDataPtr;
			byte* outputData = _OutputDataPtr;
			_i = 0;

			for (_i = 0; _i < SUBPIXELS; _i++) {
				// find the average subpixel value of the temporal layers
				temporalPixel = 0;
				temporalPixel += *(temporalDataLayer1);
				temporalPixel += *(temporalDataLayer2);
				temporalPixel += *(temporalDataLayer3);
				temporalPixel += *(temporalDataLayer4);
				temporalPixel += *(temporalDataLayer5);
				temporalPixel += *(temporalDataLayer6);
				temporalPixel += *(temporalDataLayer7);
				temporalPixel /= 7;

				// Update the temporal stack
				*(temporalDataLayer1) = *(temporalDataLayer2);
				*(temporalDataLayer2) = *(temporalDataLayer3);
				*(temporalDataLayer3) = *(temporalDataLayer4);
				*(temporalDataLayer4) = *(temporalDataLayer5);
				*(temporalDataLayer5) = *(temporalDataLayer6);
				*(temporalDataLayer6) = *(temporalDataLayer7);
				*(temporalDataLayer7) = *(inputData);

				*(outputData) = Convert.ToByte(temporalPixel);

				temporalDataLayer1++;
				temporalDataLayer2++;
				temporalDataLayer3++;
				temporalDataLayer4++;
				temporalDataLayer5++;
				temporalDataLayer6++;
				temporalDataLayer7++;
				inputData++;
				outputData++;
			}
		}


		output.CopyFromBuffer(_OutputData.AsBuffer());
	}
}
