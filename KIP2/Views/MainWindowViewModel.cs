using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIP2.Helpers;
using KIP2.Models;
using KIP2.Models.DepthProcessors;
using KIP2.Models.ImageProcessors;

namespace KIP2.Views {
	public class MainWindowViewModel : Observable {
		public string StatusText {
			get { return _StatusText ?? (_StatusText = string.Empty); }
			set { SetProperty(ref _StatusText, value); }
		}
		string _StatusText;

		public double FramesPerSecond {
			get { return _FramesPerSecond; }
			set { SetProperty(ref _FramesPerSecond, value); }
		}
		double _FramesPerSecond = 0;

		public double FrameLag {
			get { return _FrameLag; }
			set { SetProperty(ref _FrameLag, value); }
		}
		double _FrameLag = 0;

		public WriteableBitmap OutputImage {
			get { return _OutputImage; }
			set { SetProperty(ref _OutputImage, value); }
		}
		WriteableBitmap _OutputImage;

		public StreamManager StreamManager { get; set; }

		public List<string> ImageProcessorNames { get; set; }
		public List<string> DepthProcessorNames { get; set; }

		public int SelectedImageProcessorIndex {
			get { return _SelectedImageProcessorIndex; }
			set { SetProperty(ref _SelectedImageProcessorIndex, value); }
		}
		int _SelectedImageProcessorIndex;

		public int SelectedDepthProcessorIndex {
			get { return _SelectedDepthProcessorIndex; }
			set { SetProperty(ref _SelectedDepthProcessorIndex, value); }
		}
		int _SelectedDepthProcessorIndex;

		public string SelectedImageProcessorName {
			get { return _SelectedImageProcessorName; }
			set {
				if (value == _SelectedImageProcessorName)
					return;

				_SelectedImageProcessorName = value;
				
				if (StreamManager != null) {
					var processorType = Type.GetType("KIP2.Models.ImageProcessors." + _SelectedImageProcessorName + ", KIP2");
					var processorInstance = (ImageProcessorBase)Activator.CreateInstance(processorType);

					StreamManager.ImageProcessor = processorInstance;
				}
			}
		}
		string _SelectedImageProcessorName;

		public string SelectedDepthProcessorName {
			get { return _SelectedDepthProcessorName; }
			set {
				if (value == _SelectedDepthProcessorName)
					return;

				_SelectedDepthProcessorName = value;

				if (StreamManager != null) {
					var processorType = Type.GetType("KIP2.Models.DepthProcessors." + _SelectedDepthProcessorName + ", KIP2");
					var processorInstance = (DepthProcessorBase)Activator.CreateInstance(processorType);

					StreamManager.DepthProcessor = processorInstance;
				}
			}
		}
		string _SelectedDepthProcessorName;

		public MainWindowViewModel() {
			OutputImage = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);

			StreamManager = new StreamManager {
				FilteredImage = OutputImage
			};

			ImageProcessorNames = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType.Equals(typeof(ImageProcessorBase))).Select(t => t.Name).ToList();
			DepthProcessorNames = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType.Equals(typeof(DepthProcessorBase))).Select(t => t.Name).ToList();

			SelectedImageProcessorName = ImageProcessorNames.First();
			SelectedImageProcessorIndex = ImageProcessorNames.IndexOf(SelectedImageProcessorName);

			SelectedDepthProcessorName = DepthProcessorNames.First();
			SelectedDepthProcessorIndex = DepthProcessorNames.IndexOf(SelectedDepthProcessorName);

			StreamManager.UpdateFrameRate += UpdateFrameRate;
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}