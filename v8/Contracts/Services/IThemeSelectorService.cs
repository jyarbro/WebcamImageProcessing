using Microsoft.UI.Xaml;

namespace v8.Contracts.Services;

public interface IThemeSelectorService {
	ElementTheme Theme {
		get;
	}

	Task InitializeAsync();

	Task SetThemeAsync(ElementTheme theme);

	Task SetRequestedThemeAsync();
}
