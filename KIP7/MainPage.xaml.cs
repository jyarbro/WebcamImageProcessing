using KIP7.ImageProcessors;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace KIP7 {
	public sealed partial class MainPage : Page {
		/// <summary>
		/// Used by converters to get a handle to the current MainPage instance.
		/// </summary>
		public static MainPage Current;

		public List<ImageProcessorSelector> ImageProcessors { get; set; } = new List<ImageProcessorSelector> {
			new ImageProcessorSelector {
				Title = "Color Camera",
				ImageProcessor = typeof(ColorCameraProcessor)
			},
			new ImageProcessorSelector {
				Title = "Boost Green",
				ImageProcessor = typeof(BoostGreenProcessor)
			},
			new ImageProcessorSelector {
				Title = "Edge Detection",
				ImageProcessor = typeof(EdgeDetectionProcessor)
			}
		};

		public MainPage() {
			Current = this;

			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			ImageProcessorSelectorControl.ItemsSource = ImageProcessors;
			ImageProcessorSelectorControl.SelectedIndex = 0;
		}

		void ImageProcessorSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (sender is ListBox listBox && listBox.SelectedItem is ImageProcessorSelector selector) {
				ImageProcessorFrame.Navigate(typeof(ImageScene), selector);
			}
		}
	}
}
