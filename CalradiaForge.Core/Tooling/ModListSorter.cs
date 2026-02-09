namespace CalradiaForge.Core.Tooling {
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Text;

	public class ModListSorter {

		public static void LoadOrderSort<T>(ObservableCollection<T> collection,Comparison<T> comparison) {
			List<T> list = new List<T>(collection);
			list.Sort(comparison);
			for (int i = 0; i < list.Count; i++) {
				collection.Move(collection.IndexOf(list[i]),i);
			}
		}
	}
}
