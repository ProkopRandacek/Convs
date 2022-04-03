# Convs

High level C# networking library.

You create record classes that suit your excact needs.
Convs then handles the serialization, matching replies and timeouts.

## Example:
Server:
```cs
// We are going to be sending just some strings
record TextMessage(string text) : Message;

// Get a Stream from for example a TcpListener
Stream s = ...

// Wrap the stream around in a MessageTunnel
MessageTunnel mt = new(s);

// Wrap the MessageTunnel around in a TunnelWithResponses
TunnelWithResponses tunnel = new(mt);

// Perhaps we just decided to greet the client
tunnel.SendWithResponse(
	new TextMessage("AHOY!"), // The Message we are sending
	(response) => { // A custom response to that message
		if (response == null) {
			// Client did not respond in time
		} else {
			// We got a response!
		}
	}
);
```

Client:
```cs
// You probably want to have a Common library between
// the Client and the Server for these classes
record TextMessage(string text) : Message;

// Get a Stream from for example a TcpListener
Stream s = ...

// Wrap the stream around in a MessageTunnel
MessageTunnel mt = new(s);

// Wrap the MessageTunnel around in a TunnelWithResponses
TunnelWithResponses tunnel = new(mt);

// This time we just listen for Messages and reply to them
tunnel.OnReceived += (_, msg) => {
	// Create a new Message
	TextMessage reply = new("pong!");

	// Specify that it is a reply of 'msg'
	reply.SetIsReplyOf(msg);

	tunnel.Send(reply);
}
```

