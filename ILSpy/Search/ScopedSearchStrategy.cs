using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.Search
{
	class ScopedSearchStrategy : AbstractSearchStrategy
	{
		public AbstractSearchStrategy Strategy { get; set; }
		public IList<AbstractSearchStrategy> Scope { get; } = new List<AbstractSearchStrategy>();

		public ScopedSearchStrategy()
			: base(null)
		{
		}

		public override void Search(PEFile module, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (Strategy == null)
				return;

			if (IsMatch(module))
				Strategy.Search(module, cancellationToken);
		}

		public override bool IsMatch(PEFile module)
		{
			for (int i = 0; i < Scope.Count; i++)
				if (!Scope[i].IsMatch(module))
					return false;

			return true;
		}

		public override bool IsMatch(SearchResult result)
		{
			for (int i = 0; i < Scope.Count; i++)
				if (!Scope[i].IsMatch(result))
					return false;

			return true;
		}
	}

	class FilteredProducerConsumerCollection<T> : IProducerConsumerCollection<T>
	{
		readonly IProducerConsumerCollection<T> collection;
		readonly Func<T, bool> filter;

		public FilteredProducerConsumerCollection(IProducerConsumerCollection<T> targetCollection, Func<T, bool> filter)
		{
			this.collection = targetCollection;
			this.filter = filter;
		}

		public bool TryAdd(T item)
		{
			return filter(item) && collection.TryAdd(item);
		}

		public void CopyTo(T[] array, int index) => collection.CopyTo(array, index);
		public bool TryTake(out T item) => collection.TryTake(out item);
		public T[] ToArray() => collection.ToArray();
		public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
		public void CopyTo(Array array, int index) => collection.CopyTo(array, index);
		public int Count => collection.Count;
		public object SyncRoot => collection.SyncRoot;
		public bool IsSynchronized => collection.IsSynchronized;
		IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
	}
}
