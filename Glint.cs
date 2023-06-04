using FuzzyString;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Glint
{
	namespace Collections
	{
		/// <summary>
		/// Represents a dual-stack of undo and redo history of the same specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public class History<T>
		{
			private readonly Stack<T> undoHistory;
			private readonly Stack<T> redoHistory;

			/// <summary>
			/// Initializes a new instance of the <see cref="History{T}"/> class that is empty.
			/// </summary>
			public History()
			{
				undoHistory = new Stack<T>();
				redoHistory = new Stack<T>();
			}

			/// <summary>
			/// Inserts an object at the top of the undo-stack and removes all objects from the redo-stack.
			/// </summary>
			/// <param name="item"></param>
			public void Push(T item)
			{
				redoHistory.Clear();
				undoHistory.Push(item);
			}

			/// <returns><see langword="true"/> if the redo-stack is not empty; otherwise, <see langword="false"/>.</returns>
			public bool HasRedo()
			{
				return (redoHistory.Count > 0);
			}

			/// <returns><see langword="true"/> if the undo-stack is not empty; otherwise, <see langword="false"/>.</returns>
			public bool HasUndo()
			{
				return (undoHistory.Count > 0);
			}

			/// <summary>
			/// Moves the object on the top of the redo-stack to the top of the undo-stack and returns it.
			/// </summary>
			/// <returns>The object that was moved.</returns>
			/// <exception cref="InvalidOperationException"/>
			public T Redo()
			{
				var item = redoHistory.Pop();
				undoHistory.Push(item);

				return item;
			}

			/// <summary>
			/// Moves the object on the top of the undo-stack to the top of the redo-stack and returns it.
			/// </summary>
			/// <returns>The object that was moved.</returns>
			/// <exception cref="InvalidOperationException"/>
			public T Undo()
			{
				var item = undoHistory.Pop();
				redoHistory.Push(item);

				return item;
			}

			/// <summary>
			/// Returns the object at the top of the redo-stack without removing it.
			/// </summary>
			/// <returns>The object at the top of the redo-stack.</returns>
			/// <exception cref="InvalidOperationException"/>
			public T PeekRedo()
			{
				return redoHistory.Peek();
			}

			/// <summary>
			/// Returns the object at the top of the undo-stack without removing it.
			/// </summary>
			/// <returns>The object at the top of the undo-stack.</returns>
			/// <exception cref="InvalidOperationException"/>
			public T PeekUndo()
			{
				return undoHistory.Peek();
			}

			/// <summary>
			/// Removes all objects from the <see cref="History{T}"/>.
			/// </summary>
			public void Clear()
			{
				undoHistory.Clear();
				redoHistory.Clear();
			}

			public override int GetHashCode()
			{
				int offset = 0;
				uint currentCode = 0u;
				foreach (var value in undoHistory)
				{
					var next = (uint)value.GetHashCode();
					currentCode ^= (next << offset) | (next >> (-offset));
					offset += 2;
				}

				return (int)currentCode;
			}
		}

		public class Tree<T> : IEnumerable<T>
		{
			public class Enumerator : IEnumerator<T>
			{
				private int current = -1;
				private List<T> list;

				public Enumerator(Tree<T> tree)
				{
					list = tree.GetValues();
				}

				object IEnumerator.Current { get; }
				public T Current { get => list[current]; }

				public bool MoveNext()
				{
					return ((++current) < list.Count);
				}

				public void Reset()
				{
					current = -1;
				}

				public void Dispose()
				{
					current = -1;
					list = null;
				}
			}

			public class BaseNode
			{
				protected BaseNode parent;
				protected List<Node> children;

				public int ChildCount { get => children.Count; }
				public BaseNode Parent { get => parent; }
				internal List<Node> Children { get => children; }

				public int Count
				{
					get
					{
						var count = 0;

						foreach (var child in children)
						{
							count += 1 + child.Count;
						}

						return count;
					}
				}

				public BaseNode()
				{
					parent = null;
					children = new List<Node>();
				}

				public BaseNode(BaseNode parent)
				{
					this.parent = parent;
					children = new List<Node>();
				}

				public void Clear()
				{
					children.Clear();
				}

				public void AddChild(Node node)
				{
					// Remove from current parent
					if (node.Parent != null)
					{
						node.Parent.RemoveChild(node);
					}

					children.Add(node);
					node.parent = this;
				}

				public bool RemoveChild(Node node)
				{
					return children.Remove(node);
				}

				public bool ContainsChild(Node node)
				{
					foreach (var child in children)
					{
						if (child == node)
						{
							return true;
						}

						if (child.ContainsChild(node))
						{
							return true;
						}
					}

					return false;
				}

				public int GetChildIndex(Node node)
				{
					return children.FindIndex(match => match == node);
				}

				public int GetChildIndex(BaseNode node)
				{
					return children.FindIndex(match => match == (node as Node));
				}

				public int GetLocalIndex()
				{
					if (parent != null)
					{
						return parent.GetChildIndex(this);
					}

					return -1;
				}

				public void MoveChild(Node node, int index)
				{
					if (index < 0 || index >= children.Count)
					{
						throw new IndexOutOfRangeException();
					}

					var idx = GetChildIndex(node);

					if (idx >= 0)
					{
						children.RemoveAt(idx);
						children.Insert(index, node);
					}
				}
			}

			public class Node : BaseNode
			{
				private T data;

				public T Data { get => data; set => data = value; }

				public Node(T data) : base()
				{
					this.data = data;
				}

				public Node(T data, BaseNode parent) : base(parent)
				{
					this.data = data;
				}
			}

			private readonly BaseNode root;
			private bool dataDirty = false;
			private List<Node> data;
			private readonly Dictionary<T, Node> lookup;

			private List<Node> Data { get => dataDirty ? GetNodes() : data; }

			public int Count
			{
				get => root.Count;
			}

			public Tree()
			{
				root = new BaseNode();
				data = new List<Node>();
				lookup = new Dictionary<T, Node>();
			}

			public T this[int key]
			{
				get
				{
					return Data[key].Data;
				}

				set
				{
					Data[key].Data = value;
				}
			}

			public T[] ToArray()
			{
				return GetValues().ToArray();
			}

			internal List<T> GetValues()
			{
				return GetValues(root);
			}

			private List<T> GetValues(BaseNode node)
			{
				var list = new List<T>();

				foreach (var child in node.Children)
				{
					list.Add(child.Data);
					list.AddRange(GetValues(child));
				}

				return list;
			}

			internal List<Node> GetNodes()
			{
				data = GetNodes(root);
				dataDirty = false;

				return data;
			}

			private List<Node> GetNodes(BaseNode node)
			{
				var list = new List<Node>();

				foreach (var child in node.Children)
				{
					list.Add(child);
					list.AddRange(GetNodes(child));
				}

				return list;
			}

			public Node Find(T data)
			{
				if (lookup.TryGetValue(data, out Node output))
				{
					return output;
				}

				return null;
			}

			public void Add(T data, BaseNode parent = null)
			{
				parent ??= root;

				var node = new Node(data, parent);
				parent.AddChild(node);

				lookup[data] = node;

				dataDirty = true;
			}

			public void Add(T data, T parent)
			{
				Add(data, Find(parent));
			}

			public bool Remove(T data)
			{
				var node = Find(data);

				if (node != null)
				{
					lookup.Remove(data);

					if (node.Parent != null)
					{
						if (node.Parent.RemoveChild(node))
						{
							dataDirty = true;
							return true;
						}
					}
				}

				return false;
			}

			public void Clear()
			{
				root.Clear();
				lookup.Clear();

				dataDirty = true;
			}

			public bool Move(Node node, int index)
			{
				// Moves a node to specified index within its parent's children
				if (node.Parent != null)
				{
					node.Parent.MoveChild(node, index);
				}

				dataDirty = true;
				return true;
			}

			public bool Move(Node node, Node dest, bool above, out int localIndex)
			{
				localIndex = -1;
				if (node == dest)
					return false;

				if (node.Parent == dest.Parent)
				{
					// Get the local index of dest
					var idx = node.GetLocalIndex();
					var destidx = dest.GetLocalIndex();

					if (destidx >= 0)
					{
						destidx += (above ? 0 : 1) + (destidx > idx ? -1 : 0);

						if (Move(node, destidx))
						{
							localIndex = destidx;
							return true;
						}
					}
				}
				else
				{
					// Get the local index of dest
					var idx = dest.GetLocalIndex();

					if (idx >= 0)
					{
						idx += (above ? 0 : 1);

						if (Reparent(node, dest.Parent, idx))
						{
							localIndex = idx;
							return true;
						}
					}
				}

				return false;
			}

			public bool Move(T node, T dest, bool above, out int localIndex)
			{
				localIndex = -1;
				var _node = Find(node);

				if (_node != null)
				{
					return Move(_node, Find(dest), above, out localIndex);
				}

				return false;
			}

			public bool Reparent(Node node, BaseNode parent, int index = -1)
			{
				parent ??= root;

				// Cannot parent to self
				if (node == parent)
					return false;

				// Cannot parent to descendent
				if (parent is Node nodeParent && node.ContainsChild(nodeParent))
				{
					return false;
				}

				// Add to new parent
				if (parent != null)
				{
					parent.AddChild(node);
				}

				// Move to local index if provided
				if (index >= 0)
				{
					Move(node, index);
				}

				dataDirty = true;

				return true;
			}

			public bool Reparent(T node, T parent, int index = -1)
			{
				var _node = Find(node);

				if (_node != null)
				{
					return Reparent(_node, Find(parent), index);
				}

				return false;
			}

			public IEnumerator<T> GetEnumerator()
			{
				return (IEnumerator<T>)new Enumerator(this);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			public override string ToString()
			{
				return PrintNodes(root.Children, "");
			}

			private string PrintNodes(List<Node> nodes, string indent)
			{
				var output = "";

				foreach (var node in nodes)
				{
					if (output != "")
						output += "\n";
					output += indent + node.Data;

					if (node.ChildCount > 0)
					{
						output += "\n" + PrintNodes(node.Children, indent + "\t");
					}
				}

				return output;
			}
		}
	}

	public static class StringComparison
	{
		public static int Levenshtein(string s, string t)
		{
			int n = s.Length;
			int m = t.Length;
			int[,] d = new int[n + 1, m + 1];

			// Verify arguments
			if (n == 0)
			{
				return m;
			}

			if (m == 0)
			{
				return n;
			}

			// Initialize arrays
			for (int i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (int j = 0; j <= m; d[0, j] = j++)
			{
			}

			// Begin looping
			for (int i = 1; i <= n; i++)
			{
				for (int j = 1; j <= m; j++)
				{
					// Compute cost.
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + cost);
				}
			}

			// Return cost
			return d[n, m];
		}

		public static float StringSimilarity(string s, string t)
		{
			// Count how many unique characters from t are contained in s
			var chars = new Dictionary<char, int>();
			var count = t.Length;

			foreach (var tChar in t)
			{
				if (!chars.ContainsKey(tChar))
				{
					chars.Add(tChar, 1);
				}
				else
				{
					chars[tChar]++;
				}
			}

			foreach (var key in chars.Keys)
			{
				var occurences = s.Length - s.Replace(key.ToString(), "").Length;

				count -= Math.Min(occurences, chars[key]);
			}

			var countSim = (1.0f - ((float)count / (float)t.Length));
			var distSim = (1.0f - ((float)Levenshtein(s, t) / (float)Math.Max(s.Length, t.Length)));

			return (countSim * 0.4f + distSim * 0.6f);
		}

		public static bool Matches(this string source, string target)
		{
			var options = new List<ComparisonOptions>
			{
				// Choose algorithms to use
				ComparisonOptions.UseOverlapCoefficient,
				ComparisonOptions.UseLongestCommonSubsequence,
				ComparisonOptions.UseLongestCommonSubstring
			};

			// Relative strength
			var tolerance = ComparisonTolerance.Strong;

			return source.ApproximatelyEquals(target, tolerance, options.ToArray());
		}
	}

	public struct UniqueName
	{
		public string text;
		public int id;

		public UniqueName(string text, int id)
		{
			this.text = text;
			this.id = id;
		}

		public UniqueName(string text)
		{
			this.text = text.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
			var strID = text.Remove(0, this.text.Length);

			int strIDValue = -1;
			if (strID.Length > 0)
			{
				int.TryParse(strID, out strIDValue);
			}

			id = strIDValue;
		}

		public static implicit operator UniqueName(string s) => new UniqueName(s);
		public static implicit operator string(UniqueName un) => un.ToString();

		public override string ToString()
		{
			if (text == null)
			{
				return null;
			}

			return text + (id >= 0 ? id.ToString() : "");
		}
	}
}
