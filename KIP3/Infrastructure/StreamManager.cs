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

		public WriteableBitmap FilteredImage;

		public KinectSensor Sensor;
		public ImageProcessor ImageProcessor;

		public byte[] OutputData;

		public ColorImagePoint[] ColorCoordinates;
		public DepthImagePixel[] RawDepthSensorData;

		public int FrameWidth;
		public int FrameHeight;
		public int PixelCount;
		public int ByteCount;

		public Pixel[] Pixels;
		public PixelLocation[] PixelLocations;

		public void Load() {
			FrameRateDelay = 50;
			FrameTimer = DateTime.Now.AddMilliseconds(FrameRateDelay);

			ResetFPS();

			Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);

			FrameWidth = Sensor.ColorStream.FrameWidth;
			FrameHeight = Sensor.ColorStream.FrameHeight;
			PixelCount = Sensor.DepthStream.FramePixelDataLength;
			ByteCount = PixelCount * 4;

			OutputData = new byte[ByteCount];
			ColorCoordinates = new ColorImagePoint[PixelCount];

			PreparePixels();

			ImageProcessor = new ImageProcessor {
				StatusText = StatusText,
				OutputData = OutputData,
				PixelLocations = PixelLocations,
				Pixels = Pixels,
				PixelCount = PixelCount,
				ByteCount = ByteCount,
				ImageMax = new Point(FrameWidth, FrameHeight)
			};

			ImageProcessor.PropertyChanged += ImageProcessor_PropertyChanged;

			ImageProcessor.Load();

			Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
			Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

			Sensor.AllFramesReady += SensorAllFramesReady;
			Sensor.Start();

			ProcessSensorData();
		}

		/// <summary>
		/// Precalculate pixel values
		/// </summary>
		public void PreparePixels() {
			Pixels = new Pixel[PixelCount];
			PixelLocations = new PixelLocation[PixelCount];

			for (var i = 0; i < PixelCount; i++) {
				var y = i / FrameWidth;
				var x = i % FrameWidth;

				var xSq = Math.Pow(Math.Abs(x - (FrameWidth / 2)), 2);
				var ySq = Math.Pow(Math.Abs(y - (FrameHeight / 2)), 2);
				var distance = Math.Sqrt(xSq + ySq);

				PixelLocations[i] = new PixelLocation {
					X = x,
					Y = y,
					Distance = distance,
					OffsetB = i * 4,
					OffsetG = i * 4 + 1,
					OffsetR = i * 4 + 2
				};
			}
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
								pixel->Depth = short.MaxValue;

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

									var pixelOffset = colorCoordinatesPoint->Y * FrameWidth + colorCoordinatesPoint->X;

									if (depthSensorPoint->Depth > 0 && depthSensorPoint->Depth <= Pixels[ImageProcessor.FocusIndex].Depth) {

										ImageProcessor.FocusIndex = pixelOffset;
									}

									Pixels[pixelOffset].Depth = depthSensorPoint->Depth;
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