using Microsoft.UI.Windowing;
using v9.Core.ViewModels;

namespace v9;

public sealed partial class MainWindow : Window {
	const int DEFAULT_WIDTH = 1000;
	const int DEFAULT_HEIGHT = 800;
	readonly nint windowHandle;

	MainWindowViewModel ViewModel { get; init; }

	public MainWindow() {
		ViewModel = App.GetService<MainWindowViewModel>();

		InitializeComponent();

		windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

		SetWindowSize(DEFAULT_WIDTH, DEFAULT_HEIGHT);
		CenterWindow();

		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);

		ViewModel.NavigationService.Frame = NavigationFrame;
		ViewModel.NavigationViewService.Initialize(NavigationViewControl);
	}

	void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args) {
		AppTitleBar.Margin = new Thickness() {
			Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
			Top = AppTitleBar.Margin.Top,
			Right = AppTitleBar.Margin.Right,
			Bottom = AppTitleBar.Margin.Bottom
		};

	}

	/// <summary>
	/// Source: https://github.com/microsoft/WinUI-3-Demos/blob/master/src/Build2020Demo/DemoBuildCs/DemoBuildCs/DemoBuildCs/App.xaml.cs#L28
	/// </summary>
	void SetWindowSize(int width, int height) {
		var dpi = PInvoke.User32.GetDpiForWindow(windowHandle);
		var scalingFactor = (float) dpi / 96;

		width = (int) (width * scalingFactor);
		height = (int) (height * scalingFactor);

		PInvoke.User32.SetWindowPos(windowHandle, PInvoke.User32.SpecialWindowHandles.HWND_TOP, 0, 0, width, height, PInvoke.User32.SetWindowPosFlags.SWP_NOMOVE);
	}

	/// <summary>
	/// Source: https://stackoverflow.com/a/71730765/2621693
	/// </summary>
	void CenterWindow() {
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
		var appWindow = AppWindow.GetFromWindowId(windowId);

		if (appWindow is not null) {
			var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);

			if (displayArea is not null) {
				var CenteredPosition = appWindow.Position;
				CenteredPosition.X = ((displayArea.WorkArea.Width - appWindow.Size.Width) / 2);
				CenteredPosition.Y = ((displayArea.WorkArea.Height - appWindow.Size.Height) / 2);
				appWindow.Move(CenteredPosition);
			}
		}
	}
}