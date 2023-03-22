using Microsoft.UI.Xaml.Navigation;
using v8.Core.ImageProcessors;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using v8.Helpers;
using v8.ViewModels;

namespace v8.Views;

public sealed partial class ProcessedWebcamFrame : Page {
	public ImageSceneViewModel ViewModel { get; }
	ILogger Logger { get; }
	FrameRateManager FrameRateManager { get; }

	public ProcessedWebcamFrame() {
		InitializeComponent();

		Logger = new SimpleLogger();
		Logger.MessageLoggedEvent += UpdateLog;

		FrameRateManager = new FrameRateManager();
		FrameRateManager.FrameRateUpdated += UpdateFrameRate;

		ViewModel = new ImageSceneViewModel(Logger, FrameRateManager);
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		var imageProcessorSelector = e.Parameter as WebcamProcessorSelector;

		if (imageProcessorSelector is null) {
			Logger.Log($"Error with scene parameter {nameof(WebcamProcessorSelector)}");
			return;
		}

		if (imageProcessorSelector.Processor is null) {
			Logger.Log($"Error with scene parameter {nameof(WebcamProcessorSelector.Processor)}");
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

	void UpdateLog(object sender, LogEventArgs e) {
		DispatcherQueue.TryEnqueue(() => {
			Log.Text = e.Message + Log.Text;
		});
	}

	void UpdateFrameRate(object sender, FrameRateEventArgs e) {
		DispatcherQueue.TryEnqueue(() => {
			FramesPerSecond.Text = e.FramesPerSecond.ToString();
			FrameLag.Text = e.FrameLag.ToString();
		});
	}
}
