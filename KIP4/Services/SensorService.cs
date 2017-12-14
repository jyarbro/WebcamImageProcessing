using KIP.Helpers;
using Microsoft.Kinect;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP4.Services {
	public class SensorService : Observable, IDisposable {
		const uint FRAMERATE_DELAY = 50;

		public event EventHandler<FrameRateEventArgs> UpdateFrameRate;

		public WriteableBitmap OutputImage { get; } = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);

		public string StatusText {
			get => _StatusText ?? (_StatusText = string.Empty);
			set => SetProperty(ref _StatusText, value);
		}
		string _StatusText;

		public double FrameCount {
			get => _FrameCount;
			set {
				if (_FrameCount == value)
					return;

				_FrameCount = value;

				_FrameNow = DateTime.Now;
				var totalSeconds = (_FrameNow - _FrameRunTimer).TotalSeconds;

				if (_FrameTimer < _FrameNow) {
					_FrameTimer = _FrameNow.AddMilliseconds(FRAMERATE_DELAY);

					UpdateFrameRate?.Invoke(this, new FrameRateEventArgs {
						FramesPerSecond = Math.Round(_FrameCount / totalSeconds),
						FrameLag = Math.Round(_FrameDuration / _FrameCount, 3)
					});
				}

				if (totalSeconds > 5)
					ResetFPS();
			}
		}
		double _FrameCount;
		double _FrameDuration;
		DateTime _FrameRunTimer = DateTime.Now;
		DateTime _FrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);
		DateTime _FrameNow;

		KinectSensor Sensor;
		ColorFrameReader ColorFrameReader;
		FrameDescription FrameDescription;
		Int32Rect FrameChangedRect;

		int FrameWidth;
		int FrameHeight;
		uint PixelCount;
		uint ByteCount;
		byte[] OutputData;

		bool _isDisposed;

		public SensorService(KinectSensor sensor) {
			Sensor = sensor;
			Sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
		}

		public void Dispose() {
			if (_isDisposed)
				return;
			
			ColorFrameReader?.Dispose();
			ColorFrameReader = null;

			Sensor?.Close();
			Sensor = null;

			_isDisposed = true;
		}

		void ResetFPS() {
			_FrameCount = 0;
			_FrameDuration = 0;
			_FrameRunTimer = DateTime.Now;
		}

		void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e) => StatusText = Sensor.IsAvailable ? "Running" : "Sensor not available";

		void ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e) {
			using (var colorFrame = e.FrameReference.AcquireFrame()) {
				if (colorFrame == null)
					return;
					
				FrameDescription = colorFrame.FrameDescription;

				if (FrameDescription.Width != OutputImage.PixelWidth || FrameDescription.Height != OutputImage.PixelHeight)
					return;

				using (var colorBuffer = colorFrame.LockRawImageBuffer()) {
					OutputImage.Lock();
					colorFrame.CopyConvertedFrameDataToIntPtr(OutputImage.BackBuffer, ByteCount, ColorImageFormat.Bgra);
					OutputImage.AddDirtyRect(FrameChangedRect);
					OutputImage.Unlock();
				}
			}

			FrameCount++;
		}

		/// <summary>
		/// SensorService factory
		/// </summary>
		public static SensorService Create() {
			var sensor = KinectSensor.GetDefault();
			var colorFrameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
			var pixels = colorFrameDescription.LengthInPixels;
			var bytes = pixels * 4;

			var reader = sensor.ColorFrameSource.OpenReader();

			var sensorService = new SensorService(sensor) {
				ColorFrameReader = sensor.ColorFrameSource.OpenReader(),
				FrameWidth = colorFrameDescription.Width,
				FrameHeight = colorFrameDescription.Height,
				PixelCount = pixels,
				ByteCount = bytes,
				FrameChangedRect = new Int32Rect(0, 0, colorFrameDescription.Width, colorFrameDescription.Height),
				OutputData = new byte[bytes]
			};

			sensorService.ColorFrameReader.FrameArrived += sensorService.ColorFrameArrived;

			sensor.Open();

			//sensorService.StatusText = sensor.IsAvailable ? "Running" : "Sensor not available";

			return sensorService;
		}
	}
}