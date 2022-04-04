using Convs;

namespace ConvsDebug {
	public static class Program {
		public record TextMessage(string Text) : Message;
		public record IntMessage(int Number) : Message;

		public static void Main(string[] args) {
			Test2();
		}

		private static void Test2() {
			// Create a fake tunnel that does not actually send stuff over the internet
			FakeTunnel serverTunnel = new();

			// When client receives a message, just quote it back to the server as a response to what they sent
			serverTunnel.OnRemoteReceived += (_, msg) => {
				Console.WriteLine($"CLIENT: received: {msg}");

				IntMessage pong = new(((IntMessage) msg).Number + 1);

				serverTunnel.WriteAsRemote(pong);
			};


			// Creates TunnelWithResponses around the fake tunnel
			TunnelWithResponses serverHandler = new(serverTunnel);
			serverHandler.OnReceived += (_, msg) => {
				Console.WriteLine($"SERVER: received: {msg}");

				// End at some point
				if (((IntMessage)msg).Number > 3000)
					return;

				IntMessage pong = new(((IntMessage) msg).Number + 1);

				serverHandler.Send(pong);
			};

			// Init ping pong
			serverTunnel.WriteAsRemote(new IntMessage(0));
		}

		private static void Test1() {
			// Create a fake tunnel that does not actually send stuff over the internet
			FakeTunnel serverTunnel = new();

			// When client recieves a message, just quote it back to the server as a response to what they sent
			serverTunnel.OnRemoteReceived += (_, msg) => {
				Console.WriteLine($"CLIENT: received: {msg}");

				TextMessage pong = new($"Im client and you sent: '{((TextMessage) msg).Text}'");

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
						Console.WriteLine(
							$"SERVER: The client replied specifically to my request: '{((TextMessage) response).Text}'!"
						);
				}
			);
		}
	}
}
