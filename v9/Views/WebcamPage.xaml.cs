using Microsoft.UI.Xaml.Navigation;
using v9.Core.ViewModels;

namespace v9.Views;

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
		if (sender is ListBox listBox && listBox.SelectedItem is WebcamPageViewModel.Selection selection) {
			ProcessorFrame.Navigate(typeof(ProcessedWebcamFrame), selection);
		}
	}
}
