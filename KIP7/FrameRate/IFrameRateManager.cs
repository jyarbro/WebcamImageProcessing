using System;

namespace KIP7.FrameRate {
	public interface IFrameRateManager {
		event EventHandler<FrameRateEventArgs> FrameRateUpdated;

		void Increment(long elapsedMilliseconds);
	}
}