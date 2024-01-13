﻿namespace v9.Core.Helpers;

public static class FrameExtensions {
	public static object? GetPageViewModel(this Frame frame) => frame?.Content?.GetType().GetProperty("ViewModel")?.GetValue(frame.Content, null);
}