using KIP7.ImageProcessors;
using System;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace KIP7.Helpers {
	public class ImageProcessorConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, string language) {
			var imageProcessor = value as ImageProcessorSelector;

			if (imageProcessor is null)
				return "Invalid ImageProcessor";

			if (!MainPage.Current.ImageProcessors.Any())
				return "Invalid State";

			return (MainPage.Current.ImageProcessors.IndexOf(imageProcessor) + 1) + ") " + imageProcessor.Title;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => true;
	}
}