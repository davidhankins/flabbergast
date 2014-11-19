using System;
using System.Collections.Generic;

namespace Flabbergast {
[Flags]
public enum Type {
	Bool = 1,
	Int = 2,
	Float = 4,
	Str = 8,
	Template = 16,
	Tuple = 32,
	Unit = 64,
	Any = 127
}
public abstract class AstTypeableNode : AstNode {
	protected Environment Environment;
	internal virtual int EnvironmentPriority { get { return Environment.Priority; } }
	// TODO implement
	internal virtual Type Type { get { return 0; } }
	internal abstract void PropagateEnvironment(ErrorCollector collector, List<AstTypeableNode> queue, Environment environment);
	public void Analyse(ErrorCollector collector) {
		var environment = new Environment (FileName, StartRow, StartColumn, EndRow, EndColumn, null, false);
		var queue = new List<AstTypeableNode>();
		PropagateEnvironment(collector, queue, environment);
		var sorted_nodes = new SortedDictionary<int, Dictionary<AstTypeableNode, bool>>();

		foreach (var element in queue) {
			if (!sorted_nodes.ContainsKey(element.Environment.Priority)) {
				sorted_nodes[element.Environment.Priority] = new Dictionary<AstTypeableNode, bool>();
			}
			sorted_nodes[element.Environment.Priority][element] = true;
		}
	}
	// TODO implement
	internal virtual void EnsureType(Type type) { }
public interface ErrorCollector {
	void ReportTypeError(AstNode where, Type new_type, Type existing_type);
	void ReportTypeError(Environment environment, string name, Type new_type, Type existing_type);
	void RawError(AstNode where, string message);
}
internal abstract class AstTypeableSpecialNode : AstTypeableNode {
	protected Environment SpecialEnvironment;
	internal override int EnvironmentPriority {
		get {
			var ep = Environment == null ? 0 : Environment.Priority;
			var esp = SpecialEnvironment == null ? 0 : SpecialEnvironment.Priority;
			return Math.Max(ep, esp);
		}
	}
	internal abstract void PropagateSpecialEnvironment(ErrorCollector collector, List<AstTypeableNode> queue, Environment special_environment);
}
internal class EnvironmentPrioritySorter : IComparer<AstTypeableNode> {
	public int Compare(AstTypeableNode x, AstTypeableNode y) {
		return x.EnvironmentPriority - y.EnvironmentPriority;
	}
}
public abstract class NameInfo {
	protected Dictionary<string, NameInfo> Children = new Dictionary<string, NameInfo>();
	public string Name { get; protected set; }
	public abstract Type Type { get; }
	internal NameInfo Lookup(ErrorCollector collector, string name) {
		EnsureType(collector, Type.Tuple);
		if (!Children.ContainsKey(name)) {
			CreateChild(collector, name, Name);
		}
		return Children[name];
	}
	internal NameInfo Lookup(ErrorCollector collector, IEnumerator<string> names) {
		var info = this;
		while (names.MoveNext()) {
			info.EnsureType(collector, Type.Tuple);
			if (!info.Children.ContainsKey(names.Current)) {
				info.CreateChild(collector, names.Current, this.Name);
			}
			info = info.Children[names.Current];
		}
		return info;
	}
	public virtual bool HasName(string name) {
		return Children.ContainsKey(name);
	}
	public abstract void EnsureType(ErrorCollector collector, Type type);
	public abstract void CreateChild(ErrorCollector collector, string name, string root);
	public virtual bool NeedsToBreakFlow() {
		foreach (var info in Children.Values) {
			if (info.NeedsToBreakFlow()) {
				return true;
			}
		}
		return false;
	}
}
public class OpenNameInfo : NameInfo {
	private Environment Environment;
	Type RealType = Type.Any;
	public OpenNameInfo(Environment environment, string name) {
		Environment = environment;
		Name = name;
	}
	public override void EnsureType(ErrorCollector collector, Type type) {
		if ((RealType & type) == 0) {
			collector.ReportTypeError(Environment, Name, RealType, type);
		} else {
			RealType &= type;
		}
	}
	public override void CreateChild(ErrorCollector collector, string name, string root) {
		Children[name] = new OpenNameInfo(Environment, root + "." + name);
	}
	public override bool NeedsToBreakFlow() {
		return true;
	}
}
internal class BoundNameInfo : NameInfo {
	AstTypeableNode Expression;
	private Environment Environment;
	public BoundNameInfo(Environment environment, string name, AstTypeableNode expression) {
		Environment = environment;
		Name = name;
		Expression = expression;
	}
	public override void EnsureType(ErrorCollector collector, Type type) {
		Target.EnsureType(collector, type);
	}
	public override void CreateChild(ErrorCollector collector, string name, string root) {
		Children[name] = new OpenNameInfo(Environment, root + "." + name);
	}
}
internal class CopyFromParentInfo : NameInfo {
	Environment Environment;
	NameInfo Source;
	Type Mask = Type.Any;
	bool ForceBack;

