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
using System.Threading.Tasks;

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

		public KinectSensor Sensor { get; set; }
		public WriteableBitmap FilteredImage { get; set; }

		int _FPSCalcDelay = 50;

		DateTime FrameTimer;
		DateTime RunTimer;
		double FrameProcessDuration;

		int[] EdgeFilterWeights;
		int[] EdgeFilterOffsets;

		int[] DespeckleFilterWeights;
		int[] DespeckleFilterOffsets;

		int SourceStride;
		int SourceWidth;
		int SourceHeight;

		int PixelCount;
		int ByteCount;

		Int32Rect ImageRect;

		int[] IntArray1;
		int[] IntArray2;
		byte[] ByteArray1;
		byte[] ByteArray2;

		int PixelValueMax;
		int PixelEdgeThreshold;

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

			BuildFilters();

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
		}

		async void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
			var timer = System.Diagnostics.Stopwatch.StartNew();
			var parentThread = Dispatcher.CurrentDispatcher;

			using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
				if (colorFrame != null) colorFrame.CopyPixelDataTo(ByteArray1);
			}

			await Task.Run(() => {
				IntArray1 = CompressToMonochrome(ByteArray1);
				IntArray1 = FilterEdges(IntArray1);
				ByteArray2 = ExpandFromMonochrome(IntArray1, ByteArray1);

				parentThread.Invoke(() => {
					FilteredImage.WritePixels(
						ImageRect,
						ByteArray2,
						SourceStride,
						0);
				});

				FrameCounter++;

				timer.Stop();
				FrameProcessDuration += timer.ElapsedMilliseconds;
			});
		}

		byte[] ExpandFromMonochrome(int[] inputValues, byte[] inputBytes) {
			var outputValues = new byte[ByteCount];

			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < ByteCount; byteOffset += 4) {
				if (inputValues[pixelOffset] > PixelEdgeThreshold) {
					outputValues[byteOffset] = 0;
					outputValues[byteOffset + 1] = 0;
					outputValues[byteOffset + 2] = 0;
				}
				else {
					outputValues[byteOffset] = inputBytes[byteOffset];
					outputValues[byteOffset + 1] = inputBytes[byteOffset + 1];
					outputValues[byteOffset + 2] = inputBytes[byteOffset + 2];
				}

				pixelOffset++;
			}

			return outputValues;
		}

		int[] CompressToMonochrome(byte[] inputValues) {
			var outputValues = new int[PixelCount];

			var pixelOffset = 0;

			for (var byteOffset = 0; byteOffset < ByteCount; byteOffset += 4) {
				outputValues[pixelOffset] =
					(inputValues[byteOffset] * inputValues[byteOffset]) +
					(inputValues[byteOffset + 1] * inputValues[byteOffset + 1]) +
					(inputValues[byteOffset + 2] * inputValues[byteOffset + 2]);
				pixelOffset++;
			}

			return outputValues;
		}

		int[] FilterEdges(int[] monochromePixelValues) {
			var filteredPixelValues = new int[PixelCount];
			var filterLength = EdgeFilterWeights.Length;

			for (var pixel = 0; pixel < PixelCount; pixel++) {
				var aggregate = 0;

				for (var filterOffset = 0; filterOffset < filterLength; filterOffset++) {
					var offset = EdgeFilterOffsets[filterOffset] + pixel;

					if (offset > 0 && offset < PixelCount)
						aggregate += monochromePixelValues[offset] * EdgeFilterWeights[filterOffset];
				}

				if (aggregate >= PixelEdgeThreshold)
					filteredPixelValues[pixel] = PixelValueMax;
				else
					filteredPixelValues[pixel] = 0;
			}

			return filteredPixelValues;
		}

		int[] Despeckle(int[] inputPixelValues) {
			var filteredPixelValues = new int[PixelCount];
			var filterLength = DespeckleFilterWeights.Length;

			for (var pixel = 0; pixel < PixelCount; pixel++) {
				var aggregate = 0;

				for (var filterOffset = 0; filterOffset < filterLength; filterOffset++) {
					var offset = DespeckleFilterOffsets[filterOffset] + pixel;

					if (offset > 0 && offset < PixelCount)
						aggregate += inputPixelValues[offset] * DespeckleFilterWeights[filterOffset];
				}

				if (aggregate >= 255 * 255 * 9)
					filteredPixelValues[pixel] = PixelValueMax;
				else
					filteredPixelValues[pixel] = 0;
			}

			return filteredPixelValues;
		}

		void ResetFPS() {
			FrameCounter = 0;
			FrameProcessDuration = 0;
			RunTimer = default(DateTime);
		}
		
		void BuildFilters() {
			var despeckleFilter = new int[,] {
				{ -1, -1, -1 },
				{ -1,  8, -1 },
				{ -1, -1, -1 },
			};

			var edgeFilter = new int[,] {
				{  0, -1,  0, -1,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0,  0, 12,  0,  0 },
				{ -1, -1,  0, -1, -1 },
				{  0, -1,  0, -1,  0 }
			};

			PixelValueMax = 3 * 255 * 255;
			PixelEdgeThreshold = PixelValueMax / 4;

			var filterLength = edgeFilter.GetLength(0);
			var filterOffset = Convert.ToInt32(Math.Floor((double)filterLength / 2));
			var filterEnd = filterLength - filterOffset;

			EdgeFilterWeights = new int[filterLength * filterLength];
			EdgeFilterOffsets = new int[filterLength * filterLength];

			var filterOffsetCount = 0;

			for (int filterY = -filterOffset; filterY < filterEnd; filterY++) {
				for (int filterX = -filterOffset; filterX < filterEnd; filterX++) {
					EdgeFilterWeights[filterOffsetCount] = edgeFilter[filterY + filterOffset, filterX + filterOffset];
					EdgeFilterOffsets[filterOffsetCount] = (SourceWidth * filterY) + filterX;
					filterOffsetCount++;
				}
			}

			filterLength = despeckleFilter.GetLength(0);
			filterOffset = Convert.ToInt32(Math.Floor((double)filterLength / 2));
			filterEnd = filterLength - filterOffset;

			DespeckleFilterWeights = new int[filterLength * filterLength];
			DespeckleFilterOffsets = new int[filterLength * filterLength];

			filterOffsetCount = 0;

			for (int filterY = -filterOffset; filterY < filterEnd; filterY++) {
				for (int filterX = -filterOffset; filterX < filterEnd; filterX++) {
					DespeckleFilterWeights[filterOffsetCount] = despeckleFilter[filterY + filterOffset, filterX + filterOffset];
					DespeckleFilterOffsets[filterOffsetCount] = (SourceWidth * filterY) + filterX;
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