namespace KIP2.Models.ImageProcessors {
	/// <summary>
	/// Combines depth and brightness focusing by getting the brightest point near the nearest point
	/// </summary>
	public class CombinedProcessor : ImageProcessorBase {
		int _nearestFocalPointOffset;

		public override byte[] ProcessImage() {
			PrepareOutput();

			OverlaySampleGrid();

			FocalPoint = GetNearestFocalPoint(Window, ImageMid);
			_nearestFocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			FocalPoint = GetBrightestFocalPoint(AreaBoundBox, FocalPoint);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;
			
			FilterEdges(FocalPointOffset);

			OverlayFocalPoint(1, _nearestFocalPointOffset);
			OverlayFocalPoint(3, FocalPointOffset);

			return OutputArray;
		}
	}
}
