using System;
using System.Collections.Generic;

namespace KIP3.Models {
	public class NeuralNetwork {
		public List<Neuron> Neurons;

		public NeuralNetwork() {
			Neurons = new List<Neuron>();
		}

		public void Receive(List<byte[]> segmentedInput) {
		}
	}
}
