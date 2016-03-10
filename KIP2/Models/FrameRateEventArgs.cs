using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIP2.Models {
	public class FrameRateEventArgs : EventArgs {
		public double FramesPerSecond { get; set; }
		public double FrameLag { get; set; }
	}
}
