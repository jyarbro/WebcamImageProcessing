using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using v8.Contracts.Services;
using v8.Core.Contracts.Services;
using v8.Core.ImageFilters;
using v8.Core.Services;
using v8.Core.Services.FrameRate;
using v8.Core.Services.Logger;
using v8.Services;
using v8.ViewModels;
using v8.Views;

namespace v8;

public partial class App : Application {
	public static Window MainWindow { get; } = new MainWindow();

	public IHost Host { get; }

	public App() {
		InitializeComponent();

		Host = Microsoft.Extensions.Hosting.Host.
			CreateDefaultBuilder().
			UseContentRoot(AppContext.BaseDirectory).
			ConfigureAppConfiguration((context, builder) => {
				builder.Sources.Clear();
				StateManager.EnsureSettings();
				builder.AddJsonFile(StateManager.SettingsPath, false, true);
				builder.AddEnvironmentVariables();
			}).
			ConfigureServices(ConfigureServices).
			Build();

		InitializeServices();

		UnhandledException += UnhandledExceptionEventHandler;
	}

	public static T GetService<T>()
		where T : class {

		if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service) {
			throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
		}

		return service;
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args) {
		base.OnLaunched(args);

		GetService<IThemeSelectorService>().LoadTheme();

		MainWindow.Activate();

		GetService<INavigationService>().NavigateTo(typeof(WebcamPageViewModel).FullName!, args.Arguments);
	}

	void InitializeServices() {
	}

	void ConfigureServices(HostBuilderContext context, IServiceCollection services) {
		// Services
		services.AddScoped<IPageService, PageService>();
		services.AddScoped<IThemeSelectorService, ThemeSelectorService>();
		services.AddScoped<INavigationViewService, NavigationViewService>();
		services.AddScoped<INavigationService, NavigationService>();
		services.AddScoped<IFileService, FileService>();
		services.AddScoped<Core.Services.Logger.ILogger, SimpleLogger>(); // TODO JY Replace this logger with nrrdio.utils
		services.AddTransient<IFrameRateManager, FrameRateManager>();

		// Pages, Frames & ViewModels
		services.AddTransient<MainWindowViewModel>();

		services.AddTransient<SettingsPage>();
		services.AddTransient<SettingsViewModel>();

		services.AddTransient<WebcamPage>();
		services.AddTransient<WebcamPageViewModel>();

		services.AddTransient<ProcessedWebcamFrame>();
		services.AddTransient<ProcessedWebcamFrameViewModel>();

		// Filters
		services.AddTransient<GreenBooster>();
	}

	void UnhandledExceptionEventHandler(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
		// TODO: Log and handle exceptions as appropriate.
	}
}
