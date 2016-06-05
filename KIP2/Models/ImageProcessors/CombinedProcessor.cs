using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIP2.Models.ImageProcessors {
	public class CombinedProcessor : ImageProcessorBase {
		public CombinedProcessor() : base() { }

		public override byte[] ProcessImage() {
			PrepareOutput();

			FocalPoint = GetNearestFocalPoint(ImageMid);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(1);

			FocalPoint = GetBrightestFocalPoint(FocalPoint);
			FocalPointOffset = ((FocalPoint.Y * ImageMax.X) + FocalPoint.X) * 4;

			OverlayFocalPoint(3);

			return OutputArray;
		}
	}
}
