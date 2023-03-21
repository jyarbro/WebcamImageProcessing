namespace v8.Contracts.Services;

public interface IThemeSelectorService {
	ElementTheme Theme { get; }

	void LoadTheme();
	void SetTheme(ElementTheme theme);
}
