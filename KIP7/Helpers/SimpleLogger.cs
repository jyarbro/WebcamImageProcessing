using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace KIP7.Helpers {
	/// <summary>
	/// A simple logger to display text to TextBlock asynchronisely.
	/// </summary>
	public class SimpleLogger {
		CoreDispatcher Dispatcher;
		TextBlock TextBlock;
		string _messageText = string.Empty;
		readonly object _messageLock = new object();
		int _messageCount;

		public SimpleLogger(TextBlock textBlock) {
			TextBlock = textBlock;
			Dispatcher = TextBlock.Dispatcher;
		}

		/// <summary>
		/// Logs a message to be displayed.
		/// </summary>
		internal async void Log(string message) {
			var newMessage = $"[{_messageCount++}] {DateTime.Now.ToString("hh:MM:ss")} : {message}\n{_messageText}";

			lock (_messageLock) {
				_messageText = newMessage;
			}

			await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
				lock (_messageLock) {
					TextBlock.Text = _messageText;
				}
			});
		}
	}
}
