using System;

namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Applies a visual layer to depth sensor data
	/// </summary>
	public class DepthFocusProcessor : ImageProcessorBase {
		public override byte[] ProcessImage() {
			PrepareOutput();

			FocalPoint = GetNearestFocalPoint(Window, ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(3, FocalPointOffset);

			return OutputArray;
		}

		/// <summary>
		/// Colors depth data with 0 - 255 values. If values go beyond 255 values, then pattern repeats starting at 0.
		/// </summary>
		public override void PrepareOutput() {
			for (int i = 0; i < ImageDepthData.Length; i++) {
				var depth = ImageDepthData[i];
				byte color = 0;

				if (depth > 0)
					color = Convert.ToByte(depth % 255);

				OutputArray[i * 4] = color;
				OutputArray[i * 4 + 1] = color;
				OutputArray[i * 4 + 2] = color;
			}
		}
	}
}