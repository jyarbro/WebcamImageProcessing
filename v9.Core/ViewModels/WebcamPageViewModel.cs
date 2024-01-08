using CommunityToolkit.Mvvm.ComponentModel;
using v9.Core.ImageProcessors;

namespace v9.Core.ViewModels;

public class WebcamPageViewModel : ObservableRecipient {
	public List<Selection> Processors { get; set; } = new List<Selection> {
			new Selection {
				Title = "Color Camera",
				Processor = typeof(ColorCameraProcessor)
			},
			new Selection {
				Title = "Boost Green",
				Processor = typeof(BoostGreenProcessor)
			},
			new Selection {
				Title = "Edge Detection",
				Processor = typeof(EdgeDetectionProcessor)
			},
			new Selection {
				Title = "TEST TEST TEST",
				Processor = typeof(WebcamProcessor)
			}
		};

	public class Selection {
		public string? Title;
		public Type? Processor { get; set; }
	}
}
