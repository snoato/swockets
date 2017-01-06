using System;
using Newtonsoft.Json.Linq;

namespace swockets.NET
{
	public class BasicHandler : SwocketHandler
	{
		private bool connected;
		public BasicHandler()
		{
			connected = true;
		}

		public void disconnect(SwocketClientSocket sock = null)
		{
			connected = false;
			Console.WriteLine("Server disconnected");
		}

		public void handshake_unsuccessful()
		{
			connected = false;
			Console.WriteLine("Handshake unsuccessful");
		}
	}
	class MainClass
	{
		public static void Main(string[] args)
		{
			Swockets s = new Swockets(SwocketMode.ISCLIENT, new BasicHandler(), "127.0.0.1");

			while (true)
			{
				s.send(JObject.Parse("{ \"message\" : \"" + Console.ReadLine() + "\"}"));
			}
		}
	}
}
