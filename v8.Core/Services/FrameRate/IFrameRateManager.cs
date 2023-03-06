namespace v8.Core.Services.FrameRate;

public interface IFrameRateManager {
	event EventHandler<FrameRateEventArgs> FrameRateUpdated;

	void Increment(long elapsedMilliseconds);
}