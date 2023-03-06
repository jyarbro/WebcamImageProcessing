using v8.Core.ImageProcessors;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace v8.ViewModels;

public class ImageSceneViewModel {
	readonly ILogger Logger;
	readonly IFrameRateManager FrameRateManager;

	ImageProcessor? ImageProcessor;
	MediaCapture? MediaCapture;

	public ImageSceneViewModel(
		ILogger logger,
		IFrameRateManager frameRateManager
	) {
		Logger = logger;
		FrameRateManager = frameRateManager;
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
			Logger.Log($"{nameof(MediaCapture)} initialization error: {exception.Message}");
			await ImageProcessor.DisposeAsync();
			return;
		}

		await ImageProcessor.InitializeAsync(MediaCapture);
	}

	public void Shutdown() {
		Logger.Log($"Shutting down view model.");
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

		Logger.Log($"Successfully initialized MediaCapture in shared mode using MediaFrameSourceGroup {sourceGroups[0].DisplayName}.");
	}
}
