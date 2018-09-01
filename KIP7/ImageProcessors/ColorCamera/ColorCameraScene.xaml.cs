using KIP7.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7.ImageProcessors.ColorCamera {
	public sealed partial class ColorCameraScene : Page {
		const int FRAMERATE_DELAY = 50;

		readonly SimpleLogger Logger;
		readonly ColorCameraProcessor ColorCameraProcessor;

		bool AcquiringFrame;

		MediaCapture MediaCapture;
		List<MediaFrameReader> SourceReaders;
		Stopwatch InputFrameTimer;

		double _InputFrameCount;
		double _InputFrameDuration;
		double _InputTotalSeconds;
		DateTime _InputFrameRunTimer;
		DateTime _InputFrameTimer;
		DateTime _InputFrameNow;
		string FramesPerSecondText;
		string FrameLagText;

		public ColorCameraScene() {
			InitializeComponent();

			SourceReaders = new List<MediaFrameReader>();
			InputFrameTimer = new Stopwatch();

			Logger = new SimpleLogger(Log);
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

			_InputFrameRunTimer = DateTime.Now;
			_InputFrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);

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
			Logger.Log($"Shutting down scene {nameof(ColorCameraScene)}");
			InputFrameTimer.Stop();
			await CleanupMediaCaptureAsync();
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
				reader.FrameArrived -= FrameArrived;
				await reader.StopAsync();
				reader.Dispose();
				Logger.Log($"Disposed of MediaFrameReader.");
			}

			SourceReaders.Clear();
			MediaCapture.Dispose();
		}

		void UpdateFrameRateStatus() {
			_InputFrameCount++;
			_InputFrameNow = DateTime.Now;

			if (_InputFrameTimer < _InputFrameNow) {
				_InputFrameTimer = _InputFrameNow.AddMilliseconds(FRAMERATE_DELAY);
				_InputTotalSeconds = (_InputFrameNow - _InputFrameRunTimer).TotalSeconds;

				var framesPerSecondText = Math.Round(_InputFrameCount / _InputTotalSeconds).ToString();
				var frameLagText = Math.Round(_InputFrameDuration / _InputFrameCount, 2).ToString();

				Interlocked.Exchange(ref FramesPerSecondText, framesPerSecondText);
				Interlocked.Exchange(ref FrameLagText, frameLagText);

				var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
					FramesPerSecond.Text = FramesPerSecondText;
					FrameLag.Text = FrameLagText;
				});

				if (_InputTotalSeconds > 5) {
					_InputFrameCount = 0;
					_InputFrameDuration = 0;
					_InputFrameRunTimer = DateTime.Now;
				}
			}
		}

		void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
			if (AcquiringFrame)
				return;

			AcquiringFrame = true;

			InputFrameTimer.Restart();

			using (var frame = sender.TryAcquireLatestFrame()) {
				ColorCameraProcessor.ProcessFrame(frame);
			}

			_InputFrameDuration += InputFrameTimer.ElapsedMilliseconds;
			InputFrameTimer.Stop();

			UpdateFrameRateStatus();

			AcquiringFrame = false;
		}
	}
}
