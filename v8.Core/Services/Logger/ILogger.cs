namespace v8.Core.Services.Logger;

public interface ILogger {
	event EventHandler<LogEventArgs> MessageLoggedEvent;

	void Log(string message);
}