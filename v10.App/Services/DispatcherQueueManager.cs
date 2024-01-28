using Microsoft.UI.Dispatching;
using v10.Contracts.Services;

namespace v10.Services;

public class DispatcherQueueManager : IDispatcherQueueManager {
	// This might need to be a collection in the future.
	public DispatcherQueue? Current { get; set; }
}
