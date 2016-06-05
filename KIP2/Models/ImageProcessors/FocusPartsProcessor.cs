namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Slices up the focus region into parts and processes each part separately
	/// </summary>
	public class FocusPartsProcessor : ImageProcessorBase {
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