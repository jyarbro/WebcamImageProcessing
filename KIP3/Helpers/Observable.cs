using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KIP3.Helpers {
	public class Observable : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedAction]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void SetProperty<T>(ref T member, T val, [CallerMemberName] string propertyName = null) {
			if (Equals(member, val))
				return;

			member = val;

			if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
