using System.Collections.Concurrent;

namespace Convs;

public class TunnelWithResponses {
	private const int DefaultTimeoutMs = 6000; //! How long we wait for a response;
	private const int DeadCallbackCollectDelayMs = 2000; //! How often we delete dead callbacks

	//! The callback Actions that will be called if we receive the correct response
	private readonly ConcurrentDictionary<int, Action<Message?>> _callbacks = new(Environment.ProcessorCount*2, 100);

	//! When (Unix miliseconds) do we delete the callback
	private readonly ConcurrentDictionary<int, int> _callbackRemainingTimes = new(Environment.ProcessorCount*2, 100);

	private readonly IMessageTunnel _dumbTunnel;
	private readonly Timer _timer;

	public TunnelWithResponses(IMessageTunnel tunnel) {
		_dumbTunnel = tunnel;
		_timer      = new Timer(_ => DeleteDeadCallbacks(), null, 0, DeadCallbackCollectDelayMs);

		_dumbTunnel.OnReceived += HandleData;
	}

	public event EventHandler<Message>? OnReceived;

	public void Send(Message c) {
		_dumbTunnel.Write(c);
	}

	public void SendWithResponse(Message c, Action<Message?> onResponse, int timeout = DefaultTimeoutMs) {
		Send(c);
		_callbacks[c.GetHashCode()] = onResponse;
		_callbackRemainingTimes[c.GetHashCode()] = timeout;
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

			_callbackRemainingTimes.Remove(key, out int _);

			callback(d);
		}
	}

	/**
	 * @brief Iterates over all callbacks and deletes ones that are too old
	 */
	private void DeleteDeadCallbacks() {
		// TODO: Rewrite so that we dont have to update the dict values all the time
		//       but rather travel in time and check if the values are behind us
		foreach (int callbacksKey in _callbackRemainingTimes.Keys) {
			_callbackRemainingTimes[callbacksKey] -= DeadCallbackCollectDelayMs;
			if (_callbackRemainingTimes[callbacksKey] < 0) {
				if (_callbacks.TryRemove(callbacksKey, out Action<Message?> callback)) {
					callback(null);
					_callbackRemainingTimes.Remove(callbacksKey, out int _);
				}
			}
		}
	}
}
