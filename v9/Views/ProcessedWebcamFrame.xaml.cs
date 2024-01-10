using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.ImageProcessors;
using v9.Core.ViewModels;

namespace v9.Views;

public sealed partial class ProcessedWebcamFrame : Page {
	public ProcessedWebcamFrameViewModel ViewModel { get; }

	ILogger Logger { get; }
	IFrameRateHandler FrameRateHandler { get; }

	public ProcessedWebcamFrame() {
		Logger = App.GetService<ILogger<ProcessedWebcamFrame>>();
		FrameRateHandler = App.GetService<IFrameRateHandler>();
		ViewModel = App.GetService<ProcessedWebcamFrameViewModel>();

		InitializeComponent();

		HandlerLoggerProvider.Current!.GetLogger(typeof(ProcessedWebcamFrame).FullName!).EntryAddedEvent += ProcessedWebcamFrame_EntryAddedEvent;

		FrameRateHandler.FrameRateUpdated += UpdateFrameRate;
	}

	private void ProcessedWebcamFrame_EntryAddedEvent(object? sender, LogEntryEventArgs e) {
		throw new NotImplementedException();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e) {
		var imageProcessorSelector = e.Parameter as WebcamPageViewModel.Selection;

		if (imageProcessorSelector?.Processor is null) {
			Logger.LogError($"Error with scene parameter {nameof(WebcamPageViewModel.Selection)}");
			return;
		}

		Logger.LogTrace($"Loading scene '{imageProcessorSelector.Title}'");

		var imageProcessor = App.GetService(imageProcessorSelector.Processor) as ImageProcessor;

		if (imageProcessor is null) {
			Logger.LogError($"Error creating instance of {imageProcessorSelector.Processor.FullName} as {nameof(ImageProcessor)}");
			return;
		}

		imageProcessor.DispatcherQueue = DispatcherQueue;
		OutputImage.Source = imageProcessor.ImageSource;

		ViewModel.Initialize(imageProcessor);
	}

	protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.Shutdown();

	void UpdateLog(object? sender, LogEntryEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			Log.Text = e.LogEntry?.Message + Log.Text;
		});
	}

	void UpdateFrameRate(object? sender, FrameRateEventArgs e) {
		DispatcherQueue?.TryEnqueue(() => {
			FramesPerSecond.Text = e.FramesPerSecond.ToString();
			FrameLag.Text = e.FrameLag.ToString();
		});
	}
}
