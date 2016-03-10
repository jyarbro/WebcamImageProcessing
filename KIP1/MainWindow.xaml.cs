using System;
using System.ComponentModel;
using System.Windows;

namespace KinectImageProcessing {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindowViewModel ViewModel { get; set; }

		public MainWindow() {
			InitializeComponent();

			ViewModel = new MainWindowViewModel();
			DataContext = ViewModel;
		}

		void WindowLoaded(object sender, RoutedEventArgs e) {
			try {
				ViewModel.Load();
			}
			catch (Exception exception) {
				ShowExceptionMessage(exception);
			}
		}

		void WindowClosing(object sender, CancelEventArgs e) {
			if (ViewModel.Sensor != null) {
				ViewModel.Sensor.Stop();
				ViewModel.Sensor.Dispose();
			}
		}

		/// <summary>
		/// Unwraps exception to inner most message and updates error label
		/// </summary>
		void ShowExceptionMessage(Exception exception) {
			while (exception.InnerException != null)
				exception = exception.InnerException;

			ViewModel.StatusText = exception.Message;
		}
	}
}
