using v9.Core.ViewModels;

namespace v10.Views;

public sealed partial class FilePage : Page {
	public FilePageViewModel ViewModel { get; private init; }
	
	public FilePage() {
		ViewModel = App.GetService<FilePageViewModel>();
		InitializeComponent();
	}
}
