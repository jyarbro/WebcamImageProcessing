using CommunityToolkit.Mvvm.ComponentModel;
using v8.Core.ImageProcessors;
using v8.Helpers;

namespace v8.ViewModels;

public class WebcamPageViewModel : ObservableRecipient {
	public List<WebcamProcessorSelector> Processors { get; set; } = new List<WebcamProcessorSelector> {
			new WebcamProcessorSelector {
				Title = "Color Camera",
				Processor = typeof(ColorCameraProcessor)
			},
			new WebcamProcessorSelector {
				Title = "Boost Green",
				Processor = typeof(BoostGreenProcessor)
			},
			new WebcamProcessorSelector {
				Title = "Edge Detection",
				Processor = typeof(EdgeDetectionProcessor)
			}
		};

	public WebcamPageViewModel() {
	}
}

public class WebcamProcessorSelector {
	public string? Title;
	public Type? Processor { get; set; }
}
