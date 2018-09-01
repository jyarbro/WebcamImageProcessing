using System;

namespace KIP7.FrameRate {
	public class FrameRateEventArgs : EventArgs {
		public double FramesPerSecond { get; set; }
		public double FrameLag { get; set; }
	}
}
