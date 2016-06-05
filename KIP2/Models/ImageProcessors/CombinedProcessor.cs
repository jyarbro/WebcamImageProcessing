namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Combines depth and brightness focusing by getting the brightest point near the nearest point
	/// </summary>
	public class CombinedProcessor : ImageProcessorBase {
		public override byte[] ProcessImage() {
			PrepareOutput();

			FocalPoint = GetNearestFocalPoint(ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(1);

			FocalPoint = GetBrightestFocalPoint(FocalPoint);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(3);

			return OutputArray;
		}
	}
}
