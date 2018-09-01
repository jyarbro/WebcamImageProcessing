using System;

namespace KIP7.Logger {
	/// <summary>
	/// A simple logger to display text to TextBlock asynchronisely.
	/// </summary>
	public class SimpleLogger : ILogger {
		public event EventHandler<LogEventArgs> MessageLoggedEvent;

		string _messageText = string.Empty;
		int _messageCount;

		/// <summary>
		/// Logs a message to be displayed.
		/// </summary>
		public void Log(string message) => MessageLoggedEvent(this, new LogEventArgs {
			Message = $"[{_messageCount++}] {DateTime.Now.ToString("hh:MM:ss")} : {message}\n{_messageText}"
		});
	}
}
