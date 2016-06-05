namespace KIP2.Models.ImageProcessors {
	public class BrightnessFocusProcessor : ImageProcessorBase {
		int[] _focusRegionOffsets;
		byte[] _focusRegion;

		int _sampleSize;

		public BrightnessFocusProcessor() : base() {
			FocusRegionArea = 99 * 99;

			_sampleSize = 11 * 11;
			SampleGap = 10;
			SampleByteCount = _sampleSize * 4;

			_focusRegion = new byte[FocusRegionArea * 4];
			_focusRegionOffsets = new int[FocusRegionArea];
			SampleOffsets = new int[_sampleSize];

			_focusRegionOffsets = PrepareSquareOffsets(FocusRegionArea, ImageMax.X);
			SampleOffsets = PrepareSquareOffsets(_sampleSize, ImageMax.X);
		}

		public override byte[] ProcessImage() {
			FocalPoint = GetBrightestFocalPoint(ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			PrepareOutput();

			OverlaySampleGrid();
			OverlayFocalPoint(3);

			return OutputArray;
		}
	}
}