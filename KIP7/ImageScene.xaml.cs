using KIP7.FrameRate;
using KIP7.ImageProcessors;
using KIP7.Logger;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7 {
	public sealed partial class ImageScene : Page {
		public ImageSceneViewModel ViewModel { get; }
		ILogger Logger { get; }
		FrameRateManager FrameRateManager { get; }

		public ImageScene() {
			InitializeComponent();

			Logger = new SimpleLogger();
			Logger.MessageLoggedEvent += UpdateLog;

			FrameRateManager = new FrameRateManager();
			FrameRateManager.FrameRateUpdated += UpdateFrameRate;

			ViewModel = new ImageSceneViewModel(Logger, FrameRateManager);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			var imageProcessorSelector = e.Parameter as ImageProcessorSelector;

			if (imageProcessorSelector is null)
				Logger.Log($"Error with scene parameter {nameof(ImageProcessorSelector)}");

			Logger.Log($"Loading scene '{imageProcessorSelector.Title}'");

			var imageProcessor = Activator.CreateInstance(imageProcessorSelector.ImageProcessor, new object[] { Logger, FrameRateManager, OutputImage.Dispatcher }) as ImageProcessor;
			OutputImage.Source = imageProcessor.ImageSource;

			if (imageProcessor is null)
				Logger.Log($"Error creating instance of {imageProcessorSelector.ImageProcessor.FullName} as {nameof(ImageProcessor)}");

			ViewModel.Initialize(imageProcessor);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e) => ViewModel.Shutdown();

		void UpdateLog(object sender, LogEventArgs e) {
			var task = Log.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
				Log.Text = e.Message + Log.Text;
			});
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs e) {
			var task = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
				FramesPerSecond.Text = e.FramesPerSecond.ToString();
				FrameLag.Text = e.FrameLag.ToString();
			});
		}
	}
}
