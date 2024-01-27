using System.ComponentModel;
using v10.ImageFilters.Contracts;
using Windows.Graphics.Imaging;

namespace v10.ImageFilters.ImageFilters;

[DisplayName("Compressed Edge Filter")]
public class CompressedEdgeFilter : ImageFilterBase, IImageFilter {
	public CompressionFilter CompressionFilter { get; set; }
	public EdgeFilter EdgeFilter { get; set; }

	public CompressedEdgeFilter(
		CompressionFilter compressionFilter,
		EdgeFilter edgeFilter
	) {
		CompressionFilter = compressionFilter;
		EdgeFilter = edgeFilter;
	}

	public void Apply(ref SoftwareBitmap input, ref SoftwareBitmap output) {
		EdgeFilter.Apply(ref input, ref output);
		CompressionFilter.Apply(ref input, ref output);
	}
}
