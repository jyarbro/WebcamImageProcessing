using System;

namespace KIP7.Logger {
	public interface ILogger {
		event EventHandler<LogEventArgs> MessageLoggedEvent;

		void Log(string message);
	}
}