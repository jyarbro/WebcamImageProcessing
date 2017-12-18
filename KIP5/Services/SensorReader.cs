using System;
using System.ComponentModel;
using KIP.Structs;
using KIP5.Helpers;
using KIP5.Interfaces;
using Microsoft.Kinect;

namespace KIP5.Services {
	unsafe class SensorReader : IStatusTracker {
		public event EventHandler<SensorDataReadyEventArgs> SensorDataReady;
		public event PropertyChangedEventHandler StatusChanged;

		public string StatusText {
			get => _StatusText;
			set {
				if (Equals(_StatusText, value))
					return;

				_StatusText = value;

				StatusChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
			}
		}
		string _StatusText = string.Empty;

		public uint PixelCount;
		public uint ByteCount;
		public int SensorImageWidth;
		public int SensorImageHeight;

		KinectSensor Sensor;
		ColorFrameReader ColorFrameReader;
		SensorDataReadyEventArgs SensorDataReadyEventArgs;
		byte[] ColorFrameData;

		int _i;
		Pixel* _pixelPtr;
		byte* _colorBytePtr;

		public SensorReader() {
			Sensor = KinectSensor.GetDefault();
			Sensor.IsAvailableChanged += OnSensorAvailabilityChanged;

			ColorFrameReader = Sensor.ColorFrameSource.OpenReader();
			ColorFrameReader.FrameArrived += OnColorFrameArrived;

			var colorFrameDescription = Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

			SensorImageWidth = colorFrameDescription.Width;
			SensorImageHeight = colorFrameDescription.Height;

			PixelCount = colorFrameDescription.LengthInPixels;
			ByteCount = colorFrameDescription.LengthInPixels * colorFrameDescription.BytesPerPixel;

			SensorDataReadyEventArgs = new SensorDataReadyEventArgs {
				SensorData = new Pixel[PixelCount]
			};

			ColorFrameData = new byte[ByteCount];

			Sensor.Open();
		}

		void LoadColorFrame(ColorFrameReference frameReference) {
			using (var colorFrame = frameReference.AcquireFrame()) {
				try {
					colorFrame.CopyConvertedFrameDataToArray(ColorFrameData, ColorImageFormat.Bgra);
				}
				catch (NullReferenceException) { }
			}
		}

		void LoadPixels() {
			_i = 0;

			fixed (Pixel* pixels = SensorDataReadyEventArgs.SensorData) {
				fixed (byte* inputData = ColorFrameData) {
					_pixelPtr = pixels;
					_colorBytePtr = inputData;

					while (_i++ < PixelCount) {
						_pixelPtr->B = *(_colorBytePtr);
						_pixelPtr->G = *(_colorBytePtr + 1);
						_pixelPtr->R = *(_colorBytePtr + 2);

						_colorBytePtr += 4;
						_pixelPtr++;
					}
				}
			}
		}

		void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e) {
			LoadColorFrame(e.FrameReference);
			LoadPixels();
			SensorDataReady.Invoke(this, SensorDataReadyEventArgs);
		}

		void OnSensorAvailabilityChanged(object sender, IsAvailableChangedEventArgs e) => StatusText = Sensor.IsAvailable ? "Running" : "Sensor not available";
	}
}