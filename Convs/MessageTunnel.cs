namespace Convs;

/**
 * @brief Simple wrapper around a Stream with OnReceived event
 */
public class MessageTunnel : IMessageTunnel {
	private readonly Stream _stream;
	private readonly Timer _timer;
	private readonly int _maxPacketSize;

	public event EventHandler<Message>? OnReceived;

	public MessageTunnel(Stream stream, int readIntervalMs = 60, int maxPacketSize = 2<<16) {
		_stream = stream;
		_timer  = new Timer(CheckStream, null, 0, readIntervalMs);
		_maxPacketSize = maxPacketSize;
	}

	public void Write(Message m) {
		_stream.Write(m.Serialize());
	}

	public void Close() {
		_timer.Dispose();
		_stream.Close();
	}

	private void CheckStream(object? _) {
		byte[] bytes = new byte[_maxPacketSize];

		int len = _stream.Read(bytes, 0, bytes.Length);

		Array.Resize(ref bytes, len);

		Message m = Message.Deserialize(bytes);

		OnReceived?.Invoke(this, m);
	}
}
