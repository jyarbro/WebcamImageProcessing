using CommunityToolkit.Mvvm.ComponentModel;

namespace v10.Services.Models;

public class Settings : ObservableObject {
	public string OpenAIApiKey {
		get => _openAIApiKey;
		set => SetProperty(ref _openAIApiKey, value);
	}
	string _openAIApiKey = string.Empty;

	public string Theme {
		get => _theme;
		set => SetProperty(ref _theme, value);
	}
	string _theme = string.Empty;

	public Settings() {
		// Ensures any changes to settings will get saved back to the filesystem.
		PropertyChanged += StateManager.UpdateSettings;
	}
}
