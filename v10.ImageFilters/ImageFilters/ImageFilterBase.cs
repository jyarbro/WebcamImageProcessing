namespace v10.ImageFilters.ImageFilters;

public abstract class ImageFilterBase {
	protected const int CHUNK = 4;
	protected const int WIDTH = 640;
	protected const int HEIGHT = 480;
	protected const int STRIDE = WIDTH * CHUNK;
	protected const int PIXELS = WIDTH * HEIGHT;
	protected const int SUBPIXELS = PIXELS * CHUNK;

	protected byte[] _InputData = new byte[SUBPIXELS];
	protected byte[] _OutputData = new byte[SUBPIXELS];
}
