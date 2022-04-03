using System.Collections.Concurrent;

namespace Convs;

public class TunnelWithResponses {
	private const int DefaultTimeoutMs = 6000; //! How long we wait for a response;
	private const int DeadCallbackCollectDelayMs = 2000; //! How often we delete dead callbacks

	//! The callback Actions that will be called if we receive the correct response
	private readonly ConcurrentDictionary<int, Action<Message?>> _callbacks = new(Environment.ProcessorCount*2, 100);

	//! When (Unix ms) do we delete the callback
	private readonly ConcurrentDictionary<int, long> _callbackDeathTime = new(Environment.ProcessorCount*2, 100);

	private readonly IMessageTunnel _dumbTunnel;
	private readonly Timer _timer;

	public event EventHandler<Message>? OnReceived;

	public TunnelWithResponses(IMessageTunnel tunnel) {
		_dumbTunnel = tunnel;
		_timer      = new Timer(_ => DeleteDeadCallbacks(), null, 0, DeadCallbackCollectDelayMs);

		_dumbTunnel.OnReceived += HandleData;
	}

	public void Send(Message c) {
		_dumbTunnel.Write(c);
	}

	public void SendWithResponse(Message c, Action<Message?> onResponse, int timeout = DefaultTimeoutMs) {
		this.Send(c);

		long timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();

		_callbacks[c.setIDForResponse] = onResponse;
		_callbackDeathTime[c.setIDForResponse] = timeNow + timeout;
	}

	public void Close() {
		_timer.Dispose();
		_dumbTunnel.Close();
	}

	private void HandleData(object? _, Message d) {
		if (d is not RepMessage rm) {
			OnReceived?.Invoke(this, d);
		} else { // it is a response
			int key = rm.setIDForResponse;

			// we don't know this callback
			if (!_callbacks.ContainsKey(key)) {
				throw new Exception("Received invalid callback");
			}

			_callbacks.Remove(key, out Action<Message?>? callback);
			if (callback == null)
				throw new Exception("Failed to remove Action from callbacks dictionary");

			_callbackDeathTime.Remove(key, out long _);

			callback(d);
		}
	}

	/**
	 * @brief Iterates over all callbacks and deletes ones that are too old
	 */
	private void DeleteDeadCallbacks() {
		long timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();

		foreach (int callbacksKey in _callbackDeathTime.Keys) {
			if (_callbackDeathTime[callbacksKey] < timeNow) {
				if (_callbacks.TryRemove(callbacksKey, out Action<Message?>? callback)) {
					if (callback != null) {
						callback(null);
						_callbackDeathTime.Remove(callbacksKey, out long _);
					}
				}
			}
		}
	}
}
