﻿using System.ComponentModel;
using System.Text;
using System.Text.Json;
using v9.Core.Models;
using Windows.Storage;

namespace v9.Core.Services;

public class StateManager {
	public static string SettingsPath { get; } = Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");

	public static void UpdateSettings(object? sender, PropertyChangedEventArgs e) {
		if (sender is Settings settings) {
			WriteSettings(settings);
		}
	}

	public static void EnsureSettings() {
		if (!File.Exists(SettingsPath)) {
			WriteSettings(new Settings());
		}
	}

	public static void WriteSettings(Settings settings) {
		var fileContent = JsonSerializer.Serialize(new { Settings = settings }, DefaultSerializerOptions);
		File.WriteAllText(SettingsPath, fileContent, Encoding.UTF8);
	}

	public static JsonSerializerOptions DefaultSerializerOptions => new() { WriteIndented = true };
}
