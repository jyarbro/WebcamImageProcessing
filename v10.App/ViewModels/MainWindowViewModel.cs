using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Navigation;
using Nrrdio.Utilities.WinUI;
using v10.Contracts.Services;

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
		var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);

		if (selectedItem != null) {
			Selected = selectedItem;
		}
	}
}
