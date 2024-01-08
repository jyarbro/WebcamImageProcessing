using v9.Core.ViewModels;

namespace v9.Views;

public sealed partial class SettingsPage : Page {
	public SettingsViewModel ViewModel { get; }

	public SettingsPage() {
		ViewModel = App.GetService<SettingsViewModel>();
		InitializeComponent();
	}

	async void CopySettingsPathToClipboard_ButtonClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
		ViewModel.CopySettingsPathToClipboard();

		await Task.Delay(900);
		((Button) sender).Flyout.Hide();
	}
}
