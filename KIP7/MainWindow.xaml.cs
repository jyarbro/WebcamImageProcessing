using KIP7.ImageProcessors;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows;

namespace KIP7 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public ColorFrameReader ColorFrameReader;

		public MainWindow() {
			InitializeComponent();

			var sensor = KinectSensor.GetDefault();
			ColorFrameReader = sensor.ColorFrameSource.OpenReader();

			var cameraColor = new CameraColor();
			cameraColor.Initialize(sensor, ColorFrameReader);

			var contrastFilter = new ContrastFilter();
			contrastFilter.Initialize(sensor, ColorFrameReader);

			var imageProcessors = new List<ImageProcessor> {
				cameraColor,
				contrastFilter
			};

			ImageProcessors.ItemsSource = imageProcessors;

			sensor.Open();
		}
	}
}
