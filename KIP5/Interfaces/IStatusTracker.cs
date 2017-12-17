using System.ComponentModel;

namespace KIP5.Interfaces {
	interface IStatusTracker {
		event PropertyChangedEventHandler StatusChanged;

		string StatusText { get; }
	}
}