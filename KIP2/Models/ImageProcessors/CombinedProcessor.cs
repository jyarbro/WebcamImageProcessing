namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Combines depth and brightness focusing by getting the brightest point near the nearest point
	/// </summary>
	public class CombinedProcessor : ImageProcessorBase {
		public Rectangle ImageBoundBox;
		public Rectangle AreaBoundBox;

		public CombinedProcessor() {
			ImageBoundBox = new Rectangle(-ImageMid.X, -ImageMid.Y, ImageMid.X, ImageMid.Y);
			AreaBoundBox = GetCenteredBox(FocusRegionArea);
		}

		public override byte[] ProcessImage() {
			PrepareOutput();

			OverlaySampleGrid();

			FocalPoint = GetNearestFocalPoint(ImageBoundBox, ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(1);

			FocalPoint = GetBrightestFocalPoint(AreaBoundBox, FocalPoint);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(3);

			return OutputArray;
		}
	}
}
