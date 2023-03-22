using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using v8.Helpers;
using v8.ViewModels;

namespace v8.Views;

public sealed partial class WebcamPage : Page {
	/// <summary>
	/// Used by converters to get a handle to the current instance.
	/// </summary>
	public static WebcamPage? Current { get; private set; }

	public WebcamPageViewModel ViewModel { get; private init; }

	public WebcamPage() {
		Current = this;

		ViewModel = App.GetService<WebcamPageViewModel>();
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		ProcessorSelectorControl.ItemsSource = ViewModel.Processors;
		ProcessorSelectorControl.SelectedIndex = 0;
	}

	void ProcessorSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (sender is ListBox listBox && listBox.SelectedItem is WebcamProcessorSelector selector) {
			ProcessorFrame.Navigate(typeof(ProcessedWebcamFrame), selector);
		}
	}
}

public class WebcamProcessorConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not WebcamProcessorSelector processor) {
			return "Invalid processor";
		}

		if (WebcamPage.Current is null || !WebcamPage.Current.ViewModel.Processors.Any()) {
			return "Invalid state";
		}

		return WebcamPage.Current.ViewModel.Processors.IndexOf(processor) + 1 + ") " + processor.Title;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => true;
}