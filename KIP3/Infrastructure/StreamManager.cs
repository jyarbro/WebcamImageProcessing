using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using KIP3.Helpers;
using KIP3.Infrastructure;
using Microsoft.Kinect;

namespace KIP3.Models {
	public class StreamManager : Observable {
		public event EventHandler<FrameRateEventArgs> UpdateFrameRate;

		public string StatusText {
			get { return _StatusText ?? (_StatusText = string.Empty); }
			set { SetProperty(ref _StatusText, value); }
		}
		string _StatusText;

		public double FrameDuration;
		public uint FrameRateDelay;
		public DateTime FrameTimer;
		public DateTime RunTimer;

		public double FrameCount {
			get { return _FrameCount; }
			set {
				if (_FrameCount == value)
					return;

				_FrameCount = value;

				var now = DateTime.Now;
				var totalSeconds = (now - RunTimer).TotalSeconds;

				if (FrameTimer < now) {
					FrameTimer = now.AddMilliseconds(FrameRateDelay);

					UpdateFrameRate?.Invoke(this, new FrameRateEventArgs {
						FramesPerSecond = Math.Round(_FrameCount / totalSeconds),
						FrameLag = Math.Round(FrameDuration / _FrameCount, 3)
					});
				}

				if (totalSeconds > 5)
					ResetFPS();
			}
		}
		double _FrameCount = 0;

		public KinectSensor Sensor;
		public ImageProcessor ImageProcessor;
		public WriteableBitmap FilteredImage;

		public byte[] OutputData;

		public int FrameWidth;
		public int FrameHeight;
		public int PixelCount;
		public int ByteCount;

		public void Load() {
			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			Sensor = KinectSensor.KinectSensors.First(s => s.Status == KinectStatus.Connected);

			FrameWidth = Sensor.ColorStream.FrameWidth;
			FrameHeight = Sensor.ColorStream.FrameHeight;
			PixelCount = Sensor.DepthStream.FramePixelDataLength;
			ByteCount = PixelCount * 4;

			OutputData = new byte[ByteCount];

			ImageProcessor = new ImageProcessor {
				StatusText = StatusText,
				OutputData = OutputData,
				PixelCount = PixelCount,
				ByteCount = ByteCount,
				FrameWidth = FrameWidth,
				FrameHeight = FrameHeight
			};

			ImageProcessor.PropertyChanged += ImageProcessor_PropertyChanged;

			ImageProcessor.LoadProcessor();

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

			Sensor.AllFramesReady += SensorAllFramesReady;
			Sensor.Start();

			ProcessSensorData();
		}

		void ResetFPS() {
			FrameCount = 0;
			FrameDuration = 0;
			RunTimer = DateTime.Now;
		}

		void ImageProcessor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "StatusText")
				StatusText = ((ImageProcessor)sender).StatusText;
		}

		void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e) {
			Task.Run(() => {
				try {
					using (var colorFrame = e.OpenColorImageFrame()) {
						using (var depthFrame = e.OpenDepthImageFrame()) {
							ImageProcessor.UpdateInput(Sensor, colorFrame, depthFrame);
						}
					}
				}
				catch { }
			});
		}

		void ProcessSensorData() {
			Task.Run(() => {
				Stopwatch timer;
				var imageRect = new Int32Rect(0, 0, FrameWidth, FrameHeight);
				var imageStride = FrameWidth * 4;

				while (true) {
					timer = Stopwatch.StartNew();
					
					//Buffer.BlockCopy(ColorSensorData, 0, OutputData, 0, ColorSensorData.Length);

					ImageProcessor.UpdateOutput();

					try { Application.Current.Dispatcher.Invoke(() => { FilteredImage.WritePixels(imageRect, OutputData, imageStride, 0); }); }
					catch { }

					FrameCount++;

					timer.Stop();
					FrameDuration += timer.ElapsedMilliseconds;

					if (timer.ElapsedMilliseconds < 33)
						Thread.Sleep(33 - (int)timer.ElapsedMilliseconds);
				}
			});
		}
	}
}