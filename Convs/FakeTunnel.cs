using System;

namespace Convs {
	public class FakeTunnel : IMessageTunnel {
		public event EventHandler<Message>? OnReceived;

		public void Write(Message c) {
			OnRemoteReceived?.Invoke(this, c);
		}

		public void Close() { }

		public event EventHandler<Message>? OnRemoteReceived;

		public void WriteAsRemote(Message c) {
			OnReceived?.Invoke(this, c);
		}
	}
}
