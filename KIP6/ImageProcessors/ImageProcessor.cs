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
		const uint FRAMERATE_DELAY = 20;

		public int OutputWidth;
		public int OutputHeight;
		public int OutputStride;
		public uint PixelCount;
		public byte[] OutputData;
		public Int32Rect OutputUpdateRect;
		public Stopwatch FrameTimer = Stopwatch.StartNew();

		public WriteableBitmap OutputImage { get; set; }

		public double FrameCount {
			get => _FrameCount;
			set {
				if (_FrameCount == value)
					return;

				_FrameCount = value;

				_FrameNow = DateTime.Now;
				_TotalSeconds = (_FrameNow - _FrameRunTimer).TotalSeconds;

				if (_FrameTimer < _FrameNow) {
					_FrameTimer = _FrameNow.AddMilliseconds(FRAMERATE_DELAY);

					FramesPerSecond = Math.Round(_FrameCount / _TotalSeconds, 2);
					FrameLag = Math.Round(_FrameDuration / _FrameCount, 2);
				}

				if (_TotalSeconds > 5) {
					_FrameCount = 0;
					_FrameDuration = 0;
					_FrameRunTimer = DateTime.Now;
				}
			}
		}
		double _FrameCount;
		double _FrameDuration;
		double _TotalSeconds;
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

		public void LoadFrame(ColorFrameReference frameReference) {
			FrameTimer.Restart();

			FrameCount++;

			try {
				ProcessFrame(frameReference);
				Application.Current.Dispatcher.Invoke(UpdateOutputImage);
			}
			catch (NullReferenceException) { }

			FrameTimer.Stop();
			_FrameDuration += FrameTimer.ElapsedMilliseconds;
		}

		public abstract void ProcessFrame(ColorFrameReference frameReference);

		public void UpdateOutputImage() {
			OutputImage.Lock();
			OutputImage.WritePixels(OutputUpdateRect, OutputData, OutputStride, 0);
			OutputImage.Unlock();
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

		public void OnFrameArrived(object sender, ColorFrameArrivedEventArgs e) => LoadFrame(e.FrameReference);
	}
}