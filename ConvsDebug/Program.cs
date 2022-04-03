using Convs;

public static class Program {
	public record TextMessage(string text) : Message;

	public static void Main(string[] args) {
		// Create a fake tunnel that does not actually send stuff over the internet
		FakeTunnel serverTunnel = new();

		// When client recieves a message, just quote it back to the server as a response to what they sent
		serverTunnel.OnRemoteReceived += (_, msg) => {
			Console.WriteLine($"CLIENT: received: {msg}");

			TextMessage pong = new($"Im client and you sent: '{((TextMessage)msg).text}'");

			pong.SetIsReplyOf(msg);

			serverTunnel.WriteAsRemote(pong);
		};



		// Creates TunnelWithResponses around the fake tunnel
		TunnelWithResponses serverHandler = new(serverTunnel);

		// Send a Message and provide lambda that executes, when we get a response
		serverHandler.SendWithResponse(
			new TextMessage("Hello! I'm server. I want you to respond to me!"),
			(response) => {
				if (response == null)
					Console.WriteLine("SERVER: The reply timed out :(");
				else
					Console.WriteLine($"SERVER: The client replied specificaly to my request: {((TextMessage)response).text}!");
			}
		);
	}
}
