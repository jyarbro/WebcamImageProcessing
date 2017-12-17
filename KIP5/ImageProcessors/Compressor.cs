using KIP.Structs;
using KIP5.Services;
using System.Collections.Generic;
using System.Linq;

namespace KIP5.ImageProcessors {
	//unsafe class Compressor : ImageProcessor {
	//	int[] Offsets;
		
	//	int _i;
	//	Pixel _pixel;
	//	int _pixelValue;

	//	public Compressor(SensorReader sensorReader) : base(sensorReader) {
	//		CalculateOffsets();
	//	}

	//	protected override void ApplyFilters(Pixel[] sensorData) {
	//		fixed (byte* outputData = OutputData) {
	//			var outputBytePtr = outputData;
	//			_i = 0;

	//			while (_i++ < PixelCount) {
	//				var totalEffectiveValue = 0;

	//				for (var weightsIndex = 0; weightsIndex < Offsets.Length; weightsIndex++) {
	//					_j = _i + Offsets[weightsIndex];

	//					if (_j < 0 || _j >= PixelCount)
	//						continue;

	//					_pixel = sensorData[_j];
	//					_pixelValue = (_pixel.B + _pixel.G + _pixel.R) * Weights[weightsIndex];

	//					totalEffectiveValue += _pixelValue;
	//				}

	//				if (totalEffectiveValue > THRESHOLD) {
	//					*(outputBytePtr) = 0;
	//					*(outputBytePtr + 1) = 0;
	//					*(outputBytePtr + 2) = 0;
	//				}
	//				else {
	//					*(outputBytePtr) = 255;
	//					*(outputBytePtr + 1) = 255;
	//					*(outputBytePtr + 2) = 255;
	//				}

	//				outputBytePtr += 4;
	//			}
	//		}
	//	}

	//	void CalculateOffsets() {
	//		var edgeFilterWeights = new List<int> {
	//			-1, -1, -1,
	//			-1,  8, -1,
	//			-1, -1, -1,
	//		};

	//		var areaBox = new Rectangle {
	//			Origin = new Point { X = -2, Y = -2 },
	//			Extent = new Point { X = 2, Y = 2 },
	//		};

	//		var edgeFilterOffsets = CalculateOffsets(areaBox, 25, FrameWidth);

	//		var filteredPixelCount = edgeFilterWeights.Where(f => f != 0).Count();

	//		Offsets = new int[filteredPixelCount];
	//		Weights = new int[filteredPixelCount];

	//		var j = 0;

	//		for (var i = 0; i < edgeFilterWeights.Count; i++) {
	//			if (edgeFilterWeights[i] == 0)
	//				continue;

	//			Weights[j] = edgeFilterWeights[i];
	//			Offsets[j] = edgeFilterOffsets[i] * 4;

	//			j++;
	//		}
	//	}
	//}
}