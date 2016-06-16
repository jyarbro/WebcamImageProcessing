using System;
using System.Collections.Generic;

namespace KIP3.Models {
	public class NeuralNetwork {
		public List<Neuron> Neurons;

		public int SegmentWidth; // 11
		public int SegmentArea; // 121

		int Index;
		int X;
		int Y;

		public NeuralNetwork() {
			Neurons = new List<Neuron>();
		}

		public void Receive(List<byte[]> segmentedInput) {
			foreach (var segment in segmentedInput) {
				for (Index = 0; Index < SegmentArea; Index++) {
					Y = Index / SegmentWidth;
					X = Index % SegmentWidth;
				}
			}
		}
	}
}
