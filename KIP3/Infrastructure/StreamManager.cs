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

		public KinectSensor Sensor;
		public ImageProcessor ImageProcessor;

		public WriteableBitmap FilteredImage;

		public ColorImageFrame ColorFrame;
		public DepthImageFrame DepthFrame;

		public byte[] ColorSensorData;
		public DepthImagePixel[] DepthSensorData;

		public short[] ImageDepthData;

		public int PixelCount;
		public int FrameWidth;
		public int FrameHeight;

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

			FrameWidth = Sensor.ColorStream.FrameWidth;
			FrameHeight = Sensor.ColorStream.FrameHeight;
			PixelCount = FrameWidth * FrameHeight;

			ColorSensorData = new byte[PixelCount * 4];
			ImageDepthData = new short[Sensor.DepthStream.FramePixelDataLength];
			DepthSensorData = new DepthImagePixel[Sensor.DepthStream.FramePixelDataLength];

			ImageProcessor = new ImageProcessor {
				StatusText = StatusText,
				ColorSensorData = ColorSensorData,
				ImageDepthData = ImageDepthData
			};

			ImageProcessor.PropertyChanged += ImageProcessor_PropertyChanged;

			ImageProcessor.Load();

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

			Sensor.AllFramesReady += SensorAllFramesReady;
			Sensor.Start();

			ProcessSensorData();
		}

		private void ImageProcessor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			if (e.PropertyName == "StatusText")
				StatusText = ((ImageProcessor) sender).StatusText;
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

				var imageRect = new Int32Rect(0, 0, FrameWidth, FrameHeight);
				var imageStride = FrameWidth * 4;

				var colorCoordinates = new ColorImagePoint[Sensor.DepthStream.FramePixelDataLength];

				ColorImagePoint colorCoordinatesPoint;

				while (true) {
					timer = Stopwatch.StartNew();

					Sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, DepthSensorData, ColorImageFormat.RgbResolution640x480Fps30, colorCoordinates);

					for (pixel = 0; pixel < PixelCount; pixel++) {
						colorCoordinatesPoint = colorCoordinates[pixel];

						if ((colorCoordinatesPoint.X >= 0 && colorCoordinatesPoint.X < FrameWidth) && (colorCoordinatesPoint.Y >= 0 && colorCoordinatesPoint.Y < FrameHeight))
							ImageDepthData[colorCoordinatesPoint.Y * FrameWidth + colorCoordinatesPoint.X] = DepthSensorData[pixel].Depth;
					}

					if (ImageProcessor != null)
						processedImage = ImageProcessor.ProcessImage();

					if (processedImage == null)
						continue;

					//if (Application.Current == null || Application.Current.Dispatcher.HasShutdownStarted)
					//	return;

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