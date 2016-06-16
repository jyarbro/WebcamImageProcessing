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

		public KinectSensor Sensor;
		public ImageProcessor ImageProcessor;

		public WriteableBitmap FilteredImage;

		public ColorImageFrame ColorFrame;
		public DepthImageFrame DepthFrame;

		public byte[] ColorSensorData;
		public DepthImagePixel[] DepthSensorData;

		public short[] ImageDepthData;

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

		public void Load() {
			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			ImageProcessor = new ImageProcessor();
			ImageProcessor.Load();

			ColorSensorData = new byte[Sensor.ColorStream.FrameWidth * Sensor.ColorStream.FrameHeight * Sensor.ColorStream.FrameBytesPerPixel];
			ImageProcessor.ColorSensorData = ColorSensorData;

			DepthSensorData = new DepthImagePixel[Sensor.DepthStream.FramePixelDataLength];
			ImageDepthData = new short[Sensor.DepthStream.FramePixelDataLength];
			ImageProcessor.ImageDepthData = ImageDepthData;

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
		
		void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e) {
			try {
				ColorFrame = e.OpenColorImageFrame();
				DepthFrame = e.OpenDepthImageFrame();

				ColorFrame.CopyPixelDataTo(ColorSensorData);
				DepthFrame.CopyDepthImagePixelDataTo(DepthSensorData);

				ColorFrame.Dispose();
				DepthFrame.Dispose();
			}
			catch { }
		}

		void ProcessSensorData() {
			Task.Run(() => {
				Stopwatch timer;
				byte[] processedImage = null;
				int pixel;

				var imageRect = new Int32Rect(0, 0, Sensor.DepthStream.FrameWidth, Sensor.DepthStream.FrameHeight);
				var imageStride = Sensor.ColorStream.FrameWidth * Sensor.ColorStream.FrameBytesPerPixel;

				var colorCoordinates = new ColorImagePoint[Sensor.DepthStream.FramePixelDataLength];

				while (true) {
					timer = Stopwatch.StartNew();

					Sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, DepthSensorData, ColorImageFormat.RgbResolution640x480Fps30, colorCoordinates);

					for (pixel = 0; pixel < Sensor.DepthStream.FramePixelDataLength; pixel++) {
						var point = colorCoordinates[pixel];

						if ((point.X >= 0 && point.X < Sensor.DepthStream.FrameWidth) && (point.Y >= 0 && point.Y < Sensor.DepthStream.FrameHeight))
							ImageDepthData[point.Y * Sensor.DepthStream.FrameWidth + point.X] = DepthSensorData[pixel].Depth;
					}

					if (ImageProcessor != null)
						processedImage = ImageProcessor.ProcessImage();

					if (processedImage == null)
						continue;

					if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted)
						return;

					try { Application.Current.Dispatcher.Invoke(() => { FilteredImage.WritePixels(imageRect, processedImage, imageStride, 0); }); }
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