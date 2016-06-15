using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using KIP2.Helpers;
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

		protected double FrameDuration;

		uint FrameRateDelay;
		DateTime FrameTimer;
		DateTime RunTimer;

		public ImageProcessorBase ImageProcessor;

		public KinectSensor Sensor;
		public Int32Rect ImageRect;
		public WriteableBitmap FilteredImage;

		ColorImageFrame ColorFrame;
		DepthImageFrame DepthFrame;

		public Point ImageMax;

		public int PixelCount;
		public int ColorSourceStride;

		public byte[] ColorSensorData;
		public DepthImagePixel[] DepthSensorData;
		public ColorImagePoint[] ColorCoordinates;

		public short[] ImageDepthData;

		public StreamManager() {
			ImageMax = new Point(640, 480);

			PixelCount = ImageMax.X * ImageMax.Y;

			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			if (Sensor == null)
				return;

			ImageRect = new Int32Rect(0, 0, ImageMax.X, ImageMax.Y);

			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			ColorSourceStride = ImageMax.X * 4;

			ColorSensorData = new byte[PixelCount * 4];

			DepthSensorData = new DepthImagePixel[Sensor.DepthStream.FramePixelDataLength];
			ColorCoordinates = new ColorImagePoint[Sensor.DepthStream.FramePixelDataLength];
			ImageDepthData = new short[PixelCount];

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

			Sensor.AllFramesReady += SensorAllFramesReady;

			try {
				Sensor.Start();
			}
			catch (IOException) {
				Sensor = null;
			}

			ProcessSensorData();
		}

		public void SetImageProcessor(string selectedImageProcessorName) {
			var processorType = Type.GetType("KIP2.Models.ImageProcessors." + selectedImageProcessorName + ", KIP2");
			var processorInstance = (ImageProcessorBase)Activator.CreateInstance(processorType);

			processorInstance.ColorSensorData = ColorSensorData;
			processorInstance.ImageDepthData = ImageDepthData;

			processorInstance.Prepare();

			ImageProcessor = processorInstance;
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

				while (true) {
					timer = Stopwatch.StartNew();

					Sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, DepthSensorData, ColorImageFormat.RgbResolution640x480Fps30, ColorCoordinates);

					for (pixel = 0; pixel < PixelCount; pixel++) {
						var point = ColorCoordinates[pixel];

						if ((point.X >= 0 && point.X < ImageMax.X) && (point.Y >= 0 && point.Y < ImageMax.Y))
							ImageDepthData[point.Y * ImageMax.X + point.X] = DepthSensorData[pixel].Depth;
					}

					if (ImageProcessor != null)
						processedImage = ImageProcessor.ProcessImage();

					if (processedImage == null)
						continue;

					if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted)
						return;

					try { Application.Current.Dispatcher.Invoke(() => { FilteredImage.WritePixels(ImageRect, processedImage, ColorSourceStride, 0); }); }
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