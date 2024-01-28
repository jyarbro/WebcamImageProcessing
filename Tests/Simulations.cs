namespace Tests;

[TestClass]
public class Simulations {
	[TestMethod]
	public void HowLongDoesRandomTake() {
		var random = new Random();

		var timer = new Stopwatch();
		timer.Start();

		for (int i = 0; i < 10000; i++) {
			_ = random.Next(0, 255);
		}

		timer.Stop();
		Console.WriteLine($"{timer.ElapsedTicks / 1000f} ms");
	}

	[TestMethod]
	public void Simulation() {
		var random = new Random();
		byte value;

		var totalSimulations = 1000000;
		var simulatorCount = 100;
		var updateFrequency = 1000000;

		var simulators = new Simulator[simulatorCount];

		for (int i = 0; i < simulatorCount; i++) {
			simulators[i] = new Simulator();
		}

		var timer = new Stopwatch();
		timer.Start();

		for (int i = 0; i < totalSimulations; i++) {
			for (int j = 0; j < simulatorCount; j++) {
				if (random.Next(0, updateFrequency) == 1) {
					value = (byte)random.Next(0, 255);
					simulators[j].Tick(value);
				}
			}
		}

		timer.Stop();
		Console.WriteLine($"{timer.ElapsedMilliseconds} ms");
	}

	public class Simulator {
		public byte _Value;
		public byte _Strength;

		public void Tick(byte value = 0) {
			if (_Strength > 0) {
				_Strength--;
			}
			
			if (value > 0) {
				_Value = Convert.ToByte((value + _Value) / 2);

				if (_Strength < 248) {
					_Strength += 8;
				}
			}
		}
	}
}
