using CommunityToolkit.Mvvm.ComponentModel;
using v8.Core.ImageProcessors;
using v8.Helpers;

namespace v8.ViewModels;

public class MainViewModel : ObservableRecipient {
	public List<ImageProcessorSelector> ImageProcessors { get; set; } = new List<ImageProcessorSelector> {
			new ImageProcessorSelector {
				Title = "Color Camera",
				ImageProcessor = typeof(ColorCameraProcessor)
			},
			new ImageProcessorSelector {
				Title = "Boost Green",
				ImageProcessor = typeof(BoostGreenProcessor)
			},
			new ImageProcessorSelector {
				Title = "Edge Detection",
				ImageProcessor = typeof(EdgeDetectionProcessor)
			}
		};

	public MainViewModel() {
	}
}
