using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace v10.Helpers;

public static class FrameReaderLoader {
	public static async Task<MediaFrameReader?> GetFrameReaderAsync(MediaCapture mediaCapture, MediaFrameSourceKind kind) {
		var sources = mediaCapture.FrameSources.Values.Where(mfs => mfs.Info.SourceKind == kind);

		MediaFrameReader? frameReader = null;

		foreach (var source in sources) {
			string? requestedSubtype = null;

			foreach (var format in source.SupportedFormats) {
				requestedSubtype = GetSubtypeForFrameReader(kind, format);

				if (requestedSubtype is not null) {
					await source.SetFormatAsync(format);
					break;
				}
			}

			if (requestedSubtype is null) {
				continue;
			}

			frameReader = await mediaCapture.CreateFrameReaderAsync(source, requestedSubtype);
		}

		return frameReader;
	}

	/// <summary>
	/// Determines the subtype to request from the MediaFrameReader that will result in
	/// a frame that can be rendered by ConvertToDisplayableImage.
	/// </summary>
	/// <returns>Subtype string to request, or null if subtype is not renderable.</returns>
	static string? GetSubtypeForFrameReader(MediaFrameSourceKind kind, MediaFrameFormat format) {
		// Note that media encoding subtypes may differ in case.
		// https://docs.microsoft.com/en-us/uwp/api/Windows.Media.MediaProperties.MediaEncodingSubtypes
		var subtype = format.Subtype;

		switch (kind) {
			// For color sources, we accept anything and request that it be converted to Bgra8.
			case MediaFrameSourceKind.Color:
				return MediaEncodingSubtypes.Bgra8;

			// The only depth format we can render is D16.
			case MediaFrameSourceKind.Depth:
				return string.Equals(subtype, MediaEncodingSubtypes.D16, StringComparison.OrdinalIgnoreCase) ? subtype : null;

			// The only infrared formats we can render are L8 and L16.
			case MediaFrameSourceKind.Infrared:
				return string.Equals(subtype, MediaEncodingSubtypes.L8, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(subtype, MediaEncodingSubtypes.L16, StringComparison.OrdinalIgnoreCase) ? subtype : null;

			// No other source kinds are supported by this class.
			default:
				return null;
		}
	}
}
