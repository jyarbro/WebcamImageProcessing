using v9.Core.Helpers;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;
public abstract class ImageFilterBase {
	protected const int CHUNK = 4;
	protected const int WIDTH = 640;
	protected const int HEIGHT = 480;
	protected const int STRIDE = WIDTH * CHUNK;
	protected const int PIXELS = WIDTH * HEIGHT * CHUNK;

	public int Threshold {
		set => _Threshold = value;
	}
	protected int _Threshold = 128;

	protected FilterOffsets _FilterOffsets;
	protected readonly byte[] _InputData = new byte[PIXELS];
	protected readonly byte[] _OutputData = new byte[PIXELS];
	protected int _i;
	protected int _TotalEffectiveValue;

	public ImageFilterBase() {
		SetFilterOffsets(1);
	}

	public void SetFilterOffsets(int distance) {
		int offset(int row, int col) => (WIDTH * row + col) * CHUNK;

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
		result.Max = WIDTH * HEIGHT * CHUNK - result.BR - CHUNK;

		_FilterOffsets = result;
	}

	public abstract void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output);
}
