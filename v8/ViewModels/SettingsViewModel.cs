using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Nrrdio.Utilities.WinUI;
using v8.Contracts.Services;
using v8.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace v8.ViewModels;

public class SettingsViewModel : ObservableRecipient {
	public ElementTheme Theme => ThemeSelectorService.Theme;

	public string VersionDescription {
		get => _versionDescription;
		set => SetProperty(ref _versionDescription, value);
	}
	string _versionDescription;

	public string LocalSettingsFolder {
		get => GetLocalSettingsFolderShort();
	}

	public ICommand SwitchThemeCommand { get; init; }
	public Settings Settings { get; init; }

	IThemeSelectorService ThemeSelectorService { get; init; }

	public SettingsViewModel(
		IThemeSelectorService themeSelectorService,
		IOptionsMonitor<Settings> settings
	) {
		ThemeSelectorService = themeSelectorService;
		Settings = settings.CurrentValue;

		_versionDescription = GetVersionDescription();

		SwitchThemeCommand = new RelayCommand<ElementTheme>(
			(param) => {
				ThemeSelectorService.SetTheme(param);
			});
	}

	public void CopySettingsPathToClipboard() {
		var dataPackage = new DataPackage();
		dataPackage.SetText(ApplicationData.Current.LocalFolder.Path);
		Clipboard.SetContent(dataPackage);
	}

	public void UpdateOpenAIApiKey(string value) => Settings.OpenAIApiKey = value;

	static string GetVersionDescription() {
		var version = Assembly.GetExecutingAssembly().GetName().Version!;
		return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
	}

	static string GetLocalSettingsFolderShort() {
		var path = ApplicationData.Current.LocalFolder.Path;

		if (path.Length > 48) {
			var front = path.Substring(0, 24);
			var back = path.Substring(path.Length - 24, 24);

			return $"{front}...{back}";
		}

		return path;
	}
}
