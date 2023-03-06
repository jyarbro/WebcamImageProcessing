using Microsoft.UI.Xaml.Data;
using v8.Views;

namespace v8.Helpers;

public class ImageProcessorConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not ImageProcessorSelector imageProcessor) {
			return "Invalid ImageProcessor";
		}

		if (!MainPage.Current.ViewModel.ImageProcessors.Any()) {
			return "Invalid State";
		}

		return MainPage.Current.ViewModel.ImageProcessors.IndexOf(imageProcessor) + 1 + ") " + imageProcessor.Title;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => true;
}