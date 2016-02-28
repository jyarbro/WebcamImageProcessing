using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using KinectImageProcessing.Helpers;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace KinectImageProcessing {
	public class MainWindowViewModel : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		public string StatusText {
			get {
				return _StatusText ?? (_StatusText = string.Empty);
			}
			set {
				if (_StatusText == value)
					return;

				_StatusText = value;
				OnPropertyChanged();
			}
		}
		string _StatusText;

		public double FramesPerSecond {
			get { return _FramesPerSecond; }
			set {
				if (value == _FramesPerSecond)
					return;

				_FramesPerSecond = value;
				OnPropertyChanged();
			}
		}
		double _FramesPerSecond = 0;

		public double FrameLag {
			get { return _FrameLag; }
			set {
				if (value == _FrameLag)
					return;

				_FrameLag = value;
				OnPropertyChanged();
			}
		}
		double _FrameLag = 0;

		double FrameCounter {
			get { return _FrameCounter; }
			set {
				if (value == _FrameCounter)
					return;

				_FrameCounter = value;

				var now = DateTime.Now;

				if (RunTimer == null || RunTimer == default(DateTime))
					RunTimer = now;

				var totalSeconds = (now - RunTimer).TotalSeconds;

				if (FrameTimer < now) {
					FrameTimer = now.AddMilliseconds(_FPSCalcDelay);
					FramesPerSecond = Math.Round(FrameCounter / totalSeconds);
					FrameLag = Math.Round(FrameProcessDuration / FrameCounter);
				}

				if (totalSeconds > 5)
					ResetFPS();
			}
		}
		double _FrameCounter = 0;
		int _FPSCalcDelay = 50;

		public KinectSensor Sensor { get; set; }
		public WriteableBitmap FilteredImage { get; set; }

		DateTime FrameTimer { get; set; }
		DateTime RunTimer { get; set; }
		double FrameProcessDuration { get; set; }

		int[] FilterWeights { get; set; }
		int[] FilterOffsets { get; set; }

		int SourceStride { get; set; }
		int SourceWidth { get; set; }
		int SourceHeight { get; set; }

		int PixelCount { get; set; }
		int ByteCount { get; set; }

		Int32Rect ImageRect { get; set; }

		int[] IntArray1 { get; set; }
		int[] IntArray2 { get; set; }
		byte[] ByteArray1 { get; set; }
		byte[] ByteArray2 { get; set; }

		public MainWindowViewModel() {
			FrameTimer = DateTime.Now.AddMilliseconds(_FPSCalcDelay);
		}

		public void Load() {
			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			if (Sensor == null)
				return;

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

			SourceWidth = Sensor.ColorStream.FrameWidth;
			SourceHeight = Sensor.ColorStream.FrameHeight;

			ImageRect = new Int32Rect(0, 0, SourceWidth, SourceHeight);
			FilteredImage = new WriteableBitmap(SourceWidth, SourceHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

			SourceStride = FilteredImage.PixelWidth * sizeof(int);

			PixelCount = SourceWidth * SourceHeight;
			ByteCount = SourceStride * SourceHeight;

			RebuildFilterFrame();

			IntArray1 = new int[PixelCount];
			IntArray2 = new int[PixelCount];
			ByteArray1 = new byte[ByteCount];
			ByteArray2 = new byte[ByteCount];

			Sensor.ColorFrameReady += SensorColorFrameReady;

			try {
				Sensor.Start();
			}
			catch (IOException) {
				Sensor = null;
			}

			if (Sensor == null)
				throw new Exception("No Kinect found or ready");
		}

		void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
			var parentThread = Dispatcher.CurrentDispatcher;

			using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
				if (colorFrame != null) colorFrame.CopyPixelDataTo(ByteArray1);
			}

			Task.Run(() => {
				var timer = System.Diagnostics.Stopwatch.StartNew();

				IntArray1 = CompressToMonochrome(ByteArray1);
				IntArray2 = FilterEdges(IntArray1);
				ByteArray2 = ExpandFromMonochrome(IntArray2);

				#region Flush to screen

				try {
					parentThread.Invoke(() => {
						FilteredImage.WritePixels(
							ImageRect,
							ByteArray2,
							SourceStride,
							0);
					});
				}
				catch { }

				FrameCounter++;

				timer.Stop();
				FrameProcessDuration += timer.ElapsedMilliseconds;

				#endregion
			});
		}

		byte[] ExpandFromMonochrome(int[] inputValues) {
			var outputValues = new byte[ByteCount];

			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < ByteCount; byteOffset += 4) {
				var pixelValue = (byte)(255 - (inputValues[pixelOffset] / 3));
				outputValues[byteOffset] = pixelValue;
				outputValues[byteOffset + 1] = pixelValue;
				outputValues[byteOffset + 2] = pixelValue;
				outputValues[byteOffset + 3] = 255;
				pixelOffset++;
			}

			return outputValues;
		}

		int[] CompressToMonochrome(byte[] inputValues) {
			var outputValues = new int[PixelCount];

			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < ByteCount; byteOffset += 4) {
				outputValues[pixelOffset] = inputValues[byteOffset] + inputValues[byteOffset + 1] + inputValues[byteOffset + 2];
				pixelOffset++;
			}

			return outputValues;
		}

		int[] FilterEdges(int[] monochromePixelValues) {
			var filteredPixelValues = new int[PixelCount];
			var filterLength = FilterWeights.Length;

			for (var pixel = 0; pixel < PixelCount; pixel++) {
				var aggregate = 0;

				for (var filterOffset = 0; filterOffset < filterLength; filterOffset++) {
					var offset = FilterOffsets[filterOffset] + pixel;

					if (offset > 0 && offset < PixelCount)
						aggregate += monochromePixelValues[offset] * FilterWeights[filterOffset];
				}

				if (aggregate >= 128)
					aggregate = 765;
				else if (aggregate < 128)
					aggregate = 0;

				filteredPixelValues[pixel] = aggregate;
			}

			return filteredPixelValues;
		}

		void ResetFPS() {
			FrameCounter = 0;
			FrameProcessDuration = 0;
			RunTimer = default(DateTime);
		}
		
		void RebuildFilterFrame() {
			var filter = new int[,] {
				{  0, -1,  0, -1,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0,  0, 12,  0,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0, -1,  0, -1,  0 }
			};

			var filterLength = filter.GetLength(0);
			var filterOffset = Convert.ToInt32(Math.Floor((double)filterLength / 2));
			var filterEnd = filterLength - filterOffset;

			FilterWeights = new int[filterLength * filterLength];
			FilterOffsets = new int[filterLength * filterLength];

			var filterOffsetCount = 0;

			for (int filterY = -filterOffset; filterY < filterEnd; filterY++) {
				for (int filterX = -filterOffset; filterX < filterEnd; filterX++) {
					FilterWeights[filterOffsetCount] = filter[filterY + filterOffset, filterX + filterOffset];
					FilterOffsets[filterOffsetCount] = (SourceWidth * filterY) + filterX;
					filterOffsetCount++;
				}
			}
		}

		[NotifyPropertyChangedAction]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}