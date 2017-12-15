using KIP.Helpers;
using KIP.Structs;
using KIP5.Helpers;
using KIP5.Interfaces;
using KIP5.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KIP5.ImageProcessors {
	abstract class ImageProcessor : Observable, IImageProcessor {
		const uint FRAMERATE_DELAY = 50;

		public double FramesPerSecond {
			get => _FramesPerSecond;
			set => SetProperty(ref _FramesPerSecond, value);
		}
		double _FramesPerSecond = 0;

		public double FrameLag {
			get => _FrameLag;
			set => SetProperty(ref _FrameLag, value);
		}
		double _FrameLag = 0;

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

					FramesPerSecond = Math.Round(_FrameCount / totalSeconds);
					FrameLag = Math.Round(_FrameDuration / _FrameCount, 3);
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

		public WriteableBitmap OutputImage { get; } = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);

		protected Int32Rect FrameRect;
		protected int FrameStride;
		protected uint PixelCount;
		protected byte[] OutputData;

		Stopwatch _timer;
		bool _executing;
	
		public ImageProcessor(SensorReader sensorReader) {
			FrameRect = new Int32Rect(0, 0, sensorReader.SensorImageWidth, sensorReader.SensorImageHeight);
			FrameStride = sensorReader.SensorImageWidth * 4;
			PixelCount = sensorReader.PixelCount;

			OutputData = new byte[sensorReader.ByteCount];

			sensorReader.SensorDataReady += OnSensorDataReady;
		}

		protected abstract void LoadInput(Pixel[] sensorData);
		protected abstract void ApplyFilters();
		protected abstract void WriteOutput();

		void OnSensorDataReady(object sender, SensorDataReadyEventArgs args) {
			if (_executing)
				return;

			Task.Run(() => {
				_timer = Stopwatch.StartNew();

				_executing = true;

				LoadInput(args.SensorData);
				ApplyFilters();
				WriteOutput();

				FrameCount++;

				_timer.Stop();

				_FrameDuration += _timer.ElapsedMilliseconds;

				if (_timer.ElapsedMilliseconds < 33)
					Thread.Sleep(33 - (int) _timer.ElapsedMilliseconds);

				_executing = false;
			});
		}
	}
}