using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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
	class NamespaceSearchStrategy : AbstractSearchStrategy
	{
		public NamespaceSearchStrategy(IProducerConsumerCollection<SearchResult> resultQueue, string term)
			: this(resultQueue, new[] { term })
		{
		}

		public NamespaceSearchStrategy(IProducerConsumerCollection<SearchResult> resultQueue, string[] terms)
			: base(resultQueue, terms)
		{
		}

		public override void Search(PEFile module, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var typeSystem = module.GetTypeSystemOrNull();
			if (typeSystem == null) return;

			var root = ((MetadataModule)typeSystem.MainModule).RootNamespace;
			foreach (var ns in root.ChildNamespaces)
				Search(module, ns);
		}

		private void Search(PEFile module, INamespace ns)
		{
			if (ns.Types.Any()) {
				if (IsMatch(ns.FullName))
					OnFoundResult(module, ns);
			}

			foreach (var child in ns.ChildNamespaces)
				Search(module, child);
		}

		void OnFoundResult(PEFile module, INamespace ns)
		{
			var result = new NamespaceSearchResult {
				Namespace = ns,
				Name = ns.FullName,
				Fitness = 1.0f / ns.FullName.Length,
				Location = module.Name,
				Assembly = module.FullName,
			};
			OnFoundResult(result);
		}

		public override bool IsMatch(PEFile module)
		{
			var metadata = module.Metadata;
			var root = metadata.GetNamespaceDefinitionRoot();

			foreach (var nsDef in root.NamespaceDefinitions)
				if (IsMatch(metadata, nsDef))
					return true;

			return false;
		}

		private bool IsMatch(MetadataReader metadata, NamespaceDefinitionHandle nsDef)
		{
			var ns = metadata.GetNamespaceDefinition(nsDef);
			if (ns.TypeDefinitions.Length > 0) {
				string name = metadata.GetString(nsDef);
				if (IsMatch(name))
					return true;
			}

			foreach (var childDef in ns.NamespaceDefinitions)
				if (IsMatch(metadata, childDef))
					return true;

			return false;
		}

		public override bool IsMatch(SearchResult result)
		{
			switch (result) {
				case MemberSearchResult entityResult:
					return IsMatch(entityResult.Member.Namespace);

				case AssemblySearchResult assemblyResult:
					return IsMatch(assemblyResult.Module);

				case NamespaceSearchResult namespaceResult:
					var parent = namespaceResult.Namespace.ParentNamespace;
					return IsMatch(parent?.FullName);

				default:
					return false;
			}
		}
	}
}
