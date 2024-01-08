using Microsoft.Extensions.Options;
using Microsoft.UI.Composition.SystemBackdrops;
using Nrrdio.Utilities.WinUI;
using WinRT;
using v9.Core.Contracts.Services;
using v9.Core.Models;

namespace v9.Helpers;

public class ThemeSelectorService : IThemeSelectorService {
	Settings Settings { get; init; }

	DispatcherQueueHelper? DispatcherQueueHelper;
	MicaController? BackdropController;
	SystemBackdropConfiguration? ConfigurationSource;

	public ElementTheme Theme {
		get {
			if (Enum.TryParse(Settings.Theme, out ElementTheme cacheTheme)) {
				return cacheTheme;
			}

			return ElementTheme.Default;
		}
	}

	public ThemeSelectorService(
		IOptionsMonitor<Settings> settings
	) {
		Settings = settings.CurrentValue;
	}

	public void LoadTheme() {
		SetTheme(Theme);
		SetSystemBackdrop();
	}

	public void SetTheme(ElementTheme theme) {
		Settings.Theme = theme.ToString();
		((FrameworkElement) App.MainWindow.Content).RequestedTheme = theme;
	}

	void SetSystemBackdrop() {
		if (MicaController.IsSupported()) {
			DispatcherQueueHelper = new DispatcherQueueHelper();
			DispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();

			ConfigurationSource = new SystemBackdropConfiguration();

			App.MainWindow.Activated += Window_Activated;
			App.MainWindow.Closed += Window_Closed;

			((FrameworkElement) App.MainWindow.Content).ActualThemeChanged += Window_ThemeChanged;

			ConfigurationSource.IsInputActive = true;
			SetConfigurationSourceTheme();

			BackdropController = new MicaController {
				Kind = MicaKind.BaseAlt
			};

			BackdropController.AddSystemBackdropTarget(App.MainWindow.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
			BackdropController.SetSystemBackdropConfiguration(ConfigurationSource);
		}
	}

	void SetConfigurationSourceTheme() {
		if (ConfigurationSource is not null) {
			ConfigurationSource.Theme = ((FrameworkElement) App.MainWindow.Content).ActualTheme switch {
				ElementTheme.Dark => SystemBackdropTheme.Dark,
				ElementTheme.Light => SystemBackdropTheme.Light,
				ElementTheme.Default => SystemBackdropTheme.Default,
				_ => throw new ArgumentException("Unsupported theme")
			};
		}
	}

	void Window_Activated(object sender, WindowActivatedEventArgs args) {
		if (ConfigurationSource is not null) {
			ConfigurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
		}
	}

	void Window_Closed(object sender, WindowEventArgs args) {
		BackdropController?.Dispose();
		App.MainWindow.Activated -= Window_Activated;
	}

	void Window_ThemeChanged(FrameworkElement sender, object args) {
		SetConfigurationSourceTheme();
	}
}
