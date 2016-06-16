using System;

namespace KIP3.Helpers {
	[AttributeUsage(AttributeTargets.Method)]
	public class NotifyPropertyChangedActionAttribute : Attribute {
		public NotifyPropertyChangedActionAttribute() { }
		public NotifyPropertyChangedActionAttribute(string parameterName) {
			ParameterName = parameterName;
		}

		public string ParameterName { get; private set; }
	}
}
