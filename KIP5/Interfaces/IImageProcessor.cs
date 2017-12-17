using System.Windows.Media.Imaging;

namespace KIP5.Interfaces {
	interface IImageProcessor {
		WriteableBitmap OutputImage { get; }
	}
}