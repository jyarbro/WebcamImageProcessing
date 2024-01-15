using v9.Core.Helpers;

namespace v9.Core.ImageFilters;
public abstract class ImageFilterBase {
	protected const int CHUNK = 4;
	protected const int WIDTH = 640;
	protected const int HEIGHT = 480;
	protected const int STRIDE = WIDTH * CHUNK;
	protected const int PIXELS = WIDTH * HEIGHT;
	protected const int SUBPIXELS = PIXELS * CHUNK;

	protected FilterOffsets _FilterOffsets;
	protected byte[] _InputData = new byte[SUBPIXELS];
	protected byte[] _OutputData = new byte[SUBPIXELS];

	protected int _i, _j, _k;
	protected int _TotalEffectiveValue;

	public ImageFilterBase() {
		SetFilterOffsets(1);
	}

	public void SetFilterOffsets(int distance) {
		int offset(int row, int col) => (row * STRIDE) + (col * CHUNK);

		var result = new FilterOffsets {
			TL = offset(-distance, -distance),
			TC = offset(-distance, 0),
			TR = offset(-distance, distance),
			CL = offset(0, -distance),
			CC = offset(0, 0),
			CR = offset(0, distance),
			BL = offset(distance, -distance),
			BC = offset(distance, 0),
			BR = offset(distance, distance),
		};

		result.Min = result.TL * -1;
		result.Max = STRIDE * HEIGHT - result.BR - CHUNK;

		_FilterOffsets = result;
	}
}
