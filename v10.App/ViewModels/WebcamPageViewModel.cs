using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Nrrdio.Utilities.WinUI.FrameRate;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using v10.Services;
using v10.ImageFilters.Helpers;
using v10.Contracts;

namespace v10.ViewModels;

public class WebcamPageViewModel : ObservableRecipient {
	readonly ILogger Logger;
	readonly IFrameRateHandler FrameRateHandler;
	readonly WebcamProcessor WebcamProcessor;
	readonly IServiceProvider ServiceProvider;

	public SoftwareBitmapSource ImageSource { get; } = new();

	public List<Selection> Filters { get; set; } = [
		new() {
			Title = "None",
			Processor = null
		}
	];

	// Start with this nullable so we can initialize it only once later.
	MediaCapture? MediaCapture { get; set; }

	public WebcamPageViewModel(
		ILogger<WebcamPageViewModel> logger,
		IFrameRateHandler frameRateHandler,
		WebcamProcessor webcamProcessor,
		IServiceProvider serviceProvider
	) {
		Logger = logger;
		FrameRateHandler = frameRateHandler;
		WebcamProcessor = webcamProcessor;
		ServiceProvider = serviceProvider;

		foreach (var filter in ImageFilterLoader.GetList()) {
			var filterName = filter.GetCustomAttributes(typeof(DisplayNameAttribute), false).Cast<DisplayNameAttribute>().SingleOrDefault()?.DisplayName ?? "No name";

			Filters.Add(new Selection {
				Title = filterName,
				Processor = filter
			});
		}
	}

	public async Task Initialize(EventHandler<FrameRateEventArgs> updateFrameRateHandler) {
		FrameRateHandler.FrameRateUpdated += updateFrameRateHandler;

		await InitializeMediaCapture();
		await WebcamProcessor.InitializeAsync(ImageSource, MediaCapture!);
	}

	public void Uninitialize(EventHandler<FrameRateEventArgs> updateFrameRateHandler) {
		FrameRateHandler.FrameRateUpdated -= updateFrameRateHandler;
	}

	public void SetFilter(Type? filterType) {
		if (filterType is null) {
			WebcamProcessor.ImageFilter = null;
		}
		else if (filterType.GetInterface(nameof(IImageFilter)) is not null) {
			// Service locator is a necessary evil to dynamically load the filters.
			WebcamProcessor.ImageFilter = ServiceProvider.GetService(filterType) as IImageFilter;
		}
		else {
			throw new ArgumentException($"{nameof(filterType)} must be of type {nameof(IImageFilter)}", nameof(filterType));
		}
	}

	async Task InitializeMediaCapture() {
		if (MediaCapture is not null) {
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

	public class Selection {
		public string Title { get; set; } = "";
		public Type? Processor { get; set; }
	}
}
