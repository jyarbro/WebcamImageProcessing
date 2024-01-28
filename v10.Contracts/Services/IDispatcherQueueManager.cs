using Microsoft.UI.Dispatching;

namespace v10.Contracts.Services;

public interface IDispatcherQueueManager {
	DispatcherQueue? Current { get; set; }
}
