using System;
using System.Text;

using Newtonsoft.Json;

namespace Convs {
	public record Message {
		public bool IsReply;
		public long ReplyID; //! If this is a reply, this is the ID of the message we are replying to

		public readonly long ID =
#if UNITY_EDITOR || UNITY_STANDALONE
			(((long)UnityEngine.Random.Range(int.MinValue, int.MaxValue))<<32) | ((long)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
#else
			Rand.NextInt64();

		private static readonly Random Rand = new();
#endif

		private string? _setTypeName;

		private static Encoding Enc = Encoding.UTF8;

		public string TypeName {
			get => GetType().AssemblyQualifiedName ?? throw new Exception($"Failed to get Assembly Qualified Name for type {this.GetType().Name}");
			set => _setTypeName = value;
		}

		public void SetIsReplyOf(Message m) {
			this.IsReply = true;
			this.ReplyID = m.ID;
		}

		public byte[] Serialize() {
			return Enc.GetBytes(JsonConvert.SerializeObject(this));
		}

		public static Message Deserialize(byte[] data) {
			return Deserialize<Message>(data);
		}

		public static T Deserialize<T>(byte[] data) where T : Message {
			string s = Enc.GetString(data);

			Message? m = JsonConvert.DeserializeObject<Message>(s);
			if (m == null || m._setTypeName == null) throw new FormatException("Deserialization into generic message failed");

			Type targetType = Type.GetType(m._setTypeName) ?? throw new FormatException("Failed to read type name from deserialized message");

			Message o = (Message)(JsonConvert.DeserializeObject(s, targetType) ?? throw new FormatException("Failed to deserialize data into specialized message"));
			if (o.GetType() != targetType) throw new FormatException("Deserialized specialized message has got the wrong type");

			return o as T ?? throw new Exception($"Failed to convert deserialized message of type {o.GetType().Name} to {typeof(T).Name}");
		}
	}
}
