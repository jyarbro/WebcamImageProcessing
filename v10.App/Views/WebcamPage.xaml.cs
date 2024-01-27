using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.WinUI.FrameRate;
using v10.ViewModels;

namespace v10.Views;

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

	protected override async void OnNavigatedTo(NavigationEventArgs e) {
		ProcessorSelectorControl.ItemsSource = ViewModel.Filters;
		ProcessorSelectorControl.SelectedIndex = 0;

		OutputImage.Source = ViewModel.ImageSource;

		await ViewModel.Initialize(DispatcherQueue, UpdateFrameRate);
		HandlerLoggerProvider.Current!.RegisterEventHandler(UpdateLog);
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e) {
		ViewModel.Uninitialize(UpdateFrameRate);
		HandlerLoggerProvider.Current!.DeregisterEventHandler(UpdateLog);
	}

	void ProcessorSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (sender is ListBox listBox && listBox.SelectedItem is WebcamPageViewModel.Selection selection) {
			ViewModel.SetFilter(selection.Processor);
		}
	}

	void UpdateLog(object? sender, LogEntryEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			Log.Text = e.LogEntry?.Message + Log.Text;
		});
	}

	void UpdateFrameRate(object? sender, FrameRateEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			try {
				FramesPerSecond.Text = e.FramesPerSecond.ToString();
				FrameLag.Text = e.FrameLag.ToString();
			}
			catch (COMException) { }
		});
	}
}
