using KIP7.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7.ImageProcessors.ColorCamera {
	public sealed partial class ColorCameraScene : Page {
		const int FRAMERATE_DELAY = 50;

		readonly SimpleLogger Logger;
		readonly ColorCameraProcessor ColorCameraProcessor;

		MediaCapture MediaCapture;
		List<MediaFrameReader> SourceReaders = new List<MediaFrameReader>();

		public ColorCameraScene() {
			InitializeComponent();

            Logger = new SimpleLogger(OutputTextBlock);
			ColorCameraProcessor = new ColorCameraProcessor(OutputImage);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e) {
			try {
				await InitializeMediaCaptureAsync();
			}
			catch (Exception exception) {
				Logger.Log($"{nameof(MediaCapture)} initialization error: {exception.Message}");
				await CleanupMediaCaptureAsync();
				return;
			}

			var frameReader = await FrameReaderLoader.GetFrameReaderAsync(MediaCapture, MediaFrameSourceKind.Color);

			frameReader.FrameArrived += FrameReader_FrameArrived;
			SourceReaders.Add(frameReader);

			var status = await frameReader.StartAsync();

			if (status == MediaFrameReaderStartStatus.Success)
				Logger.Log($"Started MediaFrameReader.");
			else
				Logger.Log($"Unable to start MediaFrameReader. Error: {status}");
		}

		async Task InitializeMediaCaptureAsync() {
			if (MediaCapture != null)
				return;

			var sourceGroups = await MediaFrameSourceGroup.FindAllAsync();

			var settings = new MediaCaptureInitializationSettings {
				SourceGroup = sourceGroups[0],
				SharingMode = MediaCaptureSharingMode.SharedReadOnly,	// This media capture can share streaming with other apps.
				StreamingCaptureMode = StreamingCaptureMode.Video,		// Only stream video and don't initialize audio capture devices.
				MemoryPreference = MediaCaptureMemoryPreference.Cpu		// Set to CPU to ensure frames always contain CPU SoftwareBitmap images instead of preferring GPU D3DSurface images.
			};

			MediaCapture = new MediaCapture();
			await MediaCapture.InitializeAsync(settings);

			Logger.Log($"Successfully initialized MediaCapture in shared mode using MediaFrameSourceGroup {sourceGroups[0].DisplayName}.");
		}

		async Task CleanupMediaCaptureAsync() {
			if (MediaCapture is null)
				return;

			foreach (var reader in SourceReaders.Where(r => r != null)) {
				reader.FrameArrived -= FrameReader_FrameArrived;
				await reader.StopAsync();
				reader.Dispose();
			}

			SourceReaders.Clear();
			MediaCapture.Dispose();
		}

		void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
			using (var frame = sender.TryAcquireLatestFrame()) {
				// Add NRE catch instead of conditional
				ColorCameraProcessor.ProcessFrame(frame);
			}
		}
	}
}
