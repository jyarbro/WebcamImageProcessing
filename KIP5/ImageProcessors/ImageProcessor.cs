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
	unsafe abstract class ImageProcessor : Observable, IImageProcessor {
		protected const int CHUNK_SIZE = 4;
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

		public WriteableBitmap OutputImage { get; }

		protected Int32Rect FrameRect;
		protected int FrameWidth;
		protected int FrameHeight;
		protected int FrameStride;
		protected uint PixelCount;
		protected byte[] Output;

		Stopwatch _timer;
		bool _executing;

		public ImageProcessor(SensorReader sensorReader) {
			FrameWidth = sensorReader.SensorImageWidth;
			FrameHeight = sensorReader.SensorImageHeight;

			OutputImage = new WriteableBitmap(FrameWidth, FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
			FrameRect = new Int32Rect(0, 0, FrameWidth, FrameHeight);
			FrameStride = FrameWidth * CHUNK_SIZE;

			PixelCount = sensorReader.PixelCount;

			Output = new byte[sensorReader.ByteCount];

			sensorReader.SensorDataReady += OnSensorDataReady;
		}

		/// <summary>
		/// A universal method for calculating all of the linear offsets for a given square area
		/// </summary>
		/// <exception cref="ArgumentException" />
		public int[] CalculateOffsets(Rectangle areaBox, int area, int stride, int chunkSize = 1) {
			var offsets = new int[area];

			var offset = 0;

			for (var yOffset = areaBox.Origin.Y; yOffset <= areaBox.Extent.Y; yOffset++) {
				for (var xOffset = areaBox.Origin.X; xOffset <= areaBox.Extent.X; xOffset++) {
					offsets[offset] = (yOffset * stride) + xOffset;
					offsets[offset] = offsets[offset] * chunkSize;

					offset++;
				}
			}

			return offsets;
		}

		protected abstract void ApplyFilters(Pixel[] sensorData);

		void WriteOutput() {
			Application.Current?.Dispatcher.Invoke(() => {
				OutputImage.Lock();
				OutputImage.WritePixels(FrameRect, Output, FrameStride, 0);
				OutputImage.Unlock();
			});
		}

		void OnSensorDataReady(object sender, SensorDataReadyEventArgs args) {
			if (_executing)
				return;

			_executing = true;

			Task.Run(() => {
				_timer = Stopwatch.StartNew();
				
				ApplyFilters(args.SensorData);
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