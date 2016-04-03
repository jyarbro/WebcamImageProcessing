using System;

namespace KIP2.Helpers {
	public class FrameRateEventArgs : EventArgs {
		public double FramesPerSecond { get; set; }
		public double FrameLag { get; set; }
	}
}
