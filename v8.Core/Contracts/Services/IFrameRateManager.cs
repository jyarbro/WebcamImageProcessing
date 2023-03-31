using v8.Core.Services.FrameRate;

namespace v8.Core.Contracts.Services;

public interface IFrameRateManager {
	event EventHandler<FrameRateEventArgs> FrameRateUpdated;

	void Increment(long elapsedMilliseconds);
}