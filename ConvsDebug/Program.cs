using Convs;

public static class Program {
	public static void Main(string[] args) {
		RepMessage m = new("ahoj");
		Console.WriteLine(m);
		byte[] s = m.Serialize();

		RepMessage m2 = Message.Deserialize<RepMessage>(s);
		Console.WriteLine(m2);

		RepMessage m3 = Message.Deserialize<RepMessage>(s);
		Console.WriteLine(m3);
	}
}
