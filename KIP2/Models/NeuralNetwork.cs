using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIP2.Models {
	public class NeuralNetwork {
		public List<byte[]> Neurons { get; set; }

		public NeuralNetwork() {
			Neurons = new List<byte[]>();
		}

		public void Receive(List<byte[]> segmentedInputArray) {

		}
	}
}
