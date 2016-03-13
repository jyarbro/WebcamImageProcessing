using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KIP2.Helpers;
using KIP2.Models;
using KIP2.Models.ImageProcessors;

namespace KIP2.ViewModels {
	public class VisualSensorViewModel : ViewModelBase {
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

		public VisualSensorManager VisualSensorManager { get; set; }

		public List<string> ProcessorNames { get; set; }

		public int SelectedProcessorIndex {
			get { return _SelectedProcessorIndex; }
			set { SetProperty(ref _SelectedProcessorIndex, value); }
		}
		int _SelectedProcessorIndex;

		public string SelectedProcessorName {
			get { return _SelectedProcessorName; }
			set {
				if (value == _SelectedProcessorName)
					return;

				_SelectedProcessorName = value;
				
				if (VisualSensorManager != null) {
					var processorType = Type.GetType("KIP2.Models.ImageProcessors." + _SelectedProcessorName + ", KIP2");
					var processorInstance = (ImageProcessorBase)Activator.CreateInstance(processorType);

					VisualSensorManager.ImageProcessor = processorInstance;
				}
			}
		}
		string _SelectedProcessorName;

		public VisualSensorViewModel() {
			OutputImage = new WriteableBitmap(640, 480, 96.0, 96.0, PixelFormats.Bgr32, null);

			VisualSensorManager = new VisualSensorManager {
				FilteredImage = OutputImage
			};

			ProcessorNames = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.BaseType.Equals(typeof(ImageProcessorBase))).Select(t => t.Name).ToList();

			SelectedProcessorName = ProcessorNames.First();
			SelectedProcessorIndex = ProcessorNames.IndexOf(SelectedProcessorName);

			VisualSensorManager.UpdateFrameRate += UpdateFrameRate;
		}

		void UpdateFrameRate(object sender, FrameRateEventArgs args) {
			FramesPerSecond = args.FramesPerSecond;
			FrameLag = args.FrameLag;
		}
	}
}