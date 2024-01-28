﻿using Microsoft.UI.Dispatching;
using v10.Contracts.Services;

namespace v10.Services;

public class DispatcherQueueManager : IDispatcherQueueManager {
	// This might need to be a collection in the future.
	public DispatcherQueue Current { 
		get {
			if (_Current is null) {
				throw new Exception($"First set {nameof(Current)} from the current page.");
			}

			return _Current;
		}
		set => _Current = value;
	}
	DispatcherQueue? _Current;
}