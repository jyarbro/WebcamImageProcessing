using KIP.Helpers;
using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace KIP4.Services {
	public class SensorService : Observable, IDisposable {
		const uint FRAMERATE_DELAY = 50;

		public event EventHandler<FrameRateEventArgs> UpdateFrameRate;

		public WriteableBitmap OutputImage => ImageProcessorService.OutputImage;

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

				if (totalSeconds > 5) {
					_FrameCount = 0;
					_FrameDuration = 0;
					_FrameRunTimer = DateTime.Now;
				}
			}
		}
		double _FrameCount;
		double _FrameDuration;
		DateTime _FrameRunTimer = DateTime.Now;
		DateTime _FrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);
		DateTime _FrameNow;

		KinectSensor Sensor;
		ColorFrameReader ColorFrameReader;
		ImageProcessorService ImageProcessorService;

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

		void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e) => StatusText = Sensor.IsAvailable ? "Running" : "Sensor not available";

		void ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e) {
			using (var colorFrame = e.FrameReference.AcquireFrame()) {
				if (colorFrame == null)
					return;
				
				ImageProcessorService.UpdateInput(colorFrame);
			}
		}

		void KeepOutputUpdated() {
			Task.Run(() => {
				Stopwatch timer;

				while (true) {
					timer = Stopwatch.StartNew();

					ImageProcessorService.UpdateOutput();

					FrameCount++;

					timer.Stop();

					_FrameDuration += timer.ElapsedMilliseconds;

					if (timer.ElapsedMilliseconds < 33)
						Thread.Sleep(33 - (int) timer.ElapsedMilliseconds);
				}
			});
		}

		/// <summary>
		/// SensorService factory
		/// </summary>
		public static SensorService Create() {
			var sensor = KinectSensor.GetDefault();
			var colorFrameDescription = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

			var sensorService = new SensorService(sensor) {
				ColorFrameReader = sensor.ColorFrameSource.OpenReader(),
			};

			sensorService.ColorFrameReader.FrameArrived += sensorService.ColorFrameArrived;
			sensorService.ImageProcessorService = ImageProcessorService.Create(colorFrameDescription);

			sensor.Open();

			sensorService.KeepOutputUpdated();

			return sensorService;
		}
	}
}