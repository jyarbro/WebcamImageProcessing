namespace v10.Contracts.Services;

public interface INavigationViewService {
	IList<object>? MenuItems {
		get;
	}

	void Initialize(NavigationView navigationView);

	void UnregisterEvents();

	NavigationViewItem? GetSelectedItem(Type pageType);
}
