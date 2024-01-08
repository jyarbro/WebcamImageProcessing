using Microsoft.UI.Xaml.Data;
using v9.Core.ViewModels;
using v9.Views;

namespace v9.Helpers;
public class WebcamSelectionConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not WebcamPageViewModel.Selection selection) {
			return "Invalid selection";
		}

		if (WebcamPage.Current is null || WebcamPage.Current.ViewModel.Processors.Count == 0) {
			return "Invalid state";
		}

		return WebcamPage.Current.ViewModel.Processors.IndexOf(selection) + 1 + ") " + selection.Title;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => true;
}