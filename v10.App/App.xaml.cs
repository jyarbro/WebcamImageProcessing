using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nrrdio.Utilities.Loggers;
using Nrrdio.Utilities.WinUI.FrameRate;
using v10.Helpers;
using v10.Views;
using v10.Services;
using v10.ViewModels;
using v10.Services.Contracts;
using v10.ImageFilters.Helpers;

namespace v10;

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
			ConfigureLogging(builder =>
				builder.
					ClearProviders().
					AddProvider(
						new HandlerLoggerProvider {
							LogLevel = LogLevel.Information
						}
					)
			).
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

	public static object GetService(Type t) {
		var service = (Current as App)!.Host.Services.GetService(t);

		if (service is null) {
			throw new ArgumentException($"{t} needs to be registered in {nameof(ConfigureServices)} within App.xaml.cs.");
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

			return service;
		});

		services.AddScoped<IFrameRateHandler, FrameRateHandler>();

		services.AddScoped<IThemeSelectorService, ThemeSelectorService>();
		services.AddScoped<INavigationViewService, NavigationViewService>();
		services.AddScoped<INavigationService, NavigationService>();
		services.AddScoped<IFileService, FileService>();

		services.AddTransient<WebcamProcessor>();

		foreach (var imageFilterClass in ImageFilterLoader.GetList()) {
			services.AddTransient(imageFilterClass);
		}

		// Pages, Frames & ViewModels
		services.AddTransient<MainWindowViewModel>();

		services.AddTransient<FilePage>();
		services.AddTransient<FilePageViewModel>();

		services.AddTransient<WebcamPage>();
		services.AddTransient<WebcamPageViewModel>();
	}

	void UnhandledExceptionEventHandler(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) {
		// TODO: Log and handle exceptions as appropriate.
	}
}
