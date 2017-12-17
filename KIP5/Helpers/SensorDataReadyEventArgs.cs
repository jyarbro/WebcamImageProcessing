using KIP.Structs;
using System;

namespace KIP5.Helpers {
	public class SensorDataReadyEventArgs : EventArgs {
		public Pixel[] SensorData;
	}
}