using System;

namespace KIP2.Models.ImageProcessors {
	public class DepthFocusProcessor : ImageProcessorBase {
		public DepthFocusProcessor() : base() { }

		public override byte[] ProcessImage() {
			PrepareOutput();

			FocalPoint = GetNearestFocalPoint(ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(3);

			return OutputArray;
		}

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