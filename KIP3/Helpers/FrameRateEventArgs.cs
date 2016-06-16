using System;

namespace KIP3.Helpers {
	public class FrameRateEventArgs : EventArgs {
		public double FramesPerSecond { get; set; }
		public double FrameLag { get; set; }
	}
}
