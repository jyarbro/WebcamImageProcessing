using KIP5.ImageProcessors;
using KIP5.Interfaces;
using KIP5.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KIP5 {
	public partial class MainWindow : Window, INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public string StatusText {
			get => _StatusText;
			set => SetProperty(ref _StatusText, value);
		}
		string _StatusText = string.Empty;

		public MainWindow() {
			InitializeComponent();

			var sensorReader = new SensorReader();
			sensorReader.StatusChanged += StatusTextChanged;

			var imageProcessors = new List<IImageProcessor> {
				new CameraRaw(sensorReader),
				new LaplaceEdgeFilter(sensorReader),
				new SobelEdgeFilter(sensorReader)
			};

			ImageProcessors.ItemsSource = imageProcessors;
		}

		void StatusTextChanged(object sender, PropertyChangedEventArgs args) {
			if (sender is IStatusTracker && args.PropertyName == nameof(IStatusTracker.StatusText))
				StatusText = ((IStatusTracker) sender).StatusText;
		}

		void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null) {
			if (Equals(member, val))
				return;

			member = val;

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
