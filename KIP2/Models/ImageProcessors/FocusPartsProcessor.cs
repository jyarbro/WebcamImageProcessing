using System;
using System.Collections.Generic;

namespace KIP2.Models.ImageProcessors {
	public class FocusPartsProcessor : ImageProcessorBase {
		public FocusPartsProcessor() : base() {
			SampleGap = 10;

			FocusPartWidth = 11;
			FocusRegionWidth = 99;

			if (FocusRegionWidth % FocusPartWidth > 0)
				throw new Exception("Focus area width must be divisible by sample area width");

			FocusPartArea = FocusPartWidth * FocusPartWidth; // 121
			FocusRegionArea = FocusRegionWidth * FocusRegionWidth; // 9801

			FocusPartHorizontalCount = FocusRegionWidth / FocusPartWidth; // 9
			FocusPartTotalCount = FocusPartHorizontalCount * FocusPartHorizontalCount; // 81

			FocusPartOffsets = new List<int[]>();
			FocusParts = new List<byte[]>();
		}

		public override void Prepare() {
			PrepareSampleOffsets();
			PrepareFocusPartOffsets();
		}

		public override byte[] ProcessImage() {
			FocalPoint = GetBrightestFocalPoint(ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			LoadFocusArea();

			PrepareOutput();

			OverlaySampleGrid();
			OverlayFocalPoint(3);

			return OutputArray;
		}

		void LoadFocusArea() {
			for (int i = 0; i < FocusPartOffsets.Count; i++) {
				var subAreaOffsets = FocusPartOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + FocalPointOffset;

					if (effectiveOffset > 0 && effectiveOffset < ByteCount) {
						FocusParts[i][byteCount] = ColorSensorData[effectiveOffset];
						FocusParts[i][byteCount + 1] = ColorSensorData[effectiveOffset + 1];
						FocusParts[i][byteCount + 2] = ColorSensorData[effectiveOffset + 2];
					}

					byteCount += 4;
				}
			}
		}

		public override void PrepareOutput() {
			for (var i = 0; i < ByteCount; i += 4) {
				OutputArray[i] = 0;
				OutputArray[i + 1] = 0;
				OutputArray[i + 2] = 0;
			}

			for (int i = 0; i < FocusPartOffsets.Count; i++) {
				var subAreaOffsets = FocusPartOffsets[i];
				var byteCount = 0;

				foreach (var subAreaOffset in subAreaOffsets) {
					var effectiveOffset = subAreaOffset + FocalPointOffset;

					if (effectiveOffset > 0 && effectiveOffset < ByteCount) {
						OutputArray[effectiveOffset] = FocusParts[i][byteCount];
						OutputArray[effectiveOffset + 1] = FocusParts[i][byteCount + 1];
						OutputArray[effectiveOffset + 2] = FocusParts[i][byteCount + 2];
					}

					byteCount += 4;
				}
			}
		}
	}
}