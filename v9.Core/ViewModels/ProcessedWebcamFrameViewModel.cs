using Microsoft.Extensions.Logging;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.ImageProcessors;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v9.Core.ViewModels;

public class ProcessedWebcamFrameViewModel {
	ILogger Logger { get; init; }
	IFrameRateHandler FrameRateHandler;

	ImageProcessor? ImageProcessor;
	MediaCapture? MediaCapture;

	public ProcessedWebcamFrameViewModel(
		ILogger<ProcessedWebcamFrameViewModel> logger,
		IFrameRateHandler frameRateHandler
	) {
		Logger = logger;
		FrameRateHandler = frameRateHandler;
	}

	public void Initialize(ImageProcessor imageProcessor) {
		_ = InitializeAsync(imageProcessor);
	}

	public async Task InitializeAsync(ImageProcessor imageProcessor) {
		ImageProcessor = imageProcessor;

		try {
			await InitializeMediaCaptureAsync();
		}
		catch (Exception exception) {
			Logger.LogCritical($"{nameof(MediaCapture)} initialization error: {exception.Message}");
			await ImageProcessor.DisposeAsync();
			return;
		}

		await ImageProcessor.InitializeAsync(MediaCapture);
	}

	public void Shutdown() {
		Logger.LogTrace($"Shutting down view model.");
		_ = ImageProcessor?.DisposeAsync();
	}

	async Task InitializeMediaCaptureAsync() {
		if (MediaCapture != null) {
			return;
		}

		var sourceGroups = await MediaFrameSourceGroup.FindAllAsync();

		var settings = new MediaCaptureInitializationSettings {
			SourceGroup = sourceGroups[0],
			SharingMode = MediaCaptureSharingMode.SharedReadOnly,   // This media capture can share streaming with other apps.
			StreamingCaptureMode = StreamingCaptureMode.Video,      // Only stream video and don't initialize audio capture devices.
			MemoryPreference = MediaCaptureMemoryPreference.Cpu     // Set to CPU to ensure frames always contain CPU SoftwareBitmap images instead of preferring GPU D3DSurface images.
		};

		MediaCapture = new MediaCapture();
		await MediaCapture.InitializeAsync(settings);

		Logger.LogTrace($"Successfully initialized MediaCapture in shared mode using MediaFrameSourceGroup {sourceGroups[0].DisplayName}.");
	}
}
