using System;

namespace Convs {
	public interface IMessageTunnel {
		public void Write(Message m);
		public void Close();
		public event EventHandler<Message>? OnReceived;
	}
}
