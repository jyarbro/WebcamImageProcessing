using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.Utilities.WinUI;
using v10.Services.Contracts;

namespace v10.ViewModels;

public class MainWindowViewModel : ObservableRecipient {
	public INavigationService NavigationService { get; init; }
	public INavigationViewService NavigationViewService { get; init; }

	public string Title => "AppDisplayName".GetLocalized();

	public object? Selected {
		get => _selected;
		set => SetProperty(ref _selected, value);
	}
	object? _selected;

	public MainWindowViewModel(
		INavigationService navigationService,
		INavigationViewService navigationViewService
	) {
		NavigationService = navigationService;
		NavigationService.Navigated += OnNavigated;

		NavigationViewService = navigationViewService;
	}

	void OnNavigated(object sender, NavigationEventArgs e) {
		if (e.SourcePageType.Name == "SettingsPage") {
			Selected = NavigationViewService.SettingsItem;
			return;
		}

		var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);

		if (selectedItem != null) {
			Selected = selectedItem;
		}
	}
}
