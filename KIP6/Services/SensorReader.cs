using KIP.Structs;
using KIP6.Helpers;
using Microsoft.Kinect;
using System;

namespace KIP6.Services {
	public unsafe class SensorReader {
		public event EventHandler<SensorDataReadyEventArgs> SensorDataReady;

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
	}
}