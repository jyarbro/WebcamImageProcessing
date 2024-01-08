using CommunityToolkit.Mvvm.ComponentModel;

namespace v9.Core.Contracts.Services;

public interface IPageService {
	Type GetPageType(string key);
	void Configure<VM, V>() 
		where VM : ObservableObject
		where V : Page;
}
