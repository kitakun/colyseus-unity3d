using System;
using System.Linq;
using System.Collections.Generic;

using GameDevWare.Serialization;

namespace Colyseus
{
	public readonly struct PatchObject
	{
		public readonly string[] Path;
		// : "add" | "remove" | "replace";
		public readonly OperationType Operation;
		public readonly object Value;

		public PatchObject(OperationType operation, string[] path, object value)
		{
			Path = path;
			Operation = operation;
			Value = value;
		}

		public PatchObject(OperationType operation, string[] path) : this(operation, path, null)
		{

		}
	}

	public class Compare
	{
		public static PatchObject[] GetPatchList(IndexedDictionary<string, object> tree1, IndexedDictionary<string, object> tree2)
		{
			var patches = new List<PatchObject>();
			var path = new List<string>();

			Generate(tree1, tree2, patches, path);

			return patches.ToArray();
		}

		protected static void Generate(List<object> mirror, List<object> obj, List<PatchObject> patches, List<string> path)
		{
			var mirrorDict = new IndexedDictionary<string, object>();
			for (int i = 0; i < mirror.Count; i++)
			{
				mirrorDict.Add(i.ToString(), mirror.ElementAt(i));
			}

			var objDict = new IndexedDictionary<string, object>();
			for (int i = 0; i < obj.Count; i++)
			{
				objDict.Add(i.ToString(), obj.ElementAt(i));
			}

			Generate(mirrorDict, objDict, patches, path);
		}

		// Dirty check if obj is different from mirror, generate patches and update mirror
		protected static void Generate(IndexedDictionary<string, object> mirror, IndexedDictionary<string, object> obj, List<PatchObject> patches, List<string> path)
		{
			var newKeys = obj.Keys;
			var oldKeys = mirror.Keys;
			var deleted = false;

			for (int i = 0; i < oldKeys.Count; i++)
			{
				var key = oldKeys[i];
				if (
					obj.ContainsKey(key) &&
					obj[key] != null &&
					!(!obj.ContainsKey(key) && mirror.ContainsKey(key))
				)
				{
					var oldVal = mirror[key];
					var newVal = obj[key];

					if (
						oldVal != null && newVal != null &&
						!oldVal.GetType().IsPrimitive && oldVal.GetType() != typeof(string) &&
						!newVal.GetType().IsPrimitive && newVal.GetType() != typeof(string) &&
						Object.ReferenceEquals(oldVal.GetType(), newVal.GetType())
					)
					{
						var deeperPath = new List<string>(path)
						{
							key
						};

						if (oldVal is IndexedDictionary<string, object> oldValDictionaryInstance)
						{
							Generate(
								oldValDictionaryInstance,
								(IndexedDictionary<string, object>)newVal,
								patches,
								deeperPath
							);

						}
						else if (oldVal is List<object> oldValListInstance)
						{
							Generate(
								oldValListInstance,
								(List<object>)newVal,
								patches,
								deeperPath
							);
						}

					}
					else
					{
						if (
							(oldVal == null && newVal != null) ||
							!oldVal.Equals(newVal)
						)
						{
							var replacePath = new List<string>(path)
							{
								key
							};

							patches.Add(new PatchObject(OperationType.replace, replacePath.ToArray(), newVal));
						}
					}
				}
				else
				{
					var removePath = new List<string>(path)
					{
						key
					};

					patches.Add(new PatchObject(OperationType.remove, removePath.ToArray()));

					// property has been deleted
					deleted = true;
				}
			}

			if (!deleted && newKeys.Count == oldKeys.Count)
			{
				return;
			}

			foreach (string key in newKeys)
			{
				if (!mirror.ContainsKey(key) && obj.ContainsKey(key))
				{
					var addPath = new List<string>(path)
					{
						key
					};

					var newVal = obj[key];
					if (newVal != null)
					{
						var newValType = newVal.GetType();

						// compare deeper additions
						if (
							!newValType.IsPrimitive &&
							newValType != typeof(string)
						)
						{
							if (newVal is IndexedDictionary<string, object> indexDictionaryInstance)
							{
								Generate(new IndexedDictionary<string, object>(), indexDictionaryInstance, patches, addPath);
							}
							else if (newVal is List<object> newValListInstance)
							{
								Generate(new List<object>(), newValListInstance, patches, addPath);
							}
						}
					}

					patches.Add(new PatchObject(OperationType.add, addPath.ToArray(), newVal));
				}
			}
		}
	}
}
