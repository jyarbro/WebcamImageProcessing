using KIP6.ImageProcessors;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Windows;

namespace KIP6 {
	public partial class MainWindow : Window {
		public ColorFrameReader ColorFrameReader;

		public MainWindow() {
			InitializeComponent();

			var sensor = KinectSensor.GetDefault();
			ColorFrameReader = sensor.ColorFrameSource.OpenReader();

			var cameraColor = new CameraColor();
			cameraColor.Initialize(sensor, ColorFrameReader);

			//var cameraMonochrome = new CameraMonochrome();
			//cameraMonochrome.Initialize(sensor, ColorFrameReader);

			var laplaceFilter = new LaplacianEdgeFilter();
			laplaceFilter.Initialize(sensor, ColorFrameReader);

			var imageProcessors = new List<ImageProcessor> {
				cameraColor,
				//cameraMonochrome,
				laplaceFilter
			};

			ImageProcessors.ItemsSource = imageProcessors;

			sensor.Open();
		}
	}
}
