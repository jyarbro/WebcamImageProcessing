﻿using Microsoft.UI.Xaml.Navigation;

namespace v9.Core.Contracts.Services;

public interface INavigationService {
	event NavigatedEventHandler Navigated;

	bool CanGoBack {
		get;
	}

	Frame? Frame {
		get; set;
	}

	bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

	bool GoBack();
}
