using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KIP.Helpers {
	public class Observable : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null) {
			if (Equals(member, val))
				return;

			member = val;

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}