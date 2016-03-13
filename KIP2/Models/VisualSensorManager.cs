using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using KIP2.Models.ImageProcessors;
using Microsoft.Kinect;

namespace KIP2.Models {
	public class VisualSensorManager : Observable {
		public event EventHandler<FrameRateEventArgs> UpdateFrameRate;

		double FrameCount {
			get { return _FrameCount; }
			set {
				if (value == _FrameCount)
					return;

				_FrameCount = value;

				var now = DateTime.Now;
				var totalSeconds = (now - RunTimer).TotalSeconds;

				if (FrameTimer < now) {
					FrameTimer = now.AddMilliseconds(FrameRateDelay);

					if (UpdateFrameRate != null) UpdateFrameRate(this, new FrameRateEventArgs {
						FramesPerSecond = Math.Round(_FrameCount / totalSeconds),
						FrameLag = Math.Round(FrameDuration / _FrameCount, 3)
					});
				}

				if (totalSeconds > 5)
					ResetFPS();
			}
		}
		double _FrameCount = 0;

		uint FrameRateDelay;
		double FrameDuration;
		DateTime FrameTimer;
		DateTime RunTimer;

		public ImageProcessor ImageProcessor;

		public KinectSensor Sensor;
		public Int32Rect ImageRect;
		public WriteableBitmap FilteredImage;

		public int SourceStride;
		public byte[] SensorData;

		public VisualSensorManager() {
			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			if (Sensor == null)
				return;

			ImageRect = new Int32Rect(0, 0, 640, 480);

			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

			SourceStride = 640 * 4;
			SensorData = new byte[640 * 480 * 4];

			//Sensor.ColorFrameReady += GetSensorImage;
			Sensor.ColorFrameReady += GetSensorImageAsync;

			try {
				Sensor.Start();
			}
			catch (IOException) {
				Sensor = null;
			}
		}

		void ResetFPS() {
			FrameCount = 0;
			FrameDuration = 0;
			RunTimer = DateTime.Now;
		}

		async void GetSensorImageAsync(object sender, ColorImageFrameReadyEventArgs e) {
			var timer = System.Diagnostics.Stopwatch.StartNew();

			await Task.Run(() => {
				GetSensorImage(sender, e);

				timer.Stop();
				FrameDuration += timer.ElapsedMilliseconds;
			});
		}

		void GetSensorImage(object sender, ColorImageFrameReadyEventArgs e) {
			using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
				try {
					colorFrame.CopyPixelDataTo(SensorData);
				}
				catch {}
			}

			var processedImage = ImageProcessor.ProcessImage(SensorData);

			if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted)
				return;

			try {
				Application.Current.Dispatcher.Invoke(() => { FilteredImage.WritePixels(ImageRect, processedImage, SourceStride, 0); });
			}
			catch { }

			FrameCount++;
		}
	}
}
