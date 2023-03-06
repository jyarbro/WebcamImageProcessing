namespace v8.Core.Services.FrameRate;

public class FrameRateEventArgs : EventArgs {
	public double FramesPerSecond { get; set; }
	public double FrameLag { get; set; }
}
