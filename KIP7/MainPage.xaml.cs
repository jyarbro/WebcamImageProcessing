using KIP7.Helpers;
using KIP7.ImageProcessors;
using KIP7.ImageProcessors.ColorCamera;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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
				Type = typeof(ColorCameraScene)
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

		/// <summary>
		/// Called whenever the user selects a new ImageProcessor.
		/// </summary>
		void ImageProcessorSelectorControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			// Clear the status block.
			NotifyUser(string.Empty, ENotifyType.StatusMessage);

			var scenarioListBox = sender as ListBox;

			if (scenarioListBox.SelectedItem is ImageProcessorSelector selector) {
				ImageProcessorFrame.Navigate(selector.Type);
			}
		}

		/// <summary>
		/// Display a message to the user from any thread.
		/// </summary>
		void NotifyUser(string strMessage, ENotifyType type) {
			// If called from the UI thread, then update immediately.
			if (Dispatcher.HasThreadAccess) {
				UpdateStatus(strMessage, type);
			}
			// Otherwise, schedule a task on the UI thread to perform the update.
			else {
				var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
			}
		}

		void UpdateStatus(string strMessage, ENotifyType type) {
			switch (type) {
				case ENotifyType.StatusMessage:
					StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
					break;

				case ENotifyType.ErrorMessage:
					StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
					break;
			}

			StatusBlock.Text = strMessage;

			// Collapse the StatusBlock if it has no text to conserve real estate.
			StatusBorder.Visibility = (StatusBlock.Text != string.Empty) ? Visibility.Visible : Visibility.Collapsed;
			if (StatusBlock.Text != string.Empty) {
				StatusBorder.Visibility = Visibility.Visible;
				StatusPanel.Visibility = Visibility.Visible;
			}
			else {
				StatusBorder.Visibility = Visibility.Collapsed;
				StatusPanel.Visibility = Visibility.Collapsed;
			}
		}
	}
}
