using CommunityToolkit.Mvvm.ComponentModel;

namespace v10.Services.Contracts;

public interface IPageService {
	Type GetPageType(string key);
	void Configure<VM, V>()
		where VM : ObservableObject
		where V : Page;
}
