using KIP.Structs;
using System;

namespace KIP6.Helpers {
	public class SensorDataReadyEventArgs : EventArgs {
		public Pixel[] SensorData;
	}
}