using CommunityToolkit.Mvvm.ComponentModel;

namespace v10.Contracts.Services;

public interface IPageService {
	Type GetPageType(string key);
	void Configure<VM, V>()
		where VM : ObservableObject
		where V : Page;
}
