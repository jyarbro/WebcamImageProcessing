namespace v9.Core.Contracts.ViewModels;

public interface INavigationAware {
	void OnNavigatedTo(object parameter);

	void OnNavigatedFrom();
}
