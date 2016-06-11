using System;

namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Identifies the brightest point near center
	/// </summary>
	public class BrightnessFocusProcessor : ImageProcessorBase {
		public override byte[] ProcessImage() {
			FocalPoint = GetBrightestFocalPoint(Window, ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			PrepareOutput();

			OverlaySampleGrid();
			OverlayFocalPoint(3);

			return OutputArray;
		}

		/// <summary>
		/// Applies a brightness filter based on the focal point brightness.
		/// </summary>
		public override void PrepareOutput() {
			var focalPointColor = ColorSensorData[FocalPointOffset] + ColorSensorData[FocalPointOffset + 1] + ColorSensorData[FocalPointOffset + 2];

			// This threshold favors bright areas over dark areas
			// TODO - Need to test top end filtering as well.
			var threshold = focalPointColor / 4;

			for (int i = 0; i < ColorSensorData.Length; i += 4) {
				var combined = ColorSensorData[i] + ColorSensorData[i + 1] + ColorSensorData[i + 2];
				var color = Convert.ToByte(combined / 3);

				if (color >= threshold) {
					OutputArray[i] = color;
					OutputArray[i + 1] = color;
					OutputArray[i + 2] = color;
				}
				else {
					OutputArray[i] = 0;
					OutputArray[i + 1] = 0;
					OutputArray[i + 2] = 0;
				}
			}
		}
	}
}