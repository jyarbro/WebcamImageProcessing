using KIP.Helpers;
using KIP.Structs;
using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KIP6.ImageProcessors {
	public unsafe abstract class ImageProcessor : Observable {
		const uint FRAMERATE_DELAY = 50;

		public int OutputWidth;
		public int OutputHeight;
		public int OutputStride;
		public uint PixelCount;
		public byte[] OutputData;
		public Int32Rect OutputUpdateRect;

		Stopwatch _timer;
		bool _working;

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

		public WriteableBitmap OutputImage { get; set; }

		public void LoadFrame(ColorFrameReference frameReference) {

			_working = true;
			_timer = Stopwatch.StartNew();

			Task.Run(() => {
				try {
					ProcessFrame(frameReference);
					WriteOutput();
				}
				catch (NullReferenceException) { }

				FrameCount++;
				_timer.Stop();
				_FrameDuration += _timer.ElapsedMilliseconds;
				_working = false;
			});
		}

		public abstract void ProcessFrame(ColorFrameReference frameReference);

		public void WriteOutput() {
			Application.Current?.Dispatcher.Invoke(() => {
				OutputImage.Lock();
				OutputImage.WritePixels(OutputUpdateRect, OutputData, OutputStride, 0);
				OutputImage.Unlock();
			});
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
	}
}