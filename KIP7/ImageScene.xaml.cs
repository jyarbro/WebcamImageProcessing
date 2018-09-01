using KIP7.Helpers;
using KIP7.ImageProcessors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7 {
	public sealed partial class ImageScene : Page {
		const int FRAMERATE_DELAY = 50;

		readonly SimpleLogger Logger;

		bool AcquiringFrame;

		ImageProcessor ImageProcessor;
		MediaCapture MediaCapture;
		List<MediaFrameReader> SourceReaders;

		double FrameCount;
		double FrameDuration;
		DateTime FrameRunTimer;
		DateTime FrameTimer;

		public ImageScene() {
			InitializeComponent();

			SourceReaders = new List<MediaFrameReader>();
			Logger = new SimpleLogger(Log);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e) {
			var imageProcessorSelector = e.Parameter as ImageProcessorSelector;

			if (imageProcessorSelector is null)
				Logger.Log($"Error with scene parameter {nameof(ImageProcessorSelector)}");

			Logger.Log($"Loading scene '{imageProcessorSelector.Title}'");

			ImageProcessor = Activator.CreateInstance(imageProcessorSelector.ImageProcessor, OutputImage) as ImageProcessor;

			if (ImageProcessor is null)
				Logger.Log($"Error creating instance of {imageProcessorSelector.ImageProcessor.FullName} as {nameof(ImageProcessor)}");

			try {
				await InitializeMediaCaptureAsync();
			}
			catch (Exception exception) {
				Logger.Log($"{nameof(MediaCapture)} initialization error: {exception.Message}");
				await CleanupMediaCaptureAsync();
				return;
			}

			FrameRunTimer = DateTime.Now;
			FrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);

			var frameReader = await FrameReaderLoader.GetFrameReaderAsync(MediaCapture, MediaFrameSourceKind.Color);

			frameReader.FrameArrived += FrameArrived;
			SourceReaders.Add(frameReader);

			var status = await frameReader.StartAsync();

			if (status == MediaFrameReaderStartStatus.Success)
				Logger.Log($"Started MediaFrameReader.");
			else
				Logger.Log($"Unable to start MediaFrameReader. Error: {status}");
		}

		protected override async void OnNavigatedFrom(NavigationEventArgs e) {
			Logger.Log($"Shutting down scene {nameof(ImageScene)}");
			await CleanupMediaCaptureAsync();
		}

		async Task InitializeMediaCaptureAsync() {
			if (MediaCapture != null)
				return;

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

		async Task CleanupMediaCaptureAsync() {
			if (MediaCapture is null)
				return;

			foreach (var reader in SourceReaders.Where(r => r != null)) {
				reader.FrameArrived -= FrameArrived;
				await reader.StopAsync();
				reader.Dispose();
				Logger.Log($"Disposed of MediaFrameReader.");
			}

			SourceReaders.Clear();
			MediaCapture.Dispose();
		}

		void UpdateFrameRate() {
			FrameCount++;

			var now = DateTime.Now;

			if (FrameTimer < now) {
				FrameTimer = now.AddMilliseconds(FRAMERATE_DELAY);

				var totalSeconds = (now - FrameRunTimer).TotalSeconds;

				var framesPerSecondText = Math.Round(FrameCount / totalSeconds).ToString();
				var frameLagText = Math.Round(FrameDuration / FrameCount, 2).ToString();

				var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
					FramesPerSecond.Text = framesPerSecondText;
					FrameLag.Text = frameLagText;
				});

				if (totalSeconds > 5) {
					FrameCount = 0;
					FrameDuration = 0;
					FrameRunTimer = DateTime.Now;
				}
			}
		}

		void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
			if (AcquiringFrame)
				return;

			AcquiringFrame = true;

			var frameStopWatch = Stopwatch.StartNew();

			using (var frame = sender.TryAcquireLatestFrame()) {
				ImageProcessor.ProcessFrame(frame);
			}

			FrameDuration += frameStopWatch.ElapsedMilliseconds;
			frameStopWatch.Stop();

			UpdateFrameRate();

			AcquiringFrame = false;
		}
	}
}
