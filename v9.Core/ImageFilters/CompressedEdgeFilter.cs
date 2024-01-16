using System.ComponentModel;
using v9.Core.Contracts;
using Windows.Graphics.Imaging;

namespace v9.Core.ImageFilters;

[DisplayName("Compressed Edge Filter")]
public class CompressedEdgeFilter : ImageFilterBase, IImageFilter {
	public void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		throw new NotImplementedException();
	}

	public void Initialize() {
		throw new NotImplementedException();
	}
}
