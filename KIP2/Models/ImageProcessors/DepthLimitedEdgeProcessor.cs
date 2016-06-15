using System.Collections.Generic;

namespace KIP2.Models.ImageProcessors {
	public class DepthLimitedEdgeProcessor : ImageProcessorBase {
		public override byte[] ProcessImage() {
			PrepareOutput();

			FocalPoint = GetNearestFocalPoint(Window, ImageMid);
			var nearestFocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X);
			var nearestFocalPointDepth = ImageDepthData[nearestFocalPointOffset];

			var nearbyPixels = new List<int>();

			for (int i = 0; i < ImageDepthData.Length; i++) {
				var value = ImageDepthData[i] - nearestFocalPointDepth;

				if (value > 0 && value < 200)
					nearbyPixels.Add(i);
			}

			foreach (var pixel in nearbyPixels) {
				var pixelOffset = pixel * 4;

				OutputArray[pixelOffset] = 0;
				OutputArray[pixelOffset + 1] = 0;
				OutputArray[pixelOffset + 2] = 0;
			}

			return OutputArray;
		}
	}
}
