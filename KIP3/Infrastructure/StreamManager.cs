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

		public byte[] OutputData;
		public byte[] ColorSensorData;
		public ColorImagePoint[] ColorCoordinates;
		public DepthImagePixel[] RawDepthSensorData;

		public int FrameWidth;
		public int FrameHeight;

		public double FrameDuration;
		public uint FrameRateDelay;
		public DateTime FrameTimer;
		public DateTime RunTimer;

		public int PixelCount;
		public Pixel CurrentPixel;

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
			PixelCount = Sensor.DepthStream.FramePixelDataLength;

			OutputData = new byte[PixelCount * 4];
			ColorSensorData = new byte[PixelCount * 4];
			ColorCoordinates = new ColorImagePoint[PixelCount];

			ImageProcessor = new ImageProcessor {
				StatusText = StatusText,
				OutputData = OutputData
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
				StatusText = ((ImageProcessor)sender).StatusText;
		}

		void ResetFPS() {
			FrameCount = 0;
			FrameDuration = 0;
			RunTimer = DateTime.Now;
		}

		void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e) {
			Task.Run(() => {
				using (var colorFrame = e.OpenColorImageFrame()) {
					using (var depthFrame = e.OpenDepthImageFrame()) {
						CopyFrameData(colorFrame, depthFrame);
					}
				}
			});
		}

		void CopyFrameData(ColorImageFrame colorFrame, DepthImageFrame depthFrame) {
			try {
				unsafe
				{
					var i = 0;

					fixed (Pixel* pixels = ImageProcessor.Pixels)
					{
						fixed (byte* colorSensorData = colorFrame.GetRawPixelData())
						{
							var pixel = pixels;
							var color = colorSensorData;

							while (i++ < PixelCount) {
								pixel->B = *(color);
								pixel->G = *(color + 1);
								pixel->R = *(color + 2);

								color += 4;
								pixel++;
							}
						}
					}

					RawDepthSensorData = depthFrame.GetRawPixelData();
					Sensor.CoordinateMapper.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30, RawDepthSensorData, ColorImageFormat.RgbResolution640x480Fps30, ColorCoordinates);

					i = 0;

					fixed (DepthImagePixel* depthSensorData = RawDepthSensorData)
					{
						fixed (ColorImagePoint* colorCoordinates = ColorCoordinates)
						{
							var depthSensorPoint = depthSensorData;
							var colorCoordinatesPoint = colorCoordinates;

							while (i++ < PixelCount) {
								if ((colorCoordinatesPoint->X >= 0 && colorCoordinatesPoint->X < FrameWidth)
									&& (colorCoordinatesPoint->Y >= 0 && colorCoordinatesPoint->Y < FrameHeight)) {

									ImageProcessor.Pixels[colorCoordinatesPoint->Y * FrameWidth + colorCoordinatesPoint->X].Depth = depthSensorPoint->Depth;
								}

								colorCoordinatesPoint++;
								depthSensorPoint++;
							}
						}
					}
				}

				colorFrame.Dispose();
				depthFrame.Dispose();
			}
			catch { }
		}

		void ProcessSensorData() {
			Task.Run(() => {
				Stopwatch timer;
				var imageRect = new Int32Rect(0, 0, FrameWidth, FrameHeight);
				var imageStride = FrameWidth * 4;

				while (true) {
					timer = Stopwatch.StartNew();
					
					//Buffer.BlockCopy(ColorSensorData, 0, OutputData, 0, ColorSensorData.Length);

					ImageProcessor.ProcessImage();

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