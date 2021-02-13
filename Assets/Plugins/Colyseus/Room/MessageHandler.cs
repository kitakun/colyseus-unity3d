using System;

namespace Colyseus
{
	public interface IMessageHandler
	{
		Type Type { get; }
		void Invoke(object message);
	}

	public class MessageHandler<T> : IMessageHandler
	{
		public Action<T> Action;

		public void Invoke(object message)
		{
			Action.Invoke((T)message);
		}

		public Type Type
		{
			get => typeof(T);
		}
	}
}
