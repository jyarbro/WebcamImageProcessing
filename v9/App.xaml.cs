using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.WinUI.FrameRate;
using v9.Core.Contracts.Services;
using v9.Core.ImageFilters;
using v9.Core.Services;
using v9.Core.ViewModels;
using v9.Helpers;
using v9.Views;

namespace v9;

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
			ConfigureLogging(builder =>
				builder.
					ClearProviders().
					AddProvider(
						new HandlerLoggerProvider {
							LogLevel = LogLevel.Information
						}
					)
			).
			ConfigureServices(ConfigureServices).
			Build();

		InitializeServices();

		UnhandledException += UnhandledExceptionEventHandler;
	}

	public static T GetService<T>()
		where T : class {

		if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service) {
			throw new ArgumentException($"{typeof(T)} needs to be registered in {nameof(ConfigureServices)} within App.xaml.cs.");
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
		services.AddScoped<IPageService, PageService>((_) => {
			var service = new PageService();

			service.Configure<WebcamPageViewModel, WebcamPage>();
			service.Configure<FilePageViewModel, FilePage>();
			service.Configure<SettingsViewModel, SettingsPage>();

			return service;
		});

		services.AddTransient<IFrameRateHandler, FrameRateHandler>();

		services.AddScoped<IThemeSelectorService, ThemeSelectorService>();
		services.AddScoped<INavigationViewService, NavigationViewService>();
		services.AddScoped<INavigationService, NavigationService>();
		services.AddScoped<IFileService, FileService>();

		// Pages, Frames & ViewModels
		services.AddTransient<MainWindowViewModel>();

		services.AddTransient<FilePage>();
		services.AddTransient<FilePageViewModel>();

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
