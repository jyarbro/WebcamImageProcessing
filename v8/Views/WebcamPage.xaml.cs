using Microsoft.UI.Xaml.Navigation;
using v8.Helpers;
using v8.ViewModels;

namespace v8.Views;

public sealed partial class MainPage : Page {
	/// <summary>
	/// Used by converters to get a handle to the current MainPage instance.
	/// </summary>
	public static MainPage? Current { get; private set; }

	public MainViewModel ViewModel { get; private init; }

	public MainPage() {
		Current = this;

		ViewModel = App.GetService<MainViewModel>();
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		WebcamProcessorSelectorControl.ItemsSource = ViewModel.ImageProcessors;
		WebcamProcessorSelectorControl.SelectedIndex = 0;
	}

	void WebcamProcessorSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (sender is ListBox listBox && listBox.SelectedItem is ImageProcessorSelector selector) {
			ImageProcessorFrame.Navigate(typeof(ImageScene), selector);
		}
	}
}
