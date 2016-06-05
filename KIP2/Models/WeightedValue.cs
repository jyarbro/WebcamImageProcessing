using System;
using System.Collections.Generic;

namespace KIP2.Models {
	public class WeightedValue<T> {
		public int Weight { get; set; }
		public T Value { get; set; }
	}

	public static class WeightedValueExtension {
		public static WeightedValue<T> Create<T>(int weight, T value) {
			return new WeightedValue<T> { Weight = weight, Value = value };
		}

		static Random random = new Random();

		public static T WeightedRandom<T>(this IEnumerable<WeightedValue<T>> enumerable) {
			int totalWeight = 0;

			T selected = default(T);

			foreach (var data in enumerable) {
				int r = random.Next(totalWeight + data.Weight);

				if (r >= totalWeight)
					selected = data.Value;

				totalWeight += data.Weight;
			}

			return selected;
		}
	}

	// greatly inspired by https://programmers.stackexchange.com/questions/150616/return-random-list-item-by-its-weight
}
