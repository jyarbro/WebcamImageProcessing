using System.Collections.Generic;

namespace KIP2.Models {
	public class Neuron {
		public int signature;
		public object Memory;
		public Dictionary<Neuron, int> Weights;
	}
}
