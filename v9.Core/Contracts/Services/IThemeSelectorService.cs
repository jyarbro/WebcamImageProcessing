namespace v9.Core.Contracts.Services;

public interface IThemeSelectorService {
	ElementTheme Theme { get; }

	void LoadTheme();
	void SetTheme(ElementTheme theme);
}
