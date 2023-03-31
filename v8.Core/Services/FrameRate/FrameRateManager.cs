using v8.Core.Contracts.Services;

namespace v8.Core.Services.FrameRate;

public class FrameRateManager : IFrameRateManager {
	// Too low will calculate framerate too often.
	// Too high and it becomes hard to pinpoint issues.
	const int FRAMERATE_DELAY = 250;

	public event EventHandler<FrameRateEventArgs> FrameRateUpdated;

	double FrameCount;
	double FrameDuration;
	DateTime FrameRunTimer = DateTime.Now;
	DateTime FrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);

	public void Increment(long elapsedMilliseconds) {
		FrameDuration += elapsedMilliseconds;
		FrameCount++;

		if (FrameTimer < DateTime.Now) {
			FrameTimer = DateTime.Now.AddMilliseconds(FRAMERATE_DELAY);
			UpdateFrameRate();
		}
	}

	void UpdateFrameRate() {
		var totalSeconds = (DateTime.Now - FrameRunTimer).TotalSeconds;

		FrameRateUpdated(this, new FrameRateEventArgs {
			FramesPerSecond = Math.Round(FrameCount / totalSeconds),
			FrameLag = Math.Round(FrameDuration / FrameCount, 2)
		});

		if (totalSeconds > 5) {
			FrameCount = 0;
			FrameDuration = 0;
			FrameRunTimer = DateTime.Now;
		}
	}
}
