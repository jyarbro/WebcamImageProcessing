using System;

namespace KIP2.Annotations {
	[AttributeUsage(AttributeTargets.Method)]
	public class NotifyPropertyChangedActionAttribute : Attribute {
		public NotifyPropertyChangedActionAttribute() { }
		public NotifyPropertyChangedActionAttribute(string parameterName) {
			ParameterName = parameterName;
		}

		public string ParameterName { get; private set; }
	}
}
