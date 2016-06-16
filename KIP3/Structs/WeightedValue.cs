namespace KIP3.Models {
	public struct WeightedValue<T> {
		public int Weight { get; set; }
		public T Value { get; set; }
	}
}