using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using KIP2.Helpers;
using KIP2.Models.DepthProcessors;
using KIP2.Models.ImageProcessors;
using Microsoft.Kinect;

namespace KIP2.Models {
	public class StreamManager : Observable {
		public event EventHandler<FrameRateEventArgs> UpdateFrameRate;

		double FrameCount {
			get { return _FrameCount; }
			set {
				if (_FrameCount == value)
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

		protected double FrameDuration;

		uint FrameRateDelay;
		DateTime FrameTimer;
		DateTime RunTimer;

		public ImageProcessorBase ImageProcessor;
		public DepthProcessorBase DepthProcessor;

		public KinectSensor Sensor;
		public Int32Rect ImageRect;
		public WriteableBitmap FilteredImage;

		public int ColorSourceStride;

		public byte[] ColorSensorData;
		public DepthImagePixel[] DepthSensorData;

		public StreamManager() {
			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			if (Sensor == null)
				return;

			ImageRect = new Int32Rect(0, 0, 640, 480);

			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			ColorSourceStride = 640 * 4;
			ColorSensorData = new byte[640 * 480 * 4];

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			Sensor.ColorFrameReady += (object sender, ColorImageFrameReadyEventArgs e) => {
				using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
					try { colorFrame.CopyPixelDataTo(ColorSensorData); }
					catch { }
				}
			};

			DepthSensorData = new DepthImagePixel[Sensor.DepthStream.FramePixelDataLength];

			Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
			Sensor.DepthFrameReady += (object sender, DepthImageFrameReadyEventArgs e) => {
				using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
					try { depthFrame.CopyDepthImagePixelDataTo(DepthSensorData); }
					catch { }
				}
			};

			try {
				Sensor.Start();
			}
			catch (IOException) {
				Sensor = null;
			}

			ProcessSensorData();
		}

		void ResetFPS() {
			FrameCount = 0;
			FrameDuration = 0;
			RunTimer = DateTime.Now;
		}
		
		void ProcessSensorData() {
			Task.Run(() => {
				Stopwatch timer;
				byte[] processedImage = null;

				while (true) {
					timer = Stopwatch.StartNew();

					//if (DepthProcessor != null)


					if (ImageProcessor != null)
						processedImage = ImageProcessor.ProcessImage(ColorSensorData);

					if (processedImage == null)
						continue;

					if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted)
						return;

					try {
						Application.Current.Dispatcher.Invoke(() => { FilteredImage.WritePixels(ImageRect, processedImage, ColorSourceStride, 0); });
					}
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