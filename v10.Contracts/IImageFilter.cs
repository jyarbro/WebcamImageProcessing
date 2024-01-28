using Windows.Graphics.Imaging;

namespace v10.Contracts;

public interface IImageFilter {
	void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output);
}