	public CopyFromParentInfo(Environment environment, string name, NameInfo source, bool force_back) {
		Environment = environment;
		Name = name;
		Source = source;
		ForceBack = force_back;
	}
	public override void EnsureType(ErrorCollector collector, Type type) {
		if (ForceBack) {
			Source.EnsureType(collector, type);
		} else {
			if ((Mask & type) == 0) {
				collector.ReportTypeError(Environment, Name, Mask, type);
			}
			Mask &= type;
		}
	}
	public override void CreateChild(ErrorCollector collector, string name, string root) {
		if (ForceBack) {
			Source.CreateChild(collector, name, root);
		}
		if (Source.HasName(name)) {
			Children[name] = new CopyFromParentInfo(Environment, root + "." + name, Source.Lookup(collector, name), ForceBack);
		} else {
			Children[name] = new OpenNameInfo(Environment, root + "." + name);
		}
	}
	public override bool HasName(string name) {
		return base.HasName(name) || Source.HasName(name);
	}
}
public class Environment {
	Environment Parent;
	Dictionary<string, NameInfo> Children = new Dictionary<string, NameInfo>();
	public string FileName { get; private set; }
	public int StartRow { get; private set; }
	public int StartColumn { get; private set; }
	public int EndRow { get; private set; }
	public int EndColumn { get; private set; }
	public int Priority { get; private set; }
	bool ForceBack;

	public Environment(string filename, int start_row, int start_column, int end_row, int end_column, Environment parent = null, bool force_back = false) {
		if (force_back && parent == null) {
			throw new ArgumentException("Parent environment cannot be null when forcing parent-backed creation.");
		}
		FileName = filename;
		StartRow = start_row;
		StartColumn = start_column;
		EndRow = end_row;
		EndColumn = end_column;
		ForceBack = force_back;
		Parent = parent;
		Priority = parent == null ? 0 : parent.Priority + 1;
	}

	internal NameInfo AddMask(string name, AstTypeableNode expression) {
		if (Children.ContainsKey(name)) {
			throw new InvalidOperationException("The name " + name + " already exists in the environment.");
		}
		return Children[name] = new BoundNameInfo(this, name, expression);
	}
	public NameInfo AddFreeName(string name) {
		return Children[name] = new OpenNameInfo(this, name);
	}
	public NameInfo Lookup(ErrorCollector collector, IEnumerable<string> names) {
		IEnumerator<string> enumerator = names.GetEnumerator();
		if (!enumerator.MoveNext()) {
			throw new ArgumentOutOfRangeException("List of names cannot be empty.");
		}
		if (Children.ContainsKey(enumerator.Current)) {
			return Children[enumerator.Current].Lookup(collector, enumerator);
		}
		if (ForceBack) {
			Parent.Lookup(collector, names);
		}
		if (Parent != null && Parent.HasName(enumerator.Current)) {
			return Lookback(enumerator.Current).Lookup(collector, enumerator);
		}
		var info = new OpenNameInfo(this, enumerator.Current);
		Children[enumerator.Current] = info;
		return info.Lookup(collector, enumerator);
	}
	public bool HasName(string name) {
		return Children.ContainsKey(name) || Parent != null && Parent.HasName(name);
	}
	private NameInfo Lookback(string name) {
		if (Children.ContainsKey(name)) {
			return Children[name];
		}
		var copy_info = new CopyFromParentInfo(this, name, Parent.Lookback(name), ForceBack);
		Children[name] = copy_info;
		return copy_info;
	}
}
}