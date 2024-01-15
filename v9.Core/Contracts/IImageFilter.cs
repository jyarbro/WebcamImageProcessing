using Windows.Graphics.Imaging;

namespace v9.Core.Contracts;

public interface IImageFilter {
	void Initialize();
	void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output);
}
