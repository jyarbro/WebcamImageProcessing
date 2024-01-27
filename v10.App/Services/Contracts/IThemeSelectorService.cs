namespace v10.Services.Contracts;

public interface IThemeSelectorService {
	ElementTheme Theme { get; }

	void LoadTheme();
	void SetTheme(ElementTheme theme);
}
