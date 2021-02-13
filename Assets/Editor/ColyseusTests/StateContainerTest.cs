using NUnit.Framework;
using System.Collections.Generic;

using Colyseus;
using GameDevWare.Serialization;

public class StateContainerTest
{
	StateContainer container;

	[SetUp]
	public void Init()
	{
		container = new StateContainer(GetRawData());
	}

	[TearDown]
	public void Dispose()
	{
		container.RemoveAllListeners();
	}

	[Test]
	public void ListenAddString()
	{
		var newData = GetRawData();
		newData["some_string"] = "hello!";

		var listenCalls = 0;
		container.Listen("some_string", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(OperationType.add, change.Operation);
			Assert.AreEqual("hello!", change.Value);
		});

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}

	[Test]
	public void ListenReplaceNull()
	{
		var newData = GetRawData();

		var listenCalls = 0;
		container.Listen("null", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(OperationType.replace, change.Operation);
			Assert.AreEqual(10, change.Value);
		});
		newData["null"] = 10;

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}

	[Test]
	public void ListenAddNull()
	{
		var newData = GetRawData();
		newData["null_new"] = null;

		var listenCalls = 0;
		container.Listen("null_new", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(OperationType.add, change.Operation);
			Assert.AreEqual(null, change.Value);
		});

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}

	[Test]
	public void ListenAddRemove()
	{
		var newData = GetRawData();

		var players = (IndexedDictionary<string, object>)newData["players"];
		players.Remove("key1");
		players.Add("key3", new { value = "new" });

		var listenCalls = 0;
		container.Listen("players/:id", (DataChange change) =>
		{
			listenCalls++;

			if (change.Operation == OperationType.add)
			{
				Assert.AreEqual("key3", change.Path["id"]);
				Assert.AreEqual(new { value = "new" }, change.Value);

			}
			else if (change.Operation == OperationType.remove)
			{
				Assert.AreEqual("key1", change.Path["id"]);
			}
		});

		container.Set(new IndexedDictionary<string, object>(newData));
		Assert.AreEqual(2, listenCalls);
	}

	[Test]
	public void ListenReplace()
	{
		var newData = GetRawData();

		var players = (IndexedDictionary<string, object>)newData["players"];
		players["key1"] = new IndexedDictionary<string, object>(new Dictionary<string, object> {
			{"id", "key1"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object> {
				{"x", 50},
				{"y", 100}
			})}
		});
		newData["players"] = players;

		var listenCalls = 0;
		container.Listen("players/:id/position/:axis", (DataChange change) =>
		{
			listenCalls++;

			Assert.AreEqual(change.Path["id"], "key1");

			if (change.Path["axis"] == "x")
			{
				Assert.AreEqual(change.Value, 50);

			}
			else if (change.Path["axis"] == "y")
			{
				Assert.AreEqual(change.Value, 100);
			}
		});

		container.Set(newData);
		Assert.AreEqual(2, listenCalls);
	}

	[Test]
	public void ListenReplaceString()
	{
		var newData = GetRawData();
		newData["turn"] = "mutated";

		var listenCalls = 0;
		container.Listen("turn", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(change.Value, "mutated");
		});

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}


	[Test]
	public void ListenWithoutPlaceholder()
	{
		var newData = GetRawData();

		var game = (IndexedDictionary<string, object>)newData["game"];
		game["turn"] = 1;

		var listenCalls = 0;
		container.Listen("game/turn", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(change.Operation, OperationType.replace);
			Assert.AreEqual(change.Value, 1);
		});

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}

	[Test]
	public void ListenAddArray()
	{
		var newData = GetRawData();
		var messages = (List<object>)newData["messages"];
		messages.Add("new value");

		var listenCalls = 0;
		container.Listen("messages/:number", (DataChange change) =>
		{
			listenCalls++;
			Assert.AreEqual(OperationType.add, change.Operation);
			Assert.AreEqual("new value", change.Value);
		});

		container.Set(newData);
		Assert.AreEqual(1, listenCalls);
	}

	[Test]
	public void ListenRemoveArray()
	{
		var newData = GetRawData();
		var messages = (List<object>)newData["messages"];
		messages.RemoveAt(0);

		var listenCalls = 0;
		container.Listen("messages/:number", (DataChange change) =>
		{
			listenCalls++;
			if (listenCalls == 1)
			{
				Assert.AreEqual(OperationType.remove, change.Operation);
				Assert.AreEqual("2", change.Path["number"]);
				Assert.AreEqual(null, change.Value);

			}
			else if (listenCalls == 2)
			{
				Assert.AreEqual(OperationType.replace, change.Operation);
				Assert.AreEqual("1", change.Path["number"]);
				Assert.AreEqual("three", change.Value);

			}
			else if (listenCalls == 3)
			{
				Assert.AreEqual(OperationType.replace, change.Operation);
				Assert.AreEqual("0", change.Path["number"]);
				Assert.AreEqual("two", change.Value);
			}
		});

		container.Set(newData);
		Assert.AreEqual(3, listenCalls);
	}

	[Test]
	public void ListenInitialState()
	{
		var container = new StateContainer(new IndexedDictionary<string, object>());
		var listenCalls = 0;

		container.Listen("players/:id/position/:attribute", (DataChange change) =>
		{
			listenCalls++;
		});

		container.Listen("turn", (DataChange change) =>
		{
			listenCalls++;
		});

		container.Listen("game/turn", (DataChange change) =>
		{
			listenCalls++;
		});

		container.Listen("messages/:number", (DataChange change) =>
		{
			listenCalls++;
		});

		container.Set(GetRawData());

		Assert.AreEqual(9, listenCalls);
	}

	[Test]
	public void ListenWithImmediate()
	{
		var container = new StateContainer(GetRawData());
		var listenCalls = 0;

		container.Listen("players/:id/position/:attribute", (DataChange change) =>
		{
			listenCalls++;
		}, true);

		container.Listen("turn", (DataChange change) =>
		{
			listenCalls++;
		}, true);

		container.Listen("game/turn", (DataChange change) =>
		{
			listenCalls++;
		}, true);

		container.Listen("messages/:number", (DataChange change) =>
		{
			listenCalls++;
		}, true);

		Assert.AreEqual(9, listenCalls);
	}

	protected IndexedDictionary<string, object> GetRawData()
	{
		var data = new IndexedDictionary<string, object>();
		var players = new IndexedDictionary<string, object>();

		players.Add("key1", new IndexedDictionary<string, object>(new Dictionary<string, object> {
			{"id", "key1"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object> {
				{"x", 0},
				{"y", 10}
			})}
		}));
		players.Add("key2", new IndexedDictionary<string, object>(new Dictionary<string, object> {
			{"id", "key2"},
			{"position", new IndexedDictionary<string, object>(new Dictionary<string, object>{
				{"x", 10},
				{"y", 20}
			})}
		}));

		data.Add("game", new IndexedDictionary<string, object>(new Dictionary<string, object> {
			{"turn", 0}
		}));
		data.Add("players", players);
		data.Add("turn", "none");
		data.Add("null", null);
		data.Add("messages", new List<object> { "one", "two", "three" });
		return data;
	}

}
