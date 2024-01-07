using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Data;
using v8.Core.ImageProcessors;
using v8.Views;

namespace v8.ViewModels;

public class WebcamPageViewModel : ObservableRecipient {
	public List<Selection> Processors { get; set; } = new List<Selection> {
			new Selection {
				Title = "Color Camera",
				Processor = typeof(ColorCameraProcessor)
			},
			new Selection {
				Title = "Boost Green",
				Processor = typeof(BoostGreenProcessor)
			},
			new Selection {
				Title = "Edge Detection",
				Processor = typeof(EdgeDetectionProcessor)
			},
			new Selection {
				Title = "TEST TEST TEST",
				Processor = typeof(WebcamProcessor)
			}
		};

	public class Selection {
		public string? Title;
		public Type? Processor { get; set; }
	}
}

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