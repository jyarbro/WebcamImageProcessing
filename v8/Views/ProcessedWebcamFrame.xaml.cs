using Microsoft.UI.Xaml.Navigation;
using v8.Core.Contracts.Services;
using v8.Core.ImageProcessors;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using v8.ViewModels;

namespace v8.Views;

public sealed partial class ProcessedWebcamFrame : Page {
	public ProcessedWebcamFrameViewModel ViewModel { get; }

	ILogger Logger { get; }
	IFrameRateManager FrameRateManager { get; }

	public ProcessedWebcamFrame() {
		Logger = App.GetService<ILogger>();
		FrameRateManager = App.GetService<IFrameRateManager>();
		ViewModel = App.GetService<ProcessedWebcamFrameViewModel>();

		InitializeComponent();

		Logger.MessageLoggedEvent += UpdateLog;
		FrameRateManager.FrameRateUpdated += UpdateFrameRate;
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		var imageProcessorSelector = e.Parameter as WebcamPageViewModel.Selection;

		if (imageProcessorSelector is null) {
			Logger.Log($"Error with scene parameter {nameof(WebcamPageViewModel.Selection)}");
			return;
		}

		if (imageProcessorSelector.Processor is null) {
			Logger.Log($"Error with scene parameter {nameof(WebcamPageViewModel.Selection.Processor)}");
			return;
		}

		Logger.Log($"Loading scene '{imageProcessorSelector.Title}'");

		var imageProcessor = Activator.CreateInstance(imageProcessorSelector.Processor, new object[] { Logger, FrameRateManager, DispatcherQueue }) as ImageProcessor;

		if (imageProcessor is null) {
			Logger.Log($"Error creating instance of {imageProcessorSelector.Processor.FullName} as {nameof(ImageProcessor)}");
			return;
		}

		OutputImage.Source = imageProcessor.ImageSource;

		ViewModel.Initialize(imageProcessor);
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.Shutdown();

	void UpdateLog(object? sender, LogEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			Log.Text = e.Message + Log.Text;
		});
	}

	void UpdateFrameRate(object? sender, FrameRateEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			FramesPerSecond.Text = e.FramesPerSecond.ToString();
			FrameLag.Text = e.FrameLag.ToString();
		});
	}
}
