namespace v10.ViewModels.Contracts;

public interface INavigationAware {
	void OnNavigatedTo(object parameter);
	void OnNavigatedFrom();
}
