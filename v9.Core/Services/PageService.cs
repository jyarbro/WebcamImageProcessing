﻿using CommunityToolkit.Mvvm.ComponentModel;
using v9.Core.Contracts.Services;

namespace v9.Core.Services;

public class PageService : IPageService {
	readonly Dictionary<string, Type> _pages = [];

	public Type GetPageType(string key) {
		Type? pageType;
		lock (_pages) {
			if (!_pages.TryGetValue(key, out pageType)) {
				throw new ArgumentException($"Page not found: {key}. Did you forget to call {nameof(PageService)}.{nameof(Configure)}?");
			}
		}

		return pageType;
	}

	public void Configure<VM, V>()
		where VM : ObservableObject
		where V : Page {

		lock (_pages) {
			var key = typeof(VM).FullName!;
			if (_pages.ContainsKey(key)) {
				throw new ArgumentException($"The key {key} is already configured in {nameof(PageService)}");
			}

			var type = typeof(V);
			if (_pages.Any(p => p.Value == type)) {
				throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
			}

			_pages.Add(key, type);
		}
	}
}
