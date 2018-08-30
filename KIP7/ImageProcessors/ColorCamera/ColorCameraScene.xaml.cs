using KIP7.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7.ImageProcessors.ColorCamera {
	public sealed partial class ColorCameraScene : Page {
		readonly SimpleLogger Logger;
		readonly ColorCameraProcessor ColorCameraProcessor;

		int GroupSelectionIndex;
		MediaCapture MediaCapture;
		List<MediaFrameReader> SourceReaders = new List<MediaFrameReader>();

		public ColorCameraScene() {
			InitializeComponent();
            Logger = new SimpleLogger(OutputTextBlock);
			ColorCameraProcessor = new ColorCameraProcessor(OutputImage);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e) {
			await PickNextMediaSourceWorkerAsync();
		}

		/// <summary>
		/// Switches to the next camera source and starts reading frames.
		/// </summary>
		async Task PickNextMediaSourceWorkerAsync() {
			await CleanupMediaCaptureAsync();

			var allGroups = await MediaFrameSourceGroup.FindAllAsync();

			if (allGroups.Count == 0)
				return;

			// Pick next group in the array after each time the Next button is clicked.
			GroupSelectionIndex = (GroupSelectionIndex + 1) % allGroups.Count;
			var selectedGroup = allGroups[GroupSelectionIndex];

			Logger.Log($"Found {allGroups.Count} groups and selecting index [{GroupSelectionIndex}]: {selectedGroup.DisplayName}");

			try {
				// Initialize MediaCapture with selected group.
				// This can raise an exception if the source no longer exists,
				// or if the source could not be initialized.
				await InitializeMediaCaptureAsync(selectedGroup);
			}
			catch (Exception exception) {
				Logger.Log($"MediaCapture initialization error: {exception.Message}");
				await CleanupMediaCaptureAsync();
				return;
			}

			// Set up frame readers, register event handlers and start streaming.
			var startedKinds = new HashSet<MediaFrameSourceKind>();
			foreach (MediaFrameSource source in MediaCapture.FrameSources.Values) {
				MediaFrameSourceKind kind = source.Info.SourceKind;

				// Ignore this source if we already have a source of this kind.
				if (startedKinds.Contains(kind)) {
					continue;
				}

				// Look for a format which the FrameRenderer can render.
				string requestedSubtype = null;
				foreach (MediaFrameFormat format in source.SupportedFormats) {
					requestedSubtype = ColorCameraProcessor.GetSubtypeForFrameReader(kind, format);
					if (requestedSubtype != null) {
						// Tell the source to use the format we can render.
						await source.SetFormatAsync(format);
						break;
					}
				}
				if (requestedSubtype == null) {
					// No acceptable format was found. Ignore this source.
					continue;
				}

				MediaFrameReader frameReader = await MediaCapture.CreateFrameReaderAsync(source, requestedSubtype);

				frameReader.FrameArrived += FrameReader_FrameArrived;
				SourceReaders.Add(frameReader);

				MediaFrameReaderStartStatus status = await frameReader.StartAsync();
				if (status == MediaFrameReaderStartStatus.Success) {
					Logger.Log($"Started {kind} reader.");
					startedKinds.Add(kind);
				}
				else {
					Logger.Log($"Unable to start {kind} reader. Error: {status}");
				}
			}

			if (startedKinds.Count == 0) {
				Logger.Log($"No eligible sources in {selectedGroup.DisplayName}.");
			}
		}

		/// <summary>
		/// Handles a frame arrived event and renders the frame to the screen.
		/// </summary>
		void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args) {
			// TryAcquireLatestFrame will return the latest frame that has not yet been acquired.
			// This can return null if there is no such frame, or if the reader is not in the
			// "Started" state. The latter can occur if a FrameArrived event was in flight
			// when the reader was stopped.
			using (var frame = sender.TryAcquireLatestFrame()) {
				if (frame != null) {
					ColorCameraProcessor.ProcessFrame(frame);
				}
			}
		}

		/// <summary>
		/// Initializes the MediaCapture object with the given source group.
		/// </summary>
		/// <param name="sourceGroup">SourceGroup with which to initialize.</param>
		private async Task InitializeMediaCaptureAsync(MediaFrameSourceGroup sourceGroup) {
			if (MediaCapture != null) {
				return;
			}

			// Initialize mediacapture with the source group.
			MediaCapture = new MediaCapture();
			var settings = new MediaCaptureInitializationSettings {
				SourceGroup = sourceGroup,

				// This media capture can share streaming with other apps.
				SharingMode = MediaCaptureSharingMode.SharedReadOnly,

				// Only stream video and don't initialize audio capture devices.
				StreamingCaptureMode = StreamingCaptureMode.Video,

				// Set to CPU to ensure frames always contain CPU SoftwareBitmap images
				// instead of preferring GPU D3DSurface images.
				MemoryPreference = MediaCaptureMemoryPreference.Cpu
			};

			await MediaCapture.InitializeAsync(settings);
			Logger.Log("MediaCapture is successfully initialized in shared mode.");
		}

		/// <summary>
		/// Unregisters FrameArrived event handlers, stops and disposes frame readers
		/// and disposes the MediaCapture object.
		/// </summary>
		async Task CleanupMediaCaptureAsync() {
			if (MediaCapture != null) {
				using (var mediaCapture = MediaCapture) {
					MediaCapture = null;

					foreach (var reader in SourceReaders) {
						if (reader != null) {
							reader.FrameArrived -= FrameReader_FrameArrived;
							await reader.StopAsync();
							reader.Dispose();
						}
					}
					SourceReaders.Clear();
				}
			}
		}
	}
}
