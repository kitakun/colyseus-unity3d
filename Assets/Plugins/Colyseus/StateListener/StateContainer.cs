using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using GameDevWare.Serialization;

namespace Colyseus
{
	using PatchListener = Listener<Action<DataChange>>;
	using FallbackPatchListener = Listener<Action<PatchObject>>;

	public readonly struct DataChange
	{
		public readonly Dictionary<string, string> Path;
		// "add" | "remove" | "replace";
		public readonly OperationType Operation;
		public readonly object Value;

		public DataChange(Dictionary<string, string> path, OperationType operation, object value)
		{
			Path = path;
			Operation = operation;
			Value = value;
		}
	}

	public readonly struct Listener<T>
	{
		public readonly T Callback;
		public readonly Regex[] Rules;
		public readonly string[] RawRules;

		public Listener(T callback, Regex[] rules, string[] rawRules)
		{
			Callback = callback;
			Rules = rules;
			RawRules = rawRules;
		}

		public Listener(T callback, Regex[] rules) : this(callback, rules, null)
		{

		}
	}

	public class StateContainer
	{
		public IndexedDictionary<string, object> State;
		private List<PatchListener> _listeners;
		private FallbackPatchListener _defaultListener;

		private static readonly Regex[] EmptyRegexRules = new Regex[0];

		private static readonly Dictionary<string, Regex> MatcherPlaceholders = new Dictionary<string, Regex>
		{
			{ ":id", new Regex(@"^([a-zA-Z0-9\-_]+)$") },
			{ ":number", new Regex(@"^([0-9]+)$") },
			{ ":string", new Regex(@"^(\w+)$") },
			{ ":axis", new Regex(@"^([xyz])$") },
			{ ":*", new Regex(@"(.*)") },
		};

		public StateContainer(IndexedDictionary<string, object> state)
		{
			State = state;
			Reset();
		}

		public PatchObject[] Set(IndexedDictionary<string, object> newData)
		{
			var patches = Compare.GetPatchList(State, newData);

			CheckPatches(patches);
			State = newData;

			return patches;
		}

		public void RegisterPlaceholder(string placeholder, Regex matcher)
		{
			MatcherPlaceholders[placeholder] = matcher;
		}

		public FallbackPatchListener Listen(Action<PatchObject> callback)
		{
			var listener = new FallbackPatchListener(callback, EmptyRegexRules);

			_defaultListener = listener;

			return listener;
		}

		public PatchListener Listen(string segments, Action<DataChange> callback, bool immediate = false)
		{
			var rawRules = segments.Split('/');
			var regexpRules = ParseRegexRules(rawRules);

			var listener = new PatchListener(callback, regexpRules, rawRules);

			_listeners.Add(listener);

			if (immediate)
			{
				var onlyListener = new List<PatchListener>
				{
					listener
				};
				CheckPatches(Compare.GetPatchList(new IndexedDictionary<string, object>(), State), onlyListener);
			}

			return listener;
		}

		public void RemoveListener(PatchListener listener)
		{
			for (var i = _listeners.Count - 1; i >= 0; i--)
			{
				if (_listeners[i].Equals(listener))
				{
					_listeners.RemoveAt(i);
				}
			}
		}

		public void RemoveAllListeners()
		{
			Reset();
		}

		protected Regex[] ParseRegexRules(string[] rules)
		{
			var regexpRules = new Regex[rules.Length];

			for (int i = 0; i < rules.Length; i++)
			{
				var segment = rules[i];
				if (segment.IndexOf(':') == 0)
				{
					if (MatcherPlaceholders.ContainsKey(segment))
					{
						regexpRules[i] = MatcherPlaceholders[segment];
					}
					else
					{
						regexpRules[i] = MatcherPlaceholders[":*"];
					}

				}
				else
				{
					regexpRules[i] = new Regex("^" + segment + "$");
				}
			}

			return regexpRules;
		}

		private void CheckPatches(PatchObject[] patches, List<PatchListener> _listeners = null)
		{
			if (_listeners == null)
			{
				_listeners = this._listeners;
			}

			for (var i = patches.Length - 1; i >= 0; i--)
			{
				var matched = false;

				for (var j = 0; j < _listeners.Count; j++)
				{
					var listener = _listeners[j];
					var pathVariables = GetPathVariables(patches[i], listener);
					if (pathVariables != null)
					{
						var dataChange = new DataChange(pathVariables, patches[i].Operation, patches[i].Value);

						listener.Callback.Invoke(dataChange);
						matched = true;
					}
				}

				// check for fallback listener
				if (!matched && !Equals(_defaultListener, default(FallbackPatchListener)))
				{
					_defaultListener.Callback.Invoke(patches[i]);
				}
			}
		}

		private Dictionary<string, string> GetPathVariables(PatchObject patch, PatchListener listener)
		{
			var result = new Dictionary<string, string>();

			// skip if rules count differ from patch
			if (patch.Path.Length != listener.Rules.Length)
			{
				return null;
			}

			for (var i = 0; i < listener.Rules.Length; i++)
			{
				var matches = listener.Rules[i].Matches(patch.Path[i]);
				if (matches.Count == 0 || matches.Count > 2)
				{
					return null;

				}
				else if (listener.RawRules[i][0] == ':')
				{
					result.Add(listener.RawRules[i].Substring(1), matches[0].ToString());
				}
			}

			return result;
		}

		private void Reset()
		{
			_listeners = new List<PatchListener>();

			_defaultListener = default;
		}
	}
}
